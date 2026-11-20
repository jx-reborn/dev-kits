using System.Text.Json.Serialization;

namespace JXReborn.ClientDriver;

/// <summary>
/// Non-captain party member slot inside <c>g_Team[0]</c> (<c>0x01340ee8</c>).
/// </summary>
public sealed record TeamMemberSnapshot(
    [property: JsonPropertyName("slot")] int Slot,
    [property: JsonPropertyName("npc_dw_id")] uint NpcDwId,
    [property: JsonPropertyName("level")] int Level,
    [property: JsonPropertyName("name_tcvn3_hex")] string NameTcvn3Hex)
{
    [JsonIgnore]
    public string Name => HexTcvn3Decoder.Decode(NameTcvn3Hex);
}
