using JXReborn.TestBase.WireCaptures;
using JXReborn.WireParityProxy.Diffing;
using JXReborn.WireParityProxy.Proxy;
using JXReborn.WireParityProxy.Wire;

namespace JXReborn.WireParityProxy.Reporting;

/// <summary>
/// Exports a captured <see cref="WireActionWindow"/> from a live proxy session into a cryptographically
/// signed <c>*.capture.json</c> document compatible with <see cref="WireCaptureStore"/> (L3A+ offline replay).
/// </summary>
public static class L3aCaptureExporter
{
    public static string ExportActionWindow(
        WireParitySession session,
        int stepIndex,
        string captureId,
        string outputDirectory,
        string oracleProfile = "v1")
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentException.ThrowIfNullOrWhiteSpace(captureId);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputDirectory);

        if (!session.DiffCoordinator.TryGetCapturedWindow(
                stepIndex,
                out (WireActionWindow Window, IReadOnlyList<JxWireFrame> PrimaryS2c, IReadOnlyList<JxWireFrame> ShadowS2c) entry))
        {
            throw new KeyNotFoundException($"Action window step {stepIndex} was not found in session '{session.SessionId}'.");
        }

        var actor = new WireCaptureActorDefinition(
            ActorId: "P1",
            AccountName: "gm_wire_parity",
            RoleName: string.IsNullOrWhiteSpace(session.RoleName) ? "HeroWireParity01" : session.RoleName,
            Gender: 0,
            Series: session.PrimaryState.Series,
            NativePlaceId: (ushort)Math.Clamp(session.PrimaryState.MapId, 0, ushort.MaxValue),
            GmMenuSelections: [],
            SettleTicksAfterSetup: 2);

        var symbols = new Dictionary<string, WireCaptureSymbolDefinition>(StringComparer.Ordinal)
        {
            ["$actor:P1"] = new WireCaptureSymbolDefinition(
                Kind: WireCaptureSymbolKind.PlayerSelf,
                ActorId: "P1",
                RecordedDwId: session.PrimaryState.SelfNpcId)
        };

        var c2sFrames = new List<WireCaptureC2sFrame>
        {
            new(
                TickOffsetFrames: 0,
                Opcode: entry.Window.PrimaryC2s.Opcode,
                RawHex: entry.Window.PrimaryC2s.ToHexString(),
                Patches: [])
        };

        var expectedS2c = new List<WireCaptureExpectedS2cEvent>(entry.PrimaryS2c.Count);
        foreach (JxWireFrame s2c in entry.PrimaryS2c)
        {
            int tickOffset = Math.Max(0, (int)Math.Round((s2c.TimestampUtc - entry.Window.OpenedAtUtc).TotalMilliseconds / 55.555));
            expectedS2c.Add(new WireCaptureExpectedS2cEvent(
                Opcode: s2c.Opcode,
                Name: $"S2C_0x{s2c.Opcode:X2}",
                ExactByteLength: s2c.DecryptedPayload.Length,
                ExpectedTickOffsetFrames: tickOffset,
                TickToleranceFrames: 4,
                FieldAssertions: []));
        }

        var step = new WireCaptureStep(
            StepId: entry.Window.ActionLabel,
            ActorId: "P1",
            UserActionDescription: $"Exported from WireParityProxy session {session.SessionId} ({entry.Window.ActionLabel})",
            C2sFrames: c2sFrames,
            ObservationTicks: 8,
            ExpectedS2cContract: expectedS2c,
            ExpectedActorStateAfter: new Dictionary<string, WireCaptureActorStateExpectation>(StringComparer.Ordinal));

        WireCaptureDocument doc = WireCaptureStore.CreateSignedDocument(
            captureId: captureId,
            title: $"WireParityProxy export {entry.Window.ActionLabel}",
            oracleProfile: oracleProfile,
            oraclePackageArchiveSha256: null,
            actors: [actor],
            symbols: symbols,
            steps: [step]);

        return WireCaptureStore.Save(doc, outputDirectory);
    }
}
