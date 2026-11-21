namespace JXReborn.DualParityServer.Reporting;

/// <summary>
/// Paths of all report files generated for a Dual-Run Parity session.
/// </summary>
public sealed record ExportedReportPaths(
    string OutputDirectory,
    string FindingsJsonlPath,
    string MarkdownReportPath,
    string HtmlReportPath);
