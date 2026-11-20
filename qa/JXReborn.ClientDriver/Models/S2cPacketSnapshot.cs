using System.Text.Json.Serialization;

namespace JXReborn.ClientDriver;

/// <summary>
/// S2C packet ring-buffer entry captured at <c>KProtocolProcess::ProcessMsg</c>.
/// </summary>
public sealed record S2cPacketSnapshot(
    [property: JsonPropertyName("seq")] uint Seq,
    [property: JsonPropertyName("opcode")] int Opcode,
    [property: JsonPropertyName("length")] int Length,
    [property: JsonPropertyName("tick_count")] uint TickCount,
    [property: JsonPropertyName("timestamp_ms")] ulong TimestampMs,
    [property: JsonPropertyName("hex_preview")] string HexPreview);
