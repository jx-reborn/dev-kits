using JXReborn.DualParityServer.Diffing;
using JXReborn.WireParityProxy.ShadowStates;

namespace JXReborn.WireParityProxy.Diffing;

/// <summary>
/// Detects cumulative state drift between Primary (<c>legacy-linux</c>) and Shadow (<c>reborn-server</c>)
/// so QA reports clearly distinguish root-cause bugs from downstream cascade drift.
/// </summary>
public sealed class StateDriftDetector
{
    private readonly int _positionToleranceMps;
    private readonly int _criticalPositionDriftMps;
    private int _consecutiveDialogDesyncs;

    public StateDriftDetector(int positionToleranceMps = 64, int criticalPositionDriftMps = 256)
    {
        _positionToleranceMps = Math.Max(0, positionToleranceMps);
        _criticalPositionDriftMps = Math.Max(_positionToleranceMps, criticalPositionDriftMps);
    }

    public DriftAssessment Evaluate(ProxyShadowClientState primary, ProxyShadowClientState shadow)
    {
        ArgumentNullException.ThrowIfNull(primary);
        ArgumentNullException.ThrowIfNull(shadow);

        bool mapMismatch = primary.MapId > 0 && shadow.MapId > 0 && primary.MapId != shadow.MapId;

        long dx = (long)primary.MpsX - shadow.MpsX;
        long dy = (long)primary.MpsY - shadow.MpsY;
        double dist = Math.Sqrt((dx * dx) + (dy * dy));

        bool dialogMismatch = primary.Dialog.IsOpen != shadow.Dialog.IsOpen;
        int dialogDesyncs = dialogMismatch
            ? Interlocked.Increment(ref _consecutiveDialogDesyncs)
            : Interlocked.Exchange(ref _consecutiveDialogDesyncs, 0);

        if (mapMismatch)
        {
            return new DriftAssessment(
                IsDrifted: true,
                Severity: FindingSeverity.Critical,
                PositionDistanceMps: dist,
                MapMismatch: true,
                ConsecutiveDialogDesyncCount: dialogDesyncs,
                Summary: $"MapId drifted: Primary={primary.MapId} vs Shadow={shadow.MapId}");
        }

        if (dist > _criticalPositionDriftMps)
        {
            return new DriftAssessment(
                IsDrifted: true,
                Severity: FindingSeverity.Critical,
                PositionDistanceMps: dist,
                MapMismatch: false,
                ConsecutiveDialogDesyncCount: dialogDesyncs,
                Summary: $"Critical position drift: {dist:F1} mps (> {_criticalPositionDriftMps})");
        }

        if (dialogDesyncs >= 2)
        {
            return new DriftAssessment(
                IsDrifted: true,
                Severity: FindingSeverity.Warning,
                PositionDistanceMps: dist,
                MapMismatch: false,
                ConsecutiveDialogDesyncCount: dialogDesyncs,
                Summary: $"Dialog open state desynced for {dialogDesyncs} consecutive steps");
        }

        if (dist > _positionToleranceMps)
        {
            return new DriftAssessment(
                IsDrifted: false,
                Severity: FindingSeverity.Warning,
                PositionDistanceMps: dist,
                MapMismatch: false,
                ConsecutiveDialogDesyncCount: dialogDesyncs,
                Summary: $"Minor position offset: {dist:F1} mps");
        }

        return new DriftAssessment(
            IsDrifted: false,
            Severity: FindingSeverity.Info,
            PositionDistanceMps: dist,
            MapMismatch: false,
            ConsecutiveDialogDesyncCount: dialogDesyncs,
            Summary: "In sync");
    }

    public void Reset() => Interlocked.Exchange(ref _consecutiveDialogDesyncs, 0);
}
