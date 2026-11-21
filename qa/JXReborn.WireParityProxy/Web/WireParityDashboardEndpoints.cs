using System.Collections.Concurrent;
using JXReborn.DualParityServer.Diffing;
using JXReborn.DualParityServer.Standalone;
using JXReborn.WireParityProxy.Configuration;
using JXReborn.WireParityProxy.Proxy;
using JXReborn.WireParityProxy.Reporting;

namespace JXReborn.WireParityProxy.Web;

/// <summary>
/// Minimal API endpoints serving the live dashboard, findings, action timeline, report export,
/// and one-click <c>*.capture.json</c> (L3A+) export.
/// </summary>
public static class WireParityDashboardEndpoints
{
    public static void Map(
        WebApplication app,
        WireParityProxyOptions options,
        BishopShadowProxy bishopProxy,
        GameServerShadowProxy gameServerProxy,
        ConcurrentQueue<ParityFinding> globalFindings,
        StandaloneStackOrchestrator? standalone = null)
    {
        app.MapGet("/", () => Results.Content(WireParityDashboardHtml.Page, "text/html; charset=utf-8"));

        app.MapGet("/api/status", () =>
        {
            WireParitySession? session = gameServerProxy.LatestSession;
            int totalFindings = globalFindings.Count + (session?.DiffCoordinator.GetFindings().Count ?? 0);
            (ManagedServerStatus Legacy, ManagedServerStatus Reborn)? managed = standalone?.GetStatuses();
            return Results.Json(new
            {
                standaloneMode = standalone is not null,
                standaloneReady = standalone?.IsReady ?? false,
                legacyServerStatus = managed?.Legacy.State,
                rebornServerStatus = managed?.Reborn.State,
                bishopPort = bishopProxy.BoundPort,
                bishopActive = bishopProxy.ActiveConnections,
                bishopHandoffs = bishopProxy.HandoffsCompleted,
                gameServerPort = gameServerProxy.BoundPort,
                gameServerActive = gameServerProxy.ActiveConnections,
                primaryBackend = options.PrimaryBackendName,
                shadowBackend = options.ShadowBackendName,
                sessionId = session?.SessionId,
                roleName = session?.RoleName,
                c2sFrames = session?.C2sFrames ?? 0,
                primaryS2cFrames = session?.PrimaryS2cFrames ?? 0,
                shadowS2cFrames = session?.ShadowS2cFrames ?? 0,
                findingsCount = totalFindings,
                protocolViolations = session?.ProtocolGate.ViolationCount ?? 0,
                isDrifted = session?.DiffCoordinator.LatestDrift.IsDrifted ?? false,
                driftSummary = session?.DiffCoordinator.LatestDrift.Summary ?? "Idle"
            });
        });

        app.MapGet("/api/findings", () =>
        {
            var all = new List<ParityFinding>(globalFindings);
            WireParitySession? session = gameServerProxy.LatestSession;
            if (session is not null)
            {
                all.AddRange(session.DiffCoordinator.GetFindings());
            }

            return Results.Json(all);
        });

        app.MapGet("/api/steps", () =>
        {
            WireParitySession? session = gameServerProxy.LatestSession;
            return Results.Json(session?.DiffCoordinator.GetSteps() ?? []);
        });

        app.MapPost("/api/drift/reset", () =>
        {
            gameServerProxy.LatestSession?.DiffCoordinator.ResetDrift();
            return Results.Json(new { ok = true });
        });

        app.MapPost("/api/report/export", async (CancellationToken ct) =>
        {
            WireParitySession? session = gameServerProxy.LatestSession;
            if (session is null)
            {
                return Results.BadRequest(new { error = "No active GameServer session yet." });
            }

            string dir = WireParityReportWriter.WriteAll(session, options.ReportOutputDir, globalFindings.ToArray());
            if (standalone is not null)
            {
                await standalone.WriteServerLogsAsync(dir, ct).ConfigureAwait(false);
            }

            return Results.Json(new { path = dir });
        });

        app.MapPost("/api/capture/export/{stepIndex:int}", (int stepIndex) =>
        {
            WireParitySession? session = gameServerProxy.LatestSession;
            if (session is null)
            {
                return Results.BadRequest(new { error = "No active GameServer session yet." });
            }

            string captureId = $"WPP_{session.SessionId.Replace("-", "_", StringComparison.Ordinal)}_S{stepIndex:D3}";
            if (captureId.Length > 79)
            {
                captureId = captureId[..79];
            }

            string outDir = Path.Combine(options.ReportOutputDir, "captures");
            string filePath = L3aCaptureExporter.ExportActionWindow(session, stepIndex, captureId, outDir);
            return Results.Json(new { path = filePath, captureId });
        });
    }
}
