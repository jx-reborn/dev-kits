using JXReborn.ZoneEngine.PlayerSessions;
using JXReborn.ZoneEngine.WorldZones;

namespace JXReborn.ClientDriver.Tests;

/// <summary>
/// Test-only <see cref="IZoneCommand"/> that executes <see cref="MapZoneWorker.TeleportPlayerAsync"/>
/// strictly on the owning <see cref="MapZoneWorker"/> single-reader loop rather than from the xUnit thread.
/// </summary>
internal sealed record WorkerSafeTeleportZoneCommand(
    PlayerZoneState Player,
    ushort TargetMapId,
    int TargetMpsX,
    int TargetMpsY,
    byte TargetFightMode,
    TaskCompletionSource<bool> Completion) : IZoneCommand;
