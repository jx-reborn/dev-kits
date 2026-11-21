using JXReborn.DualParityServer.Diffing;
using JXReborn.TestBase.WireSimulators;

namespace JXReborn.WireParityProxy.Diffing;

/// <summary>
/// Layer A gate wrapping <see cref="GameExeProtocolVerifier"/> for the Shadow backend's S2C stream.
/// Converts protocol violations into structured <see cref="ParityFinding"/> entries without disconnecting the client.
/// </summary>
public sealed class ProtocolSafetyGate
{
    private readonly GameExeProtocolVerifier _verifier = new();
    private readonly Lock _sync = new();
    private int _violationSequence;

    public bool IsWorldLoaded
    {
        get
        {
            lock (_sync)
            {
                return _verifier.IsWorldLoaded;
            }
        }
    }

    public bool IsSyncCompleted
    {
        get
        {
            lock (_sync)
            {
                return _verifier.IsSyncCompleted;
            }
        }
    }

    public int ViolationCount => Volatile.Read(ref _violationSequence);

    public ParityFinding? ObserveShadowS2c(
        ReadOnlySpan<byte> decryptedPayload,
        int stepIndex,
        int mapId,
        int playerMpsX,
        int playerMpsY,
        string stepName)
    {
        lock (_sync)
        {
            try
            {
                _verifier.ObserveGameServerS2CPayload(decryptedPayload);
                return null;
            }
            catch (GameExeProtocolViolationException ex)
            {
                int seq = Interlocked.Increment(ref _violationSequence);
                byte opcode = decryptedPayload.IsEmpty ? (byte)0 : decryptedPayload[0];
                string hex = Convert.ToHexString(decryptedPayload[..Math.Min(decryptedPayload.Length, 64)]);
                DateTimeOffset now = DateTimeOffset.UtcNow;

                return new ParityFinding(
                    FindingId: $"PROTO-{now:yyyyMMdd}-{seq:D4}",
                    ClusterKey: $"WireProtocol::Map{mapId}::S2C_0x{opcode:X2}",
                    Severity: FindingSeverity.Critical,
                    Category: FindingCategory.WireProtocol,
                    StepIndex: stepIndex,
                    StepName: stepName,
                    FieldPath: $"S2C[0x{opcode:X2}]",
                    LegacyValue: "Valid game.exe wire protocol contract",
                    RebornValue: $"{ex.Message} (len={decryptedPayload.Length}, hex={hex})",
                    Repro: new FindingReproContext(mapId, [playerMpsX, playerMpsY], null, [stepName]),
                    StateDelta: new FindingStateDelta(0, 0, 0, 0, 0, 0, mapId, mapId),
                    LegacyRecentS2cOpcodes: [],
                    RebornRecentS2cOpcodes: [$"0x{opcode:X2}"],
                    Artifacts: new FindingArtifacts(),
                    TimestampUtc: now);
            }
        }
    }
}
