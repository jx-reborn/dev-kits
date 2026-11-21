using JXReborn.ClientDriver;
using JXReborn.WireParityProxy.Configuration;
using JXReborn.WireParityProxy.Proxy;

namespace JXReborn.WireParityProxy.Driver;

/// <summary>
/// Optional Named-Pipe driver bridge attaching to the single <c>game.exe</c> instance
/// (either spawned headless or live-attached) to read client RAM ground-truth snapshots
/// and cross-check <see cref="WireParitySession.PrimaryState"/>.
/// </summary>
public sealed class PrimaryClientDriverBridge : IAsyncDisposable
{
    private readonly Lock _sync = new();
    private JxHeadlessClient? _client;

    public bool IsConnected
    {
        get
        {
            lock (_sync)
            {
                return _client is not null;
            }
        }
    }

    public JxHeadlessClient? Client
    {
        get
        {
            lock (_sync)
            {
                return _client;
            }
        }
    }

    public async Task ConnectLiveProcessAsync(int pid, CancellationToken cancellationToken = default)
    {
        JxHeadlessClient attached = await JxHeadlessClient.AttachToProcessAsync(pid, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        lock (_sync)
        {
            _client = attached;
        }
    }

    public async Task SpawnAgainstProxyAsync(
        WireParityProxyOptions options,
        int boundBishopPort,
        bool interactiveWindow = true,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);
        JxHeadlessClient spawned = await JxHeadlessClient.SpawnAsync(
            new JxHeadlessOptions
            {
                ServerIp = options.BindHost,
                GatewayPort = boundBishopPort,
                InteractiveWindow = interactiveWindow
            },
            cancellationToken).ConfigureAwait(false);

        lock (_sync)
        {
            _client = spawned;
        }
    }

    public async Task<WorldDomSnapshot?> GetClientRamSnapshotAsync(CancellationToken cancellationToken = default)
    {
        JxHeadlessClient? current;
        lock (_sync)
        {
            current = _client;
        }

        if (current is null)
        {
            return null;
        }

        return await current.GetStateAsync(cancellationToken).ConfigureAwait(false);
    }

    public async ValueTask DisposeAsync()
    {
        JxHeadlessClient? current;
        lock (_sync)
        {
            current = _client;
            _client = null;
        }

        if (current is not null)
        {
            await current.DisposeAsync().ConfigureAwait(false);
        }
    }
}
