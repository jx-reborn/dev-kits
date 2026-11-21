using JXReborn.DualParityServer.Diffing;

namespace JXReborn.WireParityProxy.Diffing;

/// <summary>
/// Consolidated result of evaluating one C2S action window across Protocol Gate (Layer A),
/// Wire Diff (Layer B), Semantic State Diff (Layer C), and Drift Detector.
/// </summary>
public sealed record WireStepReport(
    int StepIndex,
    string ActionLabel,
    byte C2sOpcode,
    string C2sHex,
    bool C2sWasRewritten,
    int PrimaryS2cCount,
    int ShadowS2cCount,
    IReadOnlyList<string> PrimaryS2cOpcodes,
    IReadOnlyList<string> ShadowS2cOpcodes,
    DriftAssessment Drift,
    IReadOnlyList<ParityFinding> Findings,
    DateTimeOffset CompletedAtUtc)
{
    public bool Passed => Findings.Count == 0;
}
