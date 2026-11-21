using JXReborn.DualParityServer.Diffing;

namespace JXReborn.WireParityProxy.Diffing;

/// <summary>
/// Result of evaluating state divergence (drift) between Primary and Shadow states.
/// </summary>
public sealed record DriftAssessment(
    bool IsDrifted,
    FindingSeverity Severity,
    double PositionDistanceMps,
    bool MapMismatch,
    int ConsecutiveDialogDesyncCount,
    string Summary);
