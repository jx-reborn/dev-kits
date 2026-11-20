using System.Text.Json.Serialization;

namespace JXReborn.ClientDriver;

/// <summary>
/// Pending party invitation captured via <c>SMCT_UI_TEAM_INVITE</c> (<c>0x69</c> sub <c>0x0C</c>).
/// </summary>
public sealed record PendingTeamInviteSnapshot(
    [property: JsonPropertyName("seq")] uint Seq,
    [property: JsonPropertyName("tick_count")] uint TickCount,
    [property: JsonPropertyName("inviter_player_idx")] int InviterPlayerIdx,
    [property: JsonPropertyName("inviter_name_tcvn3_hex")] string InviterNameTcvn3Hex)
{
    [JsonIgnore]
    public string InviterName => HexTcvn3Decoder.Decode(InviterNameTcvn3Hex);
}
