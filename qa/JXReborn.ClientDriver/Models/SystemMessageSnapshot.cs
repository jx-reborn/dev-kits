using System.Text.Json.Serialization;

namespace JXReborn.ClientDriver;

/// <summary>
/// Intercepted system message entry decoded from TCVN3 hex.
/// </summary>
public sealed record SystemMessageSnapshot(
    [property: JsonPropertyName("seq")] uint Seq,
    [property: JsonPropertyName("tick_count")] uint TickCount,
    [property: JsonPropertyName("text_tcvn3_hex")] string TextTcvn3Hex)
{
    [JsonIgnore]
    public string Text => HexTcvn3Decoder.Decode(TextTcvn3Hex);
}
