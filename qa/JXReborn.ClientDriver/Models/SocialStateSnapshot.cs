using System.Text.Json.Serialization;

namespace JXReborn.ClientDriver;

/// <summary>
/// Client social relations, pending friend invitations, and client blacklist state.
/// </summary>
public sealed record SocialStateSnapshot(
    IReadOnlyList<SocialRelationSnapshot>? Friends = null,
    IReadOnlyList<SocialRelationSnapshot>? Associates = null,
    IReadOnlyList<string>? PendingFriendInvitesTcvn3Hex = null,
    IReadOnlyList<string>? BlacklistTcvn3Hex = null)
{
    public static readonly SocialStateSnapshot Empty = new();

    [JsonPropertyName("friends")]
    public IReadOnlyList<SocialRelationSnapshot> Friends { get; init; } = Friends ?? [];

    [JsonPropertyName("associates")]
    public IReadOnlyList<SocialRelationSnapshot> Associates { get; init; } = Associates ?? [];

    [JsonPropertyName("pending_friend_invites_tcvn3_hex")]
    public IReadOnlyList<string> PendingFriendInvitesTcvn3Hex { get; init; } = PendingFriendInvitesTcvn3Hex ?? [];

    [JsonPropertyName("blacklist_tcvn3_hex")]
    public IReadOnlyList<string> BlacklistTcvn3Hex { get; init; } = BlacklistTcvn3Hex ?? [];

    [JsonIgnore]
    public IReadOnlyList<string> PendingFriendInvites =>
        PendingFriendInvitesTcvn3Hex.Select(HexTcvn3Decoder.Decode).ToArray();

    [JsonIgnore]
    public IReadOnlyList<string> Blacklist =>
        BlacklistTcvn3Hex.Select(HexTcvn3Decoder.Decode).ToArray();
}
