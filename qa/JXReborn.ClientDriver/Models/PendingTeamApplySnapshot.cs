using System.Text.Json.Serialization;

namespace JXReborn.ClientDriver;

/// <summary>
/// Pending party join application captured via <c>SMCT_UI_TEAM_APPLY</c> (<c>0x69</c> sub <c>0x07</c>).
/// </summary>
public sealed record PendingTeamApplySnapshot(
    [property: JsonPropertyName("seq")] uint Seq,
    [property: JsonPropertyName("tick_count")] uint TickCount,
    [property: JsonPropertyName("applicant_npc_dw_id")] uint ApplicantNpcDwId,
    [property: JsonPropertyName("applicant_name_tcvn3_hex")] string ApplicantNameTcvn3Hex)
{
    [JsonIgnore]
    public string ApplicantName => HexTcvn3Decoder.Decode(ApplicantNameTcvn3Hex);
}
