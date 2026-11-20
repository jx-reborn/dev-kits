namespace JXReborn.ClientDriver.Tests.DifferentialParity;

/// <summary>
/// Execution mode for differential parity suites: live dual-run, live record-to-baseline, or offline baseline replay.
/// </summary>
public enum ParityExecutionMode
{
    Live = 0,
    Record = 1,
    Replay = 2
}
