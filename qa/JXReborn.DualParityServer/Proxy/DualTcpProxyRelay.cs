using System.Net;
using System.Net.Sockets;

namespace JXReborn.DualParityServer.Proxy;

/// <summary>
/// Transparent async TCP pass-through relay sitting between a <c>game.exe</c> client and a target server
/// (<c>legacy-linux</c> or <c>reborn-server</c>), tracking connection state and wire traffic counters.
/// </summary>
public sealed class DualTcpProxyRelay : IAsyncDisposable
{
    private readonly string _name;
    private readonly string _upstreamHost;
    private readonly int _upstreamPort;
    private readonly CancellationTokenSource _cts = new();

    private TcpListener? _listener;
    private Task? _acceptLoop;
    private int _listenPort;
    private int _activeConnections;
    private long _totalConnections;
    private long _bytesC2S;
    private long _bytesS2C;
    private long _framesC2S;
    private long _framesS2C;
    private long _lastActivityUnixMs;

    public DualTcpProxyRelay(string name, int listenPort, string upstreamHost, int upstreamPort)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(upstreamHost);
        _name = name;
        _listenPort = listenPort;
        _upstreamHost = upstreamHost;
        _upstreamPort = upstreamPort;
    }

    public int BoundPort => _listenPort;

    public bool IsListening => _listener is not null;

    public void Start()
    {
        if (_listener is not null)
        {
            return;
        }

        _listener = new TcpListener(IPAddress.Loopback, _listenPort);
        _listener.Start();
        _listenPort = ((IPEndPoint)_listener.LocalEndpoint).Port;
        _acceptLoop = Task.Run(() => AcceptLoopAsync(_cts.Token), _cts.Token);
    }

    public TcpProxyStats GetStats()
    {
        long lastMs = Interlocked.Read(ref _lastActivityUnixMs);
        return new TcpProxyStats(
            Name: _name,
            IsListening: _listener is not null,
            ListenPort: _listenPort,
            UpstreamHost: _upstreamHost,
            UpstreamPort: _upstreamPort,
            ActiveConnections: Volatile.Read(ref _activeConnections),
            TotalConnections: Interlocked.Read(ref _totalConnections),
            BytesClientToServer: Interlocked.Read(ref _bytesC2S),
            BytesServerToClient: Interlocked.Read(ref _bytesS2C),
            FramesClientToServer: Interlocked.Read(ref _framesC2S),
            FramesServerToClient: Interlocked.Read(ref _framesS2C),
            LastActivityUtc: lastMs > 0 ? DateTimeOffset.FromUnixTimeMilliseconds(lastMs) : null);
    }

    private async Task AcceptLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested && _listener is not null)
        {
            TcpClient client;
            try
            {
                client = await _listener.AcceptTcpClientAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (ObjectDisposedException)
            {
                break;
            }
            catch (SocketException)
            {
                break;
            }

            _ = Task.Run(() => HandleConnectionAsync(client, cancellationToken), cancellationToken);
        }
    }

    private async Task HandleConnectionAsync(TcpClient client, CancellationToken cancellationToken)
    {
        Interlocked.Increment(ref _activeConnections);
        Interlocked.Increment(ref _totalConnections);
        TouchActivity();

        using TcpClient clientDisposable = client;
        using var upstream = new TcpClient();
        try
        {
            client.NoDelay = true;
            upstream.NoDelay = true;
            await upstream.ConnectAsync(_upstreamHost, _upstreamPort, cancellationToken);

            NetworkStream clientStream = client.GetStream();
            NetworkStream upstreamStream = upstream.GetStream();

            using var linkCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            Task c2s = PumpAsync(clientStream, upstreamStream, isClientToServer: true, linkCts.Token);
            Task s2c = PumpAsync(upstreamStream, clientStream, isClientToServer: false, linkCts.Token);

            await Task.WhenAny(c2s, s2c);
            await linkCts.CancelAsync();
            try
            {
                await Task.WhenAll(c2s, s2c);
            }
            catch (OperationCanceledException)
            {
                // Expected when peer closes cleanly.
            }
        }
        catch (OperationCanceledException)
        {
            // Expected on shutdown.
        }
        catch (SocketException)
        {
            // Upstream unavailable or peer reset.
        }
        catch (IOException)
        {
            // Connection terminated.
        }
        finally
        {
            Interlocked.Decrement(ref _activeConnections);
        }
    }

    private async Task PumpAsync(
        NetworkStream source,
        NetworkStream destination,
        bool isClientToServer,
        CancellationToken cancellationToken)
    {
        byte[] buffer = new byte[16 * 1024];
        while (!cancellationToken.IsCancellationRequested)
        {
            int read;
            try
            {
                read = await source.ReadAsync(buffer, cancellationToken);
            }
            catch (IOException)
            {
                break;
            }
            catch (SocketException)
            {
                break;
            }

            if (read <= 0)
            {
                break;
            }

            await destination.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
            await destination.FlushAsync(cancellationToken);

            if (isClientToServer)
            {
                Interlocked.Add(ref _bytesC2S, read);
                Interlocked.Increment(ref _framesC2S);
            }
            else
            {
                Interlocked.Add(ref _bytesS2C, read);
                Interlocked.Increment(ref _framesS2C);
            }

            TouchActivity();
        }
    }

    private void TouchActivity()
    {
        Interlocked.Exchange(ref _lastActivityUnixMs, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
    }

    public async ValueTask DisposeAsync()
    {
        await _cts.CancelAsync();
        try
        {
            _listener?.Stop();
        }
        catch (SocketException)
        {
            // Best-effort listener shutdown.
        }

        _listener = null;
        if (_acceptLoop is not null)
        {
            try
            {
                await _acceptLoop;
            }
            catch (OperationCanceledException)
            {
                // Expected.
            }
        }

        _cts.Dispose();
    }
}
