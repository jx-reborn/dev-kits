using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;
using JXReborn.DualParityServer.Diffing;
using JXReborn.WireParityProxy.Diffing;
using JXReborn.WireParityProxy.Proxy;

namespace JXReborn.WireParityProxy.Reporting;

/// <summary>
/// Exports live wire-parity session results into <c>findings.jsonl</c>, <c>report.md</c>, and <c>report.html</c>.
/// </summary>
public static class WireParityReportWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = false
    };

    public static string WriteAll(
        WireParitySession session,
        string outputRootDir,
        IReadOnlyList<ParityFinding>? extraFindings = null)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputRootDir);

        string sessionDir = Path.Combine(outputRootDir, session.SessionId);
        Directory.CreateDirectory(sessionDir);

        IReadOnlyList<WireStepReport> steps = session.DiffCoordinator.GetSteps();
        var findings = new List<ParityFinding>();
        if (extraFindings is not null)
        {
            findings.AddRange(extraFindings);
        }

        findings.AddRange(session.DiffCoordinator.GetFindings());

        WriteFindingsJsonl(Path.Combine(sessionDir, "findings.jsonl"), findings);
        WriteMarkdownReport(Path.Combine(sessionDir, "report.md"), session, steps, findings);
        WriteHtmlReport(Path.Combine(sessionDir, "report.html"), session, steps, findings);

        return sessionDir;
    }

    private static void WriteFindingsJsonl(string path, IReadOnlyList<ParityFinding> findings)
    {
        var sb = new StringBuilder();
        foreach (ParityFinding finding in findings)
        {
            sb.AppendLine(JsonSerializer.Serialize(finding, JsonOptions));
        }

        File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
    }

    private static void WriteMarkdownReport(
        string path,
        WireParitySession session,
        IReadOnlyList<WireStepReport> steps,
        IReadOnlyList<ParityFinding> findings)
    {
        int passed = steps.Count(s => s.Passed);
        int critical = findings.Count(f => f.Severity == FindingSeverity.Critical);
        int warning = findings.Count(f => f.Severity == FindingSeverity.Warning);

        var sb = new StringBuilder();
        sb.AppendLine($"# Wire Parity Report — `{session.SessionId}`");
        sb.AppendLine();
        sb.AppendLine($"- **Role**: `{session.RoleName}`");
        sb.AppendLine($"- **Started (UTC)**: `{session.StartedAtUtc:O}`");
        sb.AppendLine($"- **Traffic**: C2S={session.C2sFrames}, Primary S2C={session.PrimaryS2cFrames}, Shadow S2C={session.ShadowS2cFrames}");
        sb.AppendLine($"- **Action Windows**: {steps.Count} total ({passed} passed, {steps.Count - passed} diverged)");
        sb.AppendLine($"- **Findings**: {findings.Count} total ({critical} Critical, {warning} Warning)");
        sb.AppendLine($"- **Protocol Violations (Layer A)**: {session.ProtocolGate.ViolationCount}");
        sb.AppendLine($"- **Latest Drift**: `{session.DiffCoordinator.LatestDrift.Summary}`");
        sb.AppendLine();

        sb.AppendLine("## Action Timeline");
        sb.AppendLine();
        sb.AppendLine("| Step | Action | C2S | Rewritten | Primary S2C | Shadow S2C | Drift | Findings |");
        sb.AppendLine("| ---: | :--- | :--- | :---: | :--- | :--- | :--- | ---: |");
        foreach (WireStepReport step in steps)
        {
            string pOps = string.Join(", ", step.PrimaryS2cOpcodes);
            string sOps = string.Join(", ", step.ShadowS2cOpcodes);
            sb.AppendLine(
                $"| {step.StepIndex} | `{step.ActionLabel}` | `0x{step.C2sOpcode:X2}` | {(step.C2sWasRewritten ? "Yes" : "No")} | `{pOps}` | `{sOps}` | {step.Drift.Summary} | {step.Findings.Count} |");
        }

        if (findings.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("## Findings");
            sb.AppendLine();
            sb.AppendLine("| ID | Severity | Category | Step | Field | Primary | Shadow |");
            sb.AppendLine("| :--- | :--- | :--- | ---: | :--- | :--- | :--- |");
            foreach (ParityFinding f in findings)
            {
                sb.AppendLine(
                    $"| `{f.FindingId}` | **{f.Severity}** | `{f.Category}` | {f.StepIndex} | `{f.FieldPath}` | `{f.LegacyValue}` | `{f.RebornValue}` |");
            }
        }

        File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
    }

    private static void WriteHtmlReport(
        string path,
        WireParitySession session,
        IReadOnlyList<WireStepReport> steps,
        IReadOnlyList<ParityFinding> findings)
    {
        static string Esc(string? s) => WebUtility.HtmlEncode(s ?? string.Empty);

        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html><html lang=\"en\"><head><meta charset=\"utf-8\">");
        sb.AppendLine($"<title>Wire Parity Report - {Esc(session.SessionId)}</title>");
        sb.AppendLine("<style>body{font-family:system-ui,sans-serif;background:#0f172a;color:#e2e8f0;margin:24px;}");
        sb.AppendLine("table{border-collapse:collapse;width:100%;margin-top:16px;font-size:13px;}");
        sb.AppendLine("th,td{border:1px solid #334155;padding:6px 10px;text-align:left;}th{background:#1e293b;}");
        sb.AppendLine(".crit{color:#f87171;font-weight:700;}.warn{color:#fbbf24;font-weight:700;}.ok{color:#4ade80;}</style></head><body>");
        sb.AppendLine($"<h1>JXReborn Wire Parity Report &mdash; {Esc(session.SessionId)}</h1>");
        sb.AppendLine(string.Format(
            CultureInfo.InvariantCulture,
            "<p>Role: <b>{0}</b> | C2S: {1} | Primary S2C: {2} | Shadow S2C: {3} | Findings: <b>{4}</b> | Drift: <b>{5}</b></p>",
            Esc(session.RoleName),
            session.C2sFrames,
            session.PrimaryS2cFrames,
            session.ShadowS2cFrames,
            findings.Count,
            Esc(session.DiffCoordinator.LatestDrift.Summary)));

        sb.AppendLine("<h2>Findings</h2><table><thead><tr><th>ID</th><th>Severity</th><th>Category</th><th>Step</th><th>Field</th><th>Primary</th><th>Shadow</th></tr></thead><tbody>");
        foreach (ParityFinding f in findings)
        {
            string cls = f.Severity == FindingSeverity.Critical ? "crit" : "warn";
            sb.AppendLine($"<tr><td>{Esc(f.FindingId)}</td><td class=\"{cls}\">{f.Severity}</td><td>{f.Category}</td><td>{f.StepIndex}</td><td>{Esc(f.FieldPath)}</td><td>{Esc(f.LegacyValue)}</td><td>{Esc(f.RebornValue)}</td></tr>");
        }

        sb.AppendLine("</tbody></table><h2>Action Timeline</h2><table><thead><tr><th>Step</th><th>Action</th><th>C2S</th><th>Primary S2C</th><th>Shadow S2C</th><th>Drift</th><th>Status</th></tr></thead><tbody>");
        foreach (WireStepReport step in steps)
        {
            string status = step.Passed ? "<span class=\"ok\">PASS</span>" : $"<span class=\"crit\">{step.Findings.Count} DIFF</span>";
            sb.AppendLine($"<tr><td>{step.StepIndex}</td><td>{Esc(step.ActionLabel)}</td><td>0x{step.C2sOpcode:X2}</td><td>{Esc(string.Join(", ", step.PrimaryS2cOpcodes))}</td><td>{Esc(string.Join(", ", step.ShadowS2cOpcodes))}</td><td>{Esc(step.Drift.Summary)}</td><td>{status}</td></tr>");
        }

        sb.AppendLine("</tbody></table></body></html>");
        File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
    }
}
