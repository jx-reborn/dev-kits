using System.Text.Json.Serialization;

namespace JXReborn.ClientDriver;

/// <summary>
/// Intercepted channel/chat message delivered via <c>ChannelMessageArrival</c>.
/// </summary>
public sealed record ChatMessageSnapshot(
    [property: JsonPropertyName("seq")] uint Seq,
    [property: JsonPropertyName("tick_count")] uint TickCount,
    [property: JsonPropertyName("channel_id")] uint ChannelId,
    [property: JsonPropertyName("sender_tcvn3_hex")] string SenderTcvn3Hex,
    [property: JsonPropertyName("text_tcvn3_hex")] string TextTcvn3Hex)
{
    [JsonIgnore]
    public string Sender => HexTcvn3Decoder.Decode(SenderTcvn3Hex);

    [JsonIgnore]
    public string Text => HexTcvn3Decoder.Decode(TextTcvn3Hex);
}
