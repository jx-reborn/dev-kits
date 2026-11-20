using System.Text.Json.Serialization;

namespace JXReborn.ClientDriver;

/// <summary>
/// Character slot information decoded from <c>KLogin</c>'s 36-byte roster entry.
/// </summary>
public sealed record RoleSlotSnapshot(
    [property: JsonPropertyName("index")] int Index,
    [property: JsonPropertyName("name_tcvn3_hex")] string NameTcvn3Hex,
    [property: JsonPropertyName("gender")] int Gender,
    [property: JsonPropertyName("series")] int Series,
    [property: JsonPropertyName("native_place_id")] int NativePlaceId)
{
    [JsonIgnore]
    public string Name => HexTcvn3Decoder.Decode(NameTcvn3Hex);
}
