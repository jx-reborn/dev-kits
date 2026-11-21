using System.Buffers.Binary;
using System.Net.Sockets;
using System.Security.Cryptography;
using JXReborn.Adapter.Legacy.Ciphers;
using JXReborn.Adapter.Legacy.Packets;

namespace JXReborn.WireParityProxy.Wire;

/// <summary>
/// Full-duplex L7 encrypted JX1 TCP wire connection supporting both server-side acceptance
/// (sending <c>0x20</c> handshake to <c>game.exe</c>) and client-side upstream connection
/// (receiving <c>0x20</c> handshake from <c>legacy-linux</c> or <c>reborn-server</c>).
/// </summary>
public sealed class JxWireConnection : IAsyncDisposable
{
    public const int MinFrameLength = 3;
    public const int MaxFrameLength = 0xFFFF;

    private readonly TcpClient _tcp;
    private readonly NetworkStream _stream;
    private readonly LegacyKeyTable _keyTable;
    private readonly SemaphoreSlim _writeLock = new(1, 1);
    private readonly bool _isServerSide;
    private long _readSequence;
    private long _writeSequence;
    private long _bytesRead;
    private long _bytesWritten;
    private int _disposed;

    private JxWireConnection(
        string roleLabel,
        TcpClient tcp,
        LegacyKeyTable keyTable,
        uint clientKey,
        uint serverKey,
        bool isServerSide)
    {
        RoleLabel = roleLabel;
        _tcp = tcp;
        _stream = tcp.GetStream();
        _keyTable = keyTable;
        ClientKey = clientKey;
        ServerKey = serverKey;
        _isServerSide = isServerSide;
    }

    public string RoleLabel { get; }

    public uint ClientKey { get; }

    public uint ServerKey { get; }

    public long FramesRead => Interlocked.Read(ref _readSequence);

    public long FramesWritten => Interlocked.Read(ref _writeSequence);

    public long BytesRead => Interlocked.Read(ref _bytesRead);

    public long BytesWritten => Interlocked.Read(ref _bytesWritten);

    /// <summary>
    /// Accepts an inbound client TCP connection, generates random non-zero session keys,
    /// and transmits the unencrypted 44-byte <c>s2c_accountbegin</c> (<c>0x20</c>) handshake frame.
    /// </summary>
    public static async Task<JxWireConnection> AcceptClientAsync(
        string roleLabel,
        TcpClient acceptedClient,
        LegacyKeyTable keyTable,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(acceptedClient);
        ArgumentNullException.ThrowIfNull(keyTable);

        acceptedClient.NoDelay = true;
        uint clientKey = GenerateNonZeroKey();
        uint serverKey = GenerateNonZeroKey();

        byte[] handshakeWire = new byte[AccountBeginS2CPacket.WireFrameSize];
        AccountBeginS2CPacket.Create(clientKey, serverKey).WriteWireFrame(handshakeWire);

        NetworkStream stream = acceptedClient.GetStream();
        await stream.WriteAsync(handshakeWire, cancellationToken).ConfigureAwait(false);
        await stream.FlushAsync(cancellationToken).ConfigureAwait(false);

        return new JxWireConnection(roleLabel, acceptedClient, keyTable, clientKey, serverKey, isServerSide: true);
    }

    /// <summary>
    /// Connects to an upstream JX1 server (<c>legacy-linux</c> or <c>reborn-server</c>),
    /// reads its 44-byte unencrypted <c>0x20</c> handshake frame, and extracts <c>(ClientKey, ServerKey)</c>.
    /// </summary>
    public static async Task<JxWireConnection> ConnectUpstreamAsync(
        string roleLabel,
        string host,
        int port,
        LegacyKeyTable keyTable,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(host);
        ArgumentNullException.ThrowIfNull(keyTable);

        var tcp = new TcpClient { NoDelay = true };
        try
        {
            await tcp.ConnectAsync(host, port, cancellationToken).ConfigureAwait(false);
            NetworkStream stream = tcp.GetStream();

            byte[] handshakeWire = new byte[AccountBeginS2CPacket.WireFrameSize];
            await ReadExactAsync(stream, handshakeWire, cancellationToken).ConfigureAwait(false);

            ushort frameLength = BinaryPrimitives.ReadUInt16LittleEndian(handshakeWire.AsSpan(0, 2));
            if (frameLength != AccountBeginS2CPacket.WireFrameSize)
            {
                throw new InvalidDataException(
                    $"[{roleLabel}] Unexpected handshake frame length {frameLength}, expected {AccountBeginS2CPacket.WireFrameSize}.");
            }

            (uint clientKey, uint serverKey) = HandshakeKeyObfuscator.ExtractHandshakeKeys(
                handshakeWire.AsSpan(2, AccountBeginS2CPacket.PayloadSize));

            return new JxWireConnection(roleLabel, tcp, keyTable, clientKey, serverKey, isServerSide: false);
        }
        catch
        {
            tcp.Dispose();
            throw;
        }
    }

