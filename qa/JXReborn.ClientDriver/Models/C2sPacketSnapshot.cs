using System.Text.Json.Serialization;

namespace JXReborn.ClientDriver;

/// <summary>
/// C2S packet ring-buffer entry captured at <c>IClient::SendPackToServer</c> (<c>vtable[4]</c> on <c>ADDR_GAMESVR_ICLIENT</c>).
/// </summary>
public sealed record C2sPacketSnapshot(
    [property: JsonPropertyName("seq")] uint Seq,
    [property: JsonPropertyName("opcode")] int Opcode,
    [property: JsonPropertyName("length")] int Length,
    [property: JsonPropertyName("tick_count")] uint TickCount,
    [property: JsonPropertyName("timestamp_ms")] ulong TimestampMs,
    [property: JsonPropertyName("hex_preview")] string HexPreview);
