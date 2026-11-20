using System.Text.Json.Serialization;

namespace JXReborn.ClientDriver;

/// <summary>
/// Visible entity inside the client's <c>Npc[]</c> table.
/// </summary>
public sealed record NpcSnapshot(
    [property: JsonPropertyName("array_index")] int ArrayIndex,
    [property: JsonPropertyName("is_self")] bool IsSelf,
    [property: JsonPropertyName("dw_id")] uint DwId,
    [property: JsonPropertyName("name_tcvn3_hex")] string NameTcvn3Hex,
    [property: JsonPropertyName("kind")] int Kind,
    [property: JsonPropertyName("level")] int Level,
    [property: JsonPropertyName("series")] int Series,
    [property: JsonPropertyName("camp")] int Camp,
    [property: JsonPropertyName("current_camp")] int CurrentCamp,
    [property: JsonPropertyName("doing")] int Doing,
    [property: JsonPropertyName("life")] int Life,
    [property: JsonPropertyName("life_max")] int LifeMax,
    [property: JsonPropertyName("pk_flag")] int PkFlag,
    [property: JsonPropertyName("fight_mode")] int FightMode,
    [property: JsonPropertyName("mps_x")] int MpsX,
    [property: JsonPropertyName("mps_y")] int MpsY,
    [property: JsonPropertyName("menu_state")] int MenuState = 0,
    [property: JsonPropertyName("menu_state_spr")] string MenuStateSpr = "")
{
    [JsonIgnore]
    public string Name => HexTcvn3Decoder.Decode(NameTcvn3Hex);
}