    /// <summary>
    /// Reads and decrypts the next framed packet from the TCP stream. Returns <c>null</c> on clean EOF.
    /// </summary>
    public async Task<JxWireFrame?> ReadDecryptedFrameAsync(CancellationToken cancellationToken)
    {
        byte[] header = new byte[2];
        bool gotHeader = await TryReadExactAsync(_stream, header, cancellationToken).ConfigureAwait(false);
        if (!gotHeader)
        {
            return null;
        }

        ushort frameLength = BinaryPrimitives.ReadUInt16LittleEndian(header);
        if (frameLength is < MinFrameLength or > MaxFrameLength)
        {
            throw new InvalidDataException(
                $"[{RoleLabel}] Invalid wire frame length {frameLength} (expected {MinFrameLength}..{MaxFrameLength}).");
        }

        int payloadLength = frameLength - 2;
        byte[] payload = new byte[payloadLength];
        await ReadExactAsync(_stream, payload, cancellationToken).ConfigureAwait(false);

        uint decryptKey = _isServerSide ? ClientKey : ServerKey;
        LegacyStreamCipher.EncodeDecodeByValue(payload, decryptKey, _keyTable.PublicKeys);

        Interlocked.Add(ref _bytesRead, frameLength);
        long seq = Interlocked.Increment(ref _readSequence);

        return new JxWireFrame(
            Opcode: payload[0],
            DecryptedPayload: payload,
            TimestampUtc: DateTimeOffset.UtcNow,
            Sequence: seq);
    }

    /// <summary>
    /// Encrypts <paramref name="decryptedPayload"/> with this connection's outbound key and writes the framed packet.
    /// Does not mutate <paramref name="decryptedPayload"/>.
    /// </summary>
    public async Task WriteEncryptedFrameAsync(ReadOnlyMemory<byte> decryptedPayload, CancellationToken cancellationToken)
    {
        if (decryptedPayload.IsEmpty)
        {
            throw new ArgumentException("Payload cannot be empty.", nameof(decryptedPayload));
        }

        int wireLength = decryptedPayload.Length + 2;
        if (wireLength > MaxFrameLength)
        {
            throw new ArgumentOutOfRangeException(
                nameof(decryptedPayload),
                $"Frame length {wireLength} exceeds maximum {MaxFrameLength}.");
        }

        byte[] wireBuffer = new byte[wireLength];
        BinaryPrimitives.WriteUInt16LittleEndian(wireBuffer.AsSpan(0, 2), (ushort)wireLength);
        decryptedPayload.Span.CopyTo(wireBuffer.AsSpan(2));

        uint encryptKey = _isServerSide ? ServerKey : ClientKey;
        LegacyStreamCipher.EncodeDecodeByValue(wireBuffer.AsSpan(2), encryptKey, _keyTable.PublicKeys);

        await _writeLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await _stream.WriteAsync(wireBuffer, cancellationToken).ConfigureAwait(false);
            await _stream.FlushAsync(cancellationToken).ConfigureAwait(false);
            Interlocked.Add(ref _bytesWritten, wireLength);
            Interlocked.Increment(ref _writeSequence);
        }
        finally
        {
            _writeLock.Release();
        }
    }

    public ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 0)
        {
            _stream.Dispose();
            _tcp.Dispose();
            _writeLock.Dispose();
        }

        return ValueTask.CompletedTask;
    }

    private static uint GenerateNonZeroKey()
    {
        Span<byte> bytes = stackalloc byte[4];
        uint value;
        do
        {
            RandomNumberGenerator.Fill(bytes);
            value = BinaryPrimitives.ReadUInt32LittleEndian(bytes);
        }
        while (value == 0);

        return value;
    }

    private static async Task<bool> TryReadExactAsync(
        NetworkStream stream,
        Memory<byte> buffer,
        CancellationToken cancellationToken)
    {
        int totalRead = 0;
        while (totalRead < buffer.Length)
        {
            int read = await stream.ReadAsync(buffer[totalRead..], cancellationToken).ConfigureAwait(false);
            if (read == 0)
            {
                if (totalRead == 0)
                {
                    return false;
                }

                throw new EndOfStreamException("Unexpected EOF in middle of wire frame.");
            }

            totalRead += read;
        }

        return true;
    }

    private static async Task ReadExactAsync(
        NetworkStream stream,
        Memory<byte> buffer,
        CancellationToken cancellationToken)
    {
        bool ok = await TryReadExactAsync(stream, buffer, cancellationToken).ConfigureAwait(false);
        if (!ok)
        {
            throw new EndOfStreamException("Unexpected EOF while reading wire frame.");
        }
    }
}
