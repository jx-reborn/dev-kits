using System.Text.Json.Serialization;

namespace JXReborn.ClientDriver;

/// <summary>
/// Pending P2P trade invitation captured from <c>s2c_tradeapplystart</c> (<c>0x8B</c>, 9 bytes).
/// </summary>
public sealed record PendingTradeInviteSnapshot(
    [property: JsonPropertyName("seq")] uint Seq,
    [property: JsonPropertyName("tick_count")] uint TickCount,
    [property: JsonPropertyName("inviter_player_idx")] int InviterPlayerIdx,
    [property: JsonPropertyName("inviter_npc_dw_id")] uint InviterNpcDwId);
