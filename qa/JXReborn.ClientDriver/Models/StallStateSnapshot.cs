using System.Text.Json.Serialization;

namespace JXReborn.ClientDriver;

/// <summary>
/// Client player stall state (<c>Player[1]+0x67b8</c>) and currently viewed remote stall state (<c>0x00680770</c>).
/// </summary>
public sealed record StallStateSnapshot(
    [property: JsonPropertyName("stall_state")] int StallState = 0,
    [property: JsonPropertyName("viewed_seller_npc_dw_id")] uint ViewedSellerNpcDwId = 0,
    [property: JsonPropertyName("tax_rate")] int TaxRate = 0,
    [property: JsonPropertyName("viewed_adv_tcvn3_hex")] string ViewedAdvTcvn3Hex = "",
    IReadOnlyList<ViewedStallItemSnapshot>? ViewedItems = null)
{
    public static readonly StallStateSnapshot Empty = new();

    [JsonPropertyName("viewed_items")]
    public IReadOnlyList<ViewedStallItemSnapshot> ViewedItems { get; init; } = ViewedItems ?? [];

    [JsonIgnore]
    public string ViewedAdvertisement => HexTcvn3Decoder.Decode(ViewedAdvTcvn3Hex);
}
