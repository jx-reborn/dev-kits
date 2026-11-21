namespace JXReborn.DualParityServer.Standalone;

/// <summary>
/// Live status and log tail snapshot for a single managed server in Standalone mode.
/// </summary>
public sealed record ManagedServerStatus(
    string Name,
    bool Enabled,
    bool IsReady,
    string State,
    string Endpoint,
    string? Identifier,
    IReadOnlyList<string> RecentLogs);
