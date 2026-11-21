namespace JXReborn.DualParityServer.Diffing;

/// <summary>
/// Result of comparing Legacy vs Reborn after a single mirrored or scripted step.
/// </summary>
public sealed record StepComparisonResult(
    int StepIndex,
    string StepName,
    string ActionSummary,
    DateTimeOffset TimestampUtc,
    bool Passed,
    int CriticalCount,
    int WarningCount,
    IReadOnlyList<ParityFinding> Findings);
