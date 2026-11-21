namespace JXReborn.DualParityServer.Proxy;

/// <summary>
/// Live network telemetry for a single TCP proxy relay leg (Client ↔ Upstream Server).
/// </summary>
public sealed record TcpProxyStats(
    string Name,
    bool IsListening,
    int ListenPort,
    string UpstreamHost,
    int UpstreamPort,
    int ActiveConnections,
    long TotalConnections,
    long BytesClientToServer,
    long BytesServerToClient,
    long FramesClientToServer,
    long FramesServerToClient,
    DateTimeOffset? LastActivityUtc);
