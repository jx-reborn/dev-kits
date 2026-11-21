using JXReborn.ClientDriver;
using JXReborn.DualParityServer.Automation;
using JXReborn.DualParityServer.Diffing;
using JXReborn.DualParityServer.Mirroring;
using JXReborn.DualParityServer.Reporting;
using JXReborn.DualParityServer.Session;
using JXReborn.DualParityServer.Standalone;

namespace JXReborn.DualParityServer.Web;

/// <summary>
/// Minimal API route mappings for the Dual-Run Parity Web Dashboard.
/// </summary>
public static class DashboardEndpoints
{
    public static void MapDualParityEndpoints(
        this WebApplication app,
        DualSessionManager session,
        InteractiveMirrorEngine mirror,
        ScenarioRunner scenarioRunner,
        NpcDialogCrawler crawler,
        ParityReportWriter reportWriter,
        StandaloneStackOrchestrator? standalone = null)
    {
        app.MapGet("/", () => Results.Content(DashboardHtml.Page, "text/html; charset=utf-8"));

        app.MapGet("/api/status", () =>
        {
            IReadOnlyList<StepComparisonResult> steps = session.GetSteps();
            IReadOnlyList<ParityFinding> findings = session.GetFindings();
            (ManagedServerStatus Legacy, ManagedServerStatus Reborn)? managed = standalone?.GetStatuses();
            return Results.Ok(new
            {
                sessionId = session.SessionId,
                startedAtUtc = session.StartedAtUtc,
                standaloneEnabled = standalone is not null,
                bothConnected = session.BothClientsConnected,
                mirrorRunning = mirror.IsRunning,
                mirrorStatus = mirror.StatusMessage,
                totalSteps = steps.Count,
                passedSteps = steps.Count(s => s.Passed),
                criticalCount = findings.Count(f => f.Severity == FindingSeverity.Critical),
                warningCount = findings.Count(f => f.Severity == FindingSeverity.Warning),
                legacyProxy = session.GetLegacyProxyStats(),
                rebornProxy = session.GetRebornProxyStats(),
                legacy = BuildTargetSummary(session.LegacyClient, session.LastLegacySnapshot),
                reborn = BuildTargetSummary(session.RebornClient, session.LastRebornSnapshot),
                managedServers = managed is null
                    ? null
                    : new { legacy = managed.Value.Legacy, reborn = managed.Value.Reborn }
            });
        });

        app.MapGet("/api/steps", () => Results.Ok(session.GetSteps()));
        app.MapGet("/api/findings", () => Results.Ok(session.GetFindings()));
        app.MapGet("/api/npcs", () => Results.Ok(session.GetNpcScorecard()));

        app.MapPost("/api/clients/attach-pid", async (AttachPidRequest request, CancellationToken ct) =>
        {
            try
            {
                await session.AttachByPidAsync(request.LegacyPid, request.RebornPid, ct);
                return Results.Ok(new { message = $"Attached Legacy PID {request.LegacyPid} & Reborn PID {request.RebornPid}." });
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        });

        app.MapPost("/api/clients/attach-pipe", async (AttachPipeRequest request, CancellationToken ct) =>
        {
            try
            {
                await session.AttachByPipeAsync(request.LegacyPipe, request.RebornPipe, ct);
                return Results.Ok(new { message = $"Connected pipes '{request.LegacyPipe}' & '{request.RebornPipe}'." });
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        });

        app.MapPost("/api/mirror/start", () =>
        {
            try
            {
                mirror.Start();
                return Results.Ok(new { message = "Interactive Mirror started." });
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        });

        app.MapPost("/api/mirror/stop", async () =>
        {
            await mirror.StopAsync();
            return Results.Ok(new { message = "Interactive Mirror stopped." });
        });

        app.MapPost("/api/diff/now", async (CancellationToken ct) =>
        {
            try
            {
                StepComparisonResult result = await session.CompareCurrentStateAsync(
                    stepName: "Manual Snapshot Diff",
                    actionSummary: "ManualDiff()",
                    cancellationToken: ct);
                return Results.Ok(new
                {
                    message = result.Passed
                        ? "Snapshot matched (0 critical)."
                        : $"Divergence detected: {result.CriticalCount} critical, {result.WarningCount} warning.",
                    result
                });
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        });

        app.MapPost("/api/crawl/npcs", async (CrawlNpcsRequest? request, CancellationToken ct) =>
        {
            try
            {
                IReadOnlyList<NpcParityScoreEntry> scores = await crawler.CrawlNearbyNpcsAsync(
                    maxBranchesPerNpc: request?.MaxBranchesPerNpc ?? 0,
                    npcNameFilter: request?.NpcNameFilter,
                    cancellationToken: ct);
                int matched = scores.Count(s => s.Matched);
                return Results.Ok(new
                {
                    message = $"Crawled {scores.Count} NPCs ({matched} matched, {scores.Count - matched} diverged).",
                    scores
                });
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        });

        app.MapPost("/api/scenario/run", async (ParityScenario scenario, CancellationToken ct) =>
        {
            try
            {
                IReadOnlyList<StepComparisonResult> results = await scenarioRunner.RunAsync(scenario, ct);
                int passed = results.Count(r => r.Passed);
                return Results.Ok(new
                {
                    message = $"Scenario '{scenario.Name}' finished: {passed}/{results.Count} steps passed.",
                    results
                });
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        });

        app.MapPost("/api/report/export", async (CancellationToken ct) =>
        {
            ExportedReportPaths paths = await reportWriter.ExportAsync(session, standalone: standalone, cancellationToken: ct);
            return Results.Ok(new { message = "Reports exported.", paths });
        });
    }

    private static object BuildTargetSummary(IDualTargetClient? client, WorldDomSnapshot? snap)
    {
        if (client is null)
        {
            return new { connected = false };
        }

        return new
        {
            connected = true,
            pipeName = client.PipeName,
            pid = client.ProcessId,
            itemCount = snap?.Items.Count ?? 0,
            npcCount = snap?.Npcs.Count ?? 0,
            player = snap is null
                ? null
                : new
                {
                    name = snap.Player.Name,
                    level = snap.Player.Level,
                    mapId = snap.Player.MapId,
                    mpsX = snap.Player.MpsX,
                    mpsY = snap.Player.MpsY,
                    money = snap.Player.Money,
                    exp = snap.Player.Exp
                },
            dialog = snap is null
                ? null
                : new
                {
                    isOpen = snap.Dialog.IsOpen,
                    isTalk = snap.Dialog.IsTalk,
                    prompt = snap.Dialog.Prompt,
                    options = snap.Dialog.Options
                }
        };
    }

    private sealed record AttachPidRequest(int LegacyPid, int RebornPid);

    private sealed record AttachPipeRequest(string LegacyPipe, string RebornPipe);

    private sealed record CrawlNpcsRequest(string? NpcNameFilter, int MaxBranchesPerNpc);
}
