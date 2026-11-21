namespace JXReborn.WireParityProxy.Proxy;

/// <summary>
/// Maps a Primary Bishop handoff ticket (<c>Guid</c>) to the corresponding Shadow Bishop handoff ticket
/// and both upstream GameServer endpoints.
/// </summary>
public sealed record HandoffTicketEntry(
    Guid PrimaryGuid,
    string PrimaryGsHost,
    ushort PrimaryGsPort,
    Guid ShadowGuid,
    string ShadowGsHost,
    ushort ShadowGsPort,
    string RoleName,
    DateTimeOffset CreatedAtUtc);
