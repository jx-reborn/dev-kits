using System.Text.Json.Serialization;

namespace JXReborn.ClientDriver;

/// <summary>
/// Last crafting / enchaser / foundry result captured from S2C <c>0xA0</c> (7 bytes @ <c>0x00557990</c>).
/// </summary>
public sealed record CraftingResultSnapshot(
    [property: JsonPropertyName("seq")] uint Seq = 0,
    [property: JsonPropertyName("action_kind")] byte ActionKind = 0,
    [property: JsonPropertyName("item_dw_id")] uint ItemDwId = 0,
    [property: JsonPropertyName("result_code")] byte ResultCode = 0)
{
    public static readonly CraftingResultSnapshot Empty = new();
}
