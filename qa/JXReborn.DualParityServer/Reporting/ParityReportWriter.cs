using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using JXReborn.DualParityServer.Diffing;
using JXReborn.DualParityServer.Session;
using JXReborn.DualParityServer.Standalone;

namespace JXReborn.DualParityServer.Reporting;

/// <summary>
/// Generates AI-Native (<c>findings.jsonl</c>) and human-readable (<c>report.md</c>, <c>report.html</c>)
/// parity reports from a <see cref="DualSessionManager"/> run.
/// </summary>
public sealed class ParityReportWriter
{
    private static readonly JsonSerializerOptions JsonLineOptions = new()
    {
        WriteIndented = false,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public async Task<ExportedReportPaths> ExportAsync(
        DualSessionManager session,
        string? outputDirectoryOverride = null,
        StandaloneStackOrchestrator? standalone = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(session);

        string outDir = outputDirectoryOverride
            ?? Path.Combine(session.Options.ReportOutputDir, session.SessionId);
        Directory.CreateDirectory(outDir);

        IReadOnlyList<StepComparisonResult> steps = session.GetSteps();
        IReadOnlyList<ParityFinding> findings = session.GetFindings();
        IReadOnlyList<NpcParityScoreEntry> npcScores = session.GetNpcScorecard();

        string jsonlPath = Path.Combine(outDir, "findings.jsonl");
        string mdPath = Path.Combine(outDir, "report.md");
        string htmlPath = Path.Combine(outDir, "report.html");

        await WriteFindingsJsonlAsync(jsonlPath, findings, cancellationToken);
        await File.WriteAllTextAsync(mdPath, BuildMarkdownReport(session, steps, findings, npcScores), Encoding.UTF8, cancellationToken);
        await File.WriteAllTextAsync(htmlPath, BuildHtmlReport(session, steps, findings, npcScores), Encoding.UTF8, cancellationToken);

        if (standalone is not null)
        {
            await standalone.WriteServerLogsAsync(outDir, cancellationToken);
        }

        return new ExportedReportPaths(outDir, jsonlPath, mdPath, htmlPath);
    }

    private static async Task WriteFindingsJsonlAsync(
        string path,
        IReadOnlyList<ParityFinding> findings,
        CancellationToken cancellationToken)
    {
        var sb = new StringBuilder();
        foreach (ParityFinding finding in findings)
        {
            sb.AppendLine(JsonSerializer.Serialize(finding, JsonLineOptions));
        }

        await File.WriteAllTextAsync(path, sb.ToString(), Encoding.UTF8, cancellationToken);
    }

    private static string BuildMarkdownReport(
        DualSessionManager session,
        IReadOnlyList<StepComparisonResult> steps,
        IReadOnlyList<ParityFinding> findings,
        IReadOnlyList<NpcParityScoreEntry> npcScores)
    {
        int passedSteps = steps.Count(s => s.Passed);
        int failedSteps = steps.Count - passedSteps;
        int criticalCount = findings.Count(f => f.Severity == FindingSeverity.Critical);
        int warningCount = findings.Count(f => f.Severity == FindingSeverity.Warning);
        double passRate = steps.Count == 0 ? 100.0 : (passedSteps * 100.0) / steps.Count;

        var sb = new StringBuilder();
        sb.AppendLine(CultureInfo.InvariantCulture, $"# JXReborn Dual-Run Parity Report (`{session.SessionId}`)");
        sb.AppendLine();
        sb.AppendLine(CultureInfo.InvariantCulture, $"- **Started (UTC):** `{session.StartedAtUtc:O}`");
        sb.AppendLine(CultureInfo.InvariantCulture, $"- **Generated (UTC):** `{DateTimeOffset.UtcNow:O}`");
        sb.AppendLine(CultureInfo.InvariantCulture, $"- **Legacy Target:** `{session.Options.LegacyHost}:{session.Options.LegacyPort}`");
        sb.AppendLine(CultureInfo.InvariantCulture, $"- **Reborn Target:** `{session.Options.RebornHost}:{session.Options.RebornPort}`");
        sb.AppendLine(CultureInfo.InvariantCulture, $"- **Steps:** `{steps.Count}` total (`{passedSteps}` passed, `{failedSteps}` failed — **{passRate:F1}%**)");
        sb.AppendLine(CultureInfo.InvariantCulture, $"- **Findings:** `{criticalCount}` CRITICAL, `{warningCount}` WARNING (`{findings.Count}` total)");
        sb.AppendLine();

        if (npcScores.Count > 0)
        {
            int matchedNpcs = npcScores.Count(n => n.Matched);
            sb.AppendLine(CultureInfo.InvariantCulture, $"## NPC Parity Scorecard ({matchedNpcs}/{npcScores.Count} matched)");
            sb.AppendLine();
            sb.AppendLine("| Map | NPC | Pos | Branches | Critical | Warning | Status |");
            sb.AppendLine("|---:|---|---|---:|---:|---:|---|");
            foreach (NpcParityScoreEntry npc in npcScores)
            {
                string status = npc.Matched ? "PASS" : "FAIL";
                sb.AppendLine(CultureInfo.InvariantCulture, $"| {npc.MapId} | {EscapeMd(npc.NpcName)} | ({npc.MpsX}, {npc.MpsY}) | {npc.BranchesTested} | {npc.CriticalFindings} | {npc.WarningFindings} | **{status}** — {EscapeMd(npc.Summary)} |");
            }

            sb.AppendLine();
        }

        if (findings.Count > 0)
        {
            sb.AppendLine("## Grouped Findings (Clustered for Triage / AI Agent)");
            sb.AppendLine();
            foreach (IGrouping<string, ParityFinding> cluster in findings
                .GroupBy(f => f.ClusterKey, StringComparer.Ordinal)
                .OrderByDescending(g => g.Max(x => x.Severity))
                .ThenBy(g => g.Key, StringComparer.Ordinal))
            {
                ParityFinding sample = cluster.First();
                sb.AppendLine(CultureInfo.InvariantCulture, $"### [{sample.Severity}] `{cluster.Key}` (x{cluster.Count()})");
                sb.AppendLine(CultureInfo.InvariantCulture, $"- **Step:** #{sample.StepIndex} `{sample.StepName}`");
                sb.AppendLine(CultureInfo.InvariantCulture, $"- **Field:** `{sample.FieldPath}`");
                sb.AppendLine(CultureInfo.InvariantCulture, $"- **Legacy:** `{EscapeMd(sample.LegacyValue)}`");
                sb.AppendLine(CultureInfo.InvariantCulture, $"- **Reborn:** `{EscapeMd(sample.RebornValue)}`");
                sb.AppendLine(CultureInfo.InvariantCulture, $"- **Repro:** Map `{sample.Repro.MapId}` @ `({sample.Repro.PlayerPos[0]}, {sample.Repro.PlayerPos[1]})` | Actions: `{string.Join(" -> ", sample.Repro.Actions)}`");
                sb.AppendLine();
            }
        }
        else
        {
            sb.AppendLine("## Findings");
            sb.AppendLine();
            sb.AppendLine("No parity divergences detected.");
            sb.AppendLine();
        }

        return sb.ToString();
    }

    private static string BuildHtmlReport(
        DualSessionManager session,
        IReadOnlyList<StepComparisonResult> steps,
        IReadOnlyList<ParityFinding> findings,
        IReadOnlyList<NpcParityScoreEntry> npcScores)
    {
        int passedSteps = steps.Count(s => s.Passed);
        int criticalCount = findings.Count(f => f.Severity == FindingSeverity.Critical);
        int warningCount = findings.Count(f => f.Severity == FindingSeverity.Warning);

        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html><html lang=\"en\"><head><meta charset=\"utf-8\">");
        sb.AppendLine(CultureInfo.InvariantCulture, $"<title>JXReborn Dual Parity Report — {Enc(session.SessionId)}</title>");
        sb.AppendLine("<style>");
        sb.AppendLine("body{font-family:system-ui,-apple-system,Segoe UI,Roboto,sans-serif;background:#0f172a;color:#e2e8f0;margin:0;padding:24px;}");
        sb.AppendLine("h1,h2{margin:0 0 12px;} .grid{display:grid;grid-template-columns:repeat(4,minmax(140px,1fr));gap:12px;margin:16px 0 24px;}");
        sb.AppendLine(".card{background:#1e293b;border:1px solid #334155;border-radius:10px;padding:14px;} .num{font-size:26px;font-weight:700;}");
        sb.AppendLine(".crit{color:#f87171;} .warn{color:#fbbf24;} .ok{color:#34d399;}");
        sb.AppendLine("table{width:100%;border-collapse:collapse;margin-bottom:24px;background:#1e293b;border-radius:10px;overflow:hidden;}");
        sb.AppendLine("th,td{padding:10px 12px;border-bottom:1px solid #334155;text-align:left;font-size:13px;vertical-align:top;}");
        sb.AppendLine("th{background:#0b1220;color:#94a3b8;text-transform:uppercase;font-size:11px;letter-spacing:.05em;}");
        sb.AppendLine("code{background:#0b1220;padding:2px 6px;border-radius:4px;font-size:12px;color:#93c5fd;}");
        sb.AppendLine(".badge{padding:2px 8px;border-radius:999px;font-weight:600;font-size:11px;}");
        sb.AppendLine(".b-crit{background:#7f1d1d;color:#fecaca;} .b-warn{background:#78350f;color:#fde68a;} .b-ok{background:#064e3b;color:#a7f3d0;}");
        sb.AppendLine("</style></head><body>");

        sb.AppendLine(CultureInfo.InvariantCulture, $"<h1>JXReborn Dual-Run Parity Report</h1><p>Session: <code>{Enc(session.SessionId)}</code> | Legacy: <code>{Enc(session.Options.LegacyHost)}:{session.Options.LegacyPort}</code> | Reborn: <code>{Enc(session.Options.RebornHost)}:{session.Options.RebornPort}</code></p>");
        sb.AppendLine("<div class=\"grid\">");
        sb.AppendLine(CultureInfo.InvariantCulture, $"<div class=\"card\"><div>Total Steps</div><div class=\"num\">{steps.Count}</div></div>");
        sb.AppendLine(CultureInfo.InvariantCulture, $"<div class=\"card\"><div>Passed Steps</div><div class=\"num ok\">{passedSteps}</div></div>");
        sb.AppendLine(CultureInfo.InvariantCulture, $"<div class=\"card\"><div>Critical Findings</div><div class=\"num crit\">{criticalCount}</div></div>");
        sb.AppendLine(CultureInfo.InvariantCulture, $"<div class=\"card\"><div>Warnings</div><div class=\"num warn\">{warningCount}</div></div>");
        sb.AppendLine("</div>");

        if (npcScores.Count > 0)
        {
            sb.AppendLine("<h2>NPC Parity Scorecard</h2><table><thead><tr><th>Map</th><th>NPC</th><th>Position</th><th>Branches</th><th>Critical</th><th>Warning</th><th>Status</th></tr></thead><tbody>");
            foreach (NpcParityScoreEntry npc in npcScores)
            {
                string badge = npc.Matched ? "<span class=\"badge b-ok\">PASS</span>" : "<span class=\"badge b-crit\">FAIL</span>";
                sb.AppendLine(CultureInfo.InvariantCulture, $"<tr><td>{npc.MapId}</td><td>{Enc(npc.NpcName)}</td><td>({npc.MpsX}, {npc.MpsY})</td><td>{npc.BranchesTested}</td><td>{npc.CriticalFindings}</td><td>{npc.WarningFindings}</td><td>{badge} {Enc(npc.Summary)}</td></tr>");
            }

            sb.AppendLine("</tbody></table>");
        }

        sb.AppendLine("<h2>Findings</h2><table><thead><tr><th>ID</th><th>Severity</th><th>Category</th><th>Step</th><th>Field</th><th>Legacy</th><th>Reborn</th></tr></thead><tbody>");
        foreach (ParityFinding f in findings)
        {
            string badge = f.Severity == FindingSeverity.Critical
                ? "<span class=\"badge b-crit\">CRITICAL</span>"
                : "<span class=\"badge b-warn\">WARNING</span>";
            sb.AppendLine(CultureInfo.InvariantCulture, $"<tr><td><code>{Enc(f.FindingId)}</code></td><td>{badge}</td><td>{f.Category}</td><td>#{f.StepIndex} {Enc(f.StepName)}</td><td><code>{Enc(f.FieldPath)}</code></td><td>{Enc(f.LegacyValue)}</td><td>{Enc(f.RebornValue)}</td></tr>");
        }

        if (findings.Count == 0)
        {
            sb.AppendLine("<tr><td colspan=\"7\" class=\"ok\">No parity divergences detected.</td></tr>");
        }

        sb.AppendLine("</tbody></table></body></html>");
        return sb.ToString();
    }

    private static string EscapeMd(string value) =>
        value.Replace("|", "\\|", StringComparison.Ordinal).Replace("\n", " ", StringComparison.Ordinal);

    private static string Enc(string value) => WebUtility.HtmlEncode(value);
}
