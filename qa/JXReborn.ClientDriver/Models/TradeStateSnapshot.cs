using System.Text.Json.Serialization;

namespace JXReborn.ClientDriver;

/// <summary>
/// Client P2P trade state from <c>KPlayerTrade</c> (<c>Player[1]+0x5750</c>) and S2C trade sync packets (<c>0x8B</c>, <c>0x75</c>, <c>0x77</c>, <c>0x81</c>, <c>0x78</c>).
/// </summary>
public sealed record TradeStateSnapshot(
    [property: JsonPropertyName("state")] int State = 0,
    [property: JsonPropertyName("partner_npc_dw_id")] uint PartnerNpcDwId = 0,
    [property: JsonPropertyName("self_locked")] bool SelfLocked = false,
    [property: JsonPropertyName("other_locked")] bool OtherLocked = false,
    [property: JsonPropertyName("self_confirmed")] bool SelfConfirmed = false,
    [property: JsonPropertyName("other_confirmed")] bool OtherConfirmed = false,
    [property: JsonPropertyName("other_money")] int OtherMoney = 0,
    [property: JsonPropertyName("last_decision_result")] int LastDecisionResult = -1,
    [property: JsonPropertyName("decision_seq")] uint DecisionSeq = 0,
    IReadOnlyList<PendingTradeInviteSnapshot>? PendingInvites = null)
{
    public static readonly TradeStateSnapshot Empty = new();

    [JsonPropertyName("pending_invites")]
    public IReadOnlyList<PendingTradeInviteSnapshot> PendingInvites { get; init; } = PendingInvites ?? [];
}
