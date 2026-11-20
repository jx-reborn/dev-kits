using System.Text.Json.Serialization;

namespace JXReborn.ClientDriver;

/// <summary>
/// Full-payload C2S or S2C wire frame captured between <c>begin_wire_capture</c> and <c>end_wire_capture</c>.
/// </summary>
public sealed record CapturedWireFrameSnapshot(
    [property: JsonPropertyName("dir")] string Direction,
    [property: JsonPropertyName("seq")] uint Seq,
    [property: JsonPropertyName("tick_count")] uint TickCount,
    [property: JsonPropertyName("relative_tick")] long RelativeTickFrames,
    [property: JsonPropertyName("timestamp_ms")] ulong TimestampMs,
    [property: JsonPropertyName("opcode")] int Opcode,
    [property: JsonPropertyName("length")] int Length,
    [property: JsonPropertyName("raw_hex")] string RawHex);
