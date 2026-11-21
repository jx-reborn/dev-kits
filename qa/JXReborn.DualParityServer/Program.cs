using System.Text.Json;
using JXReborn.DualParityServer.Automation;
using JXReborn.DualParityServer.Configuration;
using JXReborn.DualParityServer.Diffing;
using JXReborn.DualParityServer.Mirroring;
using JXReborn.DualParityServer.Reporting;
using JXReborn.DualParityServer.Session;
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

session.StartProxiesIfConfigured();

Console.WriteLine($"[DualParity] Session ID   : {session.SessionId}");
Console.WriteLine($"[DualParity] Legacy       : {options.LegacyHost}:{options.LegacyPort} (Proxy: {(options.LegacyProxyPort > 0 ? $"127.0.0.1:{options.LegacyProxyPort}" : "off")})");
Console.WriteLine($"[DualParity] Reborn       : {options.RebornHost}:{options.RebornPort} (Proxy: {(options.RebornProxyPort > 0 ? $"127.0.0.1:{options.RebornProxyPort}" : "off")})");

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
    ExportedReportPaths paths = await reportWriter.ExportAsync(session);
    int failed = results.Count(r => !r.Passed);
    Console.WriteLine($"[DualParity] Scenario '{scenario.Name}' done: {results.Count - failed}/{results.Count} passed.");
    Console.WriteLine($"[DualParity] Reports      : {paths.OutputDirectory}");
    return failed == 0 ? 0 : 2;
}

if (string.Equals(options.Mode, "npccrawl", StringComparison.OrdinalIgnoreCase))
{
    IReadOnlyList<NpcParityScoreEntry> scores = await crawler.CrawlNearbyNpcsAsync(maxBranchesPerNpc: 0);
    ExportedReportPaths paths = await reportWriter.ExportAsync(session);
    int matched = scores.Count(s => s.Matched);
    Console.WriteLine($"[DualParity] NPC Crawl done: {matched}/{scores.Count} NPCs matched.");
    Console.WriteLine($"[DualParity] Reports       : {paths.OutputDirectory}");
    return matched == scores.Count ? 0 : 2;
}

if (string.Equals(options.Mode, "interactive", StringComparison.OrdinalIgnoreCase) && session.BothClientsConnected)
{
    mirror.Start();
}

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();
builder.WebHost.UseUrls($"http://127.0.0.1:{options.DashboardPort}");

WebApplication app = builder.Build();
app.MapDualParityEndpoints(session, mirror, scenarioRunner, crawler, reportWriter);

Console.WriteLine($"[DualParity] Dashboard    : http://127.0.0.1:{options.DashboardPort}");
Console.WriteLine("[DualParity] Press Ctrl+C to stop and auto-export reports.");

await app.RunAsync();

if (session.GetSteps().Count > 0 || session.GetNpcScorecard().Count > 0)
{
    ExportedReportPaths finalPaths = await reportWriter.ExportAsync(session);
    Console.WriteLine($"[DualParity] Auto-exported reports to: {finalPaths.OutputDirectory}");
}

return 0;
