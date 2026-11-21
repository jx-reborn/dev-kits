using System.Collections.Concurrent;

namespace JXReborn.WireParityProxy.Proxy;

/// <summary>
/// Thread-safe TTL map bridging <see cref="BishopShadowProxy"/> handoff (<c>0x34</c>)
/// to <see cref="GameServerShadowProxy"/> login (<c>0x42</c>).
/// </summary>
public sealed class HandoffTicketMap
{
    private readonly ConcurrentDictionary<Guid, HandoffTicketEntry> _entries = new();
    private readonly TimeSpan _ttl;

    public HandoffTicketMap(TimeSpan? ttl = null)
    {
        _ttl = ttl ?? TimeSpan.FromSeconds(60);
    }

    public int PendingCount => _entries.Count;

    public void Register(HandoffTicketEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        PurgeExpired(DateTimeOffset.UtcNow);
        _entries[entry.PrimaryGuid] = entry;
    }

    public bool TryTake(Guid primaryGuid, out HandoffTicketEntry? entry)
    {
        PurgeExpired(DateTimeOffset.UtcNow);
        if (_entries.TryRemove(primaryGuid, out HandoffTicketEntry? found))
        {
            entry = found;
            return true;
        }

        entry = null;
        return false;
    }

    private void PurgeExpired(DateTimeOffset now)
    {
        foreach ((Guid key, HandoffTicketEntry value) in _entries)
        {
            if (now - value.CreatedAtUtc > _ttl)
            {
                _entries.TryRemove(key, out _);
            }
        }
    }
}
