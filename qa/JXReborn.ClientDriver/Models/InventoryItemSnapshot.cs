using System.Text.Json.Serialization;

namespace JXReborn.ClientDriver;

/// <summary>
/// Player inventory/equipment/hand item snapshot from <c>KItemList</c> and global <c>Item[]</c>.
/// </summary>
public sealed record InventoryItemSnapshot(
    [property: JsonPropertyName("array_index")] int ArrayIndex,
    [property: JsonPropertyName("server_item_id")] uint ServerItemId,
    [property: JsonPropertyName("place")] int Place,
    [property: JsonPropertyName("grid_x")] int GridX,
    [property: JsonPropertyName("grid_y")] int GridY,
    [property: JsonPropertyName("genre")] int Genre,
    [property: JsonPropertyName("detail_type")] int DetailType,
    [property: JsonPropertyName("particular_type")] int ParticularType,
    [property: JsonPropertyName("level")] int Level,
    [property: JsonPropertyName("series")] int Series,
    [property: JsonPropertyName("width")] int Width,
    [property: JsonPropertyName("height")] int Height,
    [property: JsonPropertyName("cur_durability")] int CurDurability,
    [property: JsonPropertyName("max_durability")] int MaxDurability,
    [property: JsonPropertyName("stack_count")] int StackCount,
    [property: JsonPropertyName("name_tcvn3_hex")] string NameTcvn3Hex)
{
    [JsonIgnore]
    public string Name => HexTcvn3Decoder.Decode(NameTcvn3Hex);
}
