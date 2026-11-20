using System.Text.Json.Serialization;

namespace JXReborn.ClientDriver;

/// <summary>
/// Friend or associate (enemy/brother) relation delivered via <c>IClientCallback</c> or <c>s2c_chatonefrienddata</c>.
/// </summary>
public sealed record SocialRelationSnapshot(
    [property: JsonPropertyName("name_tcvn3_hex")] string NameTcvn3Hex,
    [property: JsonPropertyName("unit_tcvn3_hex")] string UnitTcvn3Hex,
    [property: JsonPropertyName("group_tcvn3_hex")] string GroupTcvn3Hex,
    [property: JsonPropertyName("state")] byte State)
{
    [JsonIgnore]
    public string Name => HexTcvn3Decoder.Decode(NameTcvn3Hex);

    [JsonIgnore]
    public string Unit => HexTcvn3Decoder.Decode(UnitTcvn3Hex);

    [JsonIgnore]
    public string Group => HexTcvn3Decoder.Decode(GroupTcvn3Hex);
}
