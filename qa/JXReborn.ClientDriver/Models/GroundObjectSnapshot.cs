using System.Text.Json.Serialization;

namespace JXReborn.ClientDriver;

/// <summary>
/// Ground object snapshot (<c>KObj</c>) visible in the client's active <c>KObjSet</c>.
/// </summary>
public sealed record GroundObjectSnapshot(
    [property: JsonPropertyName("array_index")] int ArrayIndex,
    [property: JsonPropertyName("object_id")] int ObjectId,
    [property: JsonPropertyName("data_id")] int DataId,
    [property: JsonPropertyName("kind")] int Kind,
    [property: JsonPropertyName("name_tcvn3_hex")] string NameTcvn3Hex,
    [property: JsonPropertyName("item_width")] int ItemWidth,
    [property: JsonPropertyName("item_height")] int ItemHeight,
    [property: JsonPropertyName("money_amount")] int MoneyAmount,
    [property: JsonPropertyName("mps_x")] int MpsX,
    [property: JsonPropertyName("mps_y")] int MpsY)
{
    [JsonIgnore]
    public string Name => HexTcvn3Decoder.Decode(NameTcvn3Hex);
}
