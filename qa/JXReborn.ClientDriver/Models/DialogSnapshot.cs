using System.Text.Json.Serialization;

namespace JXReborn.ClientDriver;

/// <summary>
/// Intercepted <c>KUiMsgSel</c> / script Say dialog snapshot.
/// </summary>
public sealed record DialogSnapshot(
    [property: JsonPropertyName("is_open")] bool IsOpen,
    [property: JsonPropertyName("sequence")] uint Sequence,
    [property: JsonPropertyName("opened_tick")] uint OpenedTick,
    [property: JsonPropertyName("prompt_tcvn3_hex")] string PromptTcvn3Hex,
    [property: JsonPropertyName("options_tcvn3_hex")] IReadOnlyList<string> OptionsTcvn3Hex,
    [property: JsonPropertyName("is_talk")] bool IsTalk = false,
    [property: JsonPropertyName("talk_need_callback")] bool TalkNeedCallback = false)
{
    [JsonIgnore]
    public string Prompt => HexTcvn3Decoder.Decode(PromptTcvn3Hex);

    [JsonIgnore]
    public IReadOnlyList<string> Options => OptionsTcvn3Hex.Select(HexTcvn3Decoder.Decode).ToArray();
}
