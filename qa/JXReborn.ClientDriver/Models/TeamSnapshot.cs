using System.Text.Json.Serialization;

namespace JXReborn.ClientDriver;

/// <summary>
/// Client party and aura state read from <c>Player[1].m_cTeam</c>, <c>g_Team[0]</c>, and <c>KNpc::m_ActiveAuraID</c>.
/// </summary>
public sealed record TeamSnapshot(
    [property: JsonPropertyName("in_team")] bool InTeam,
    [property: JsonPropertyName("is_captain")] bool IsCaptain,
    [property: JsonPropertyName("is_open")] bool IsOpen,
    [property: JsonPropertyName("team_server_id")] int TeamServerId,
    [property: JsonPropertyName("captain_npc_dw_id")] uint CaptainNpcDwId,
    [property: JsonPropertyName("captain_level")] int CaptainLevel,
    [property: JsonPropertyName("captain_name_tcvn3_hex")] string CaptainNameTcvn3Hex,
    [property: JsonPropertyName("member_count")] int MemberCount,
    [property: JsonPropertyName("active_aura_skill_id")] int ActiveAuraSkillId,
    IReadOnlyList<TeamMemberSnapshot>? Members = null,
    IReadOnlyList<PendingTeamInviteSnapshot>? PendingInvites = null,
    IReadOnlyList<PendingTeamApplySnapshot>? PendingApplies = null)
{
    public static readonly TeamSnapshot Empty = new(false, false, false, -1, 0, 0, string.Empty, 0, 0);

    [JsonIgnore]
    public string CaptainName => HexTcvn3Decoder.Decode(CaptainNameTcvn3Hex);

    [JsonPropertyName("members")]
    public IReadOnlyList<TeamMemberSnapshot> Members { get; init; } = Members ?? [];

    [JsonPropertyName("pending_invites")]
    public IReadOnlyList<PendingTeamInviteSnapshot> PendingInvites { get; init; } = PendingInvites ?? [];

    [JsonPropertyName("pending_applies")]
    public IReadOnlyList<PendingTeamApplySnapshot> PendingApplies { get; init; } = PendingApplies ?? [];
}
