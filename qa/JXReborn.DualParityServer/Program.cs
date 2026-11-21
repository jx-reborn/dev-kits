using System.Diagnostics;
using System.Text.Json;
using JXReborn.DualParityServer.Automation;
using JXReborn.DualParityServer.Configuration;
using JXReborn.DualParityServer.Diffing;
using JXReborn.DualParityServer.Mirroring;
using JXReborn.DualParityServer.Reporting;
using JXReborn.DualParityServer.Session;
using JXReborn.DualParityServer.Standalone;
using JXReborn.DualParityServer.Web;

DualParityOptions options = DualParityOptions.ParseArgs(args);

var diffEngine = new SemanticDiffEngine(new DiffOptions
{
    PositionToleranceMps = options.PositionToleranceMps,
    CriticalPositionDriftMps = options.CriticalPositionDriftMps
});

await using var session = new DualSessionManager(options, diffEngine);
await using var mirror = new InteractiveMirrorEngine(session);
var crawler = new NpcDialogCrawler(session);
var scenarioRunner = new ScenarioRunner(session, crawler);
var reportWriter = new ParityReportWriter();

StandaloneStackOrchestrator? standalone = options.Standalone
    ? new StandaloneStackOrchestrator(options, session.SessionId)
    : null;

try
{
    Console.WriteLine($"[DualParity] Session ID   : {session.SessionId}");
    Console.WriteLine($"[DualParity] Mode         : {(options.Standalone ? "STANDALONE (All-in-One)" : options.Mode)}");
    Console.WriteLine($"[DualParity] Legacy       : {options.LegacyHost}:{options.LegacyPort} (Proxy: {(options.LegacyProxyPort > 0 ? $"127.0.0.1:{options.LegacyProxyPort}" : "off")})");
    Console.WriteLine($"[DualParity] Reborn       : {options.RebornHost}:{options.RebornPort} (Proxy: {(options.RebornProxyPort > 0 ? $"127.0.0.1:{options.RebornProxyPort}" : "off")})");

    if (standalone is not null)
    {
        Console.WriteLine("[DualParity] Booting standalone stack (legacy-linux container + isolated JXReborn.Host T=0)...");
        await standalone.StartAsync();
        Console.WriteLine("[DualParity] Both servers are ready.");
    }

    session.StartProxiesIfConfigured();
    await session.ConnectClientsAsync();

    if (string.Equals(options.Mode, "scenario", StringComparison.OrdinalIgnoreCase))
    {
        if (string.IsNullOrWhiteSpace(options.ScenarioPath) || !File.Exists(options.ScenarioPath))
        {
            Console.Error.WriteLine("[DualParity] --scenario <path.json> is required in scenario mode.");
            return 1;
        }

        string json = await File.ReadAllTextAsync(options.ScenarioPath);
        ParityScenario scenario = JsonSerializer.Deserialize<ParityScenario>(json)
            ?? throw new InvalidOperationException($"Failed to parse scenario '{options.ScenarioPath}'.");

        IReadOnlyList<StepComparisonResult> results = await scenarioRunner.RunAsync(scenario);
        ExportedReportPaths paths = await reportWriter.ExportAsync(session, standalone: standalone);
        int failed = results.Count(r => !r.Passed);
        Console.WriteLine($"[DualParity] Scenario '{scenario.Name}' done: {results.Count - failed}/{results.Count} passed.");
        Console.WriteLine($"[DualParity] Reports      : {paths.OutputDirectory}");
        return failed == 0 ? 0 : 2;
    }

    if (string.Equals(options.Mode, "npccrawl", StringComparison.OrdinalIgnoreCase))
    {
        IReadOnlyList<NpcParityScoreEntry> scores = await crawler.CrawlNearbyNpcsAsync(maxBranchesPerNpc: 0);
        ExportedReportPaths paths = await reportWriter.ExportAsync(session, standalone: standalone);
        int matched = scores.Count(s => s.Matched);
        Console.WriteLine($"[DualParity] NPC Crawl done: {matched}/{scores.Count} NPCs matched.");
        Console.WriteLine($"[DualParity] Reports       : {paths.OutputDirectory}");
        return matched == scores.Count ? 0 : 2;
    }

    bool autoStartMirror =
        (options.Standalone || string.Equals(options.Mode, "interactive", StringComparison.OrdinalIgnoreCase)) &&
        session.BothClientsConnected;

    if (autoStartMirror)
    {
        mirror.Start();
        Console.WriteLine("[DualParity] Interactive Mirror auto-started (Legacy -> Reborn).");
    }

    string dashboardUrl = $"http://127.0.0.1:{options.DashboardPort}";
    WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
    builder.Logging.ClearProviders();
    builder.WebHost.UseUrls(dashboardUrl);

    WebApplication app = builder.Build();
    app.MapDualParityEndpoints(session, mirror, scenarioRunner, crawler, reportWriter, standalone);

    Console.WriteLine($"[DualParity] Dashboard    : {dashboardUrl}");
    Console.WriteLine("[DualParity] Press Ctrl+C to stop, export reports, and clean up.");

    if (options.Standalone && options.OpenBrowserOnStandalone && options.InteractiveWindows)
    {
        TryOpenBrowser(dashboardUrl);
    }

    await app.RunAsync();

    if (session.GetSteps().Count > 0 || session.GetNpcScorecard().Count > 0 || standalone is not null)
    {
        ExportedReportPaths finalPaths = await reportWriter.ExportAsync(session, standalone: standalone);
        Console.WriteLine($"[DualParity] Auto-exported reports to: {finalPaths.OutputDirectory}");
    }

    return 0;
}
finally
{
    if (standalone is not null)
    {
        Console.WriteLine("[DualParity] Cleaning up standalone stack...");
        await standalone.DisposeAsync();
    }
}

static void TryOpenBrowser(string url)
{
    try
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = url,
            UseShellExecute = true
        });
    }
    catch (Exception)
    {
        // Non-fatal if browser cannot be opened.
    }
}
