using System.Text.Json.Serialization;

namespace JXReborn.ClientDriver;

/// <summary>
/// Result returned by <c>select_dialog</c>.
/// </summary>
public sealed record SelectDialogResult(
    [property: JsonPropertyName("sent")] bool Sent,
    [property: JsonPropertyName("option_index")] int OptionIndex,
    [property: JsonPropertyName("used_ui_msgsel")] bool UsedUiMsgSel);
