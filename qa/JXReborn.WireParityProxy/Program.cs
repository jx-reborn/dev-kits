using System.Collections.Concurrent;
using System.Diagnostics;
using JXReborn.Adapter.Legacy.Ciphers;
using JXReborn.DualParityServer.Diffing;
using JXReborn.DualParityServer.Standalone;
using JXReborn.WireParityProxy.Configuration;
using JXReborn.WireParityProxy.Driver;
using JXReborn.WireParityProxy.Proxy;
using JXReborn.WireParityProxy.Reporting;
using JXReborn.WireParityProxy.Web;
using JXReborn.WireParityProxy.Wire;

WireParityProxyOptions options = WireParityProxyOptions.ParseArgs(args);
LegacyKeyTable keyTable = WireKeyTableLoader.Load(options);

string runId = $"wpp-{DateTimeOffset.UtcNow:yyyyMMdd-HHmmss}";
var globalFindings = new ConcurrentQueue<ParityFinding>();
var handoffMap = new HandoffTicketMap(TimeSpan.FromSeconds(Math.Max(5, options.HandoffTicketTtlSeconds)));

StandaloneStackOrchestrator? standalone = options.StandaloneMode
    ? new StandaloneStackOrchestrator(options.ToStandaloneDualParityOptions(), runId)
    : null;

await using var driverBridge = new PrimaryClientDriverBridge();
await using var gameServerProxy = new GameServerShadowProxy(options, keyTable, handoffMap);
await using var bishopProxy = new BishopShadowProxy(
    options,
    keyTable,
    handoffMap,
    () => gameServerProxy.BoundPort,
    finding => globalFindings.Enqueue(finding));

try
{
    Console.WriteLine($"[WireParityProxy] Run ID     : {runId}");
    Console.WriteLine($"[WireParityProxy] Mode       : {(options.StandaloneMode ? "STANDALONE (All-in-One)" : "Attach to running servers")}");
    Console.WriteLine($"[WireParityProxy] Primary    : {options.PrimaryBackendName}");
    Console.WriteLine($"[WireParityProxy] Shadow     : {options.ShadowBackendName}");

    if (standalone is not null)
    {
        Console.WriteLine("[WireParityProxy] Booting standalone stack (legacy-linux container T=0 + isolated JXReborn.Host T=0)...");
        await standalone.StartAsync();
        Console.WriteLine("[WireParityProxy] Both backend servers are ready.");
    }

    gameServerProxy.Start();
    bishopProxy.Start();

    Console.WriteLine($"[WireParityProxy] Bishop L7  : {options.BindHost}:{bishopProxy.BoundPort}");
    Console.WriteLine($"[WireParityProxy] GS L7      : {options.BindHost}:{gameServerProxy.BoundPort}");

    if (options.SpawnClient)
    {
        Console.WriteLine($"[WireParityProxy] Spawning single game.exe pointing to {options.BindHost}:{bishopProxy.BoundPort}...");
        await driverBridge.SpawnAgainstProxyAsync(options, bishopProxy.BoundPort, options.InteractiveWindow);
        Console.WriteLine("[WireParityProxy] Single game.exe + driver attached.");
    }

    string dashboardUrl = $"http://{options.BindHost}:{options.DashboardPort}";
    WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
    builder.WebHost.UseUrls(dashboardUrl);
    builder.Logging.SetMinimumLevel(LogLevel.Warning);

    WebApplication app = builder.Build();
    WireParityDashboardEndpoints.Map(app, options, bishopProxy, gameServerProxy, globalFindings, standalone);

    Console.WriteLine($"[WireParityProxy] Dashboard  : {dashboardUrl}");
    Console.WriteLine("[WireParityProxy] Press Ctrl+C to stop, export reports, and clean up.");

    if (options.StandaloneMode && options.OpenBrowserOnStandalone && options.InteractiveWindow)
    {
        TryOpenBrowser(dashboardUrl);
    }

    await app.RunAsync();
}
finally
{
    WireParitySession? latest = gameServerProxy.LatestSession;
    if (latest is not null)
    {
        try
        {
            string reportDir = WireParityReportWriter.WriteAll(latest, options.ReportOutputDir, globalFindings.ToArray());
            if (standalone is not null)
            {
                await standalone.WriteServerLogsAsync(reportDir);
            }

            Console.WriteLine($"[WireParityProxy] Reports exported to: {reportDir}");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[WireParityProxy] Failed to export final report: {ex.Message}");
        }
    }

    if (standalone is not null)
    {
        Console.WriteLine("[WireParityProxy] Shutting down standalone servers...");
        await standalone.DisposeAsync();
    }
}

static void TryOpenBrowser(string url)
{
    try
    {
        Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
    }
    catch
    {
        // Best-effort browser open.
    }
}
