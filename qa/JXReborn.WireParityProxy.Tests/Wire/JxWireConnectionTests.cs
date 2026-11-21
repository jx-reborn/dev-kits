using System.Net;
using System.Net.Sockets;
using JXReborn.Adapter.Legacy.Ciphers;
using JXReborn.WireParityProxy.Wire;
using Xunit;

namespace JXReborn.WireParityProxy.Tests.Wire;

public sealed class JxWireConnectionTests
{
    [Fact]
    public async Task AcceptAndConnect_ShouldExchangeHandshakeKeysAndRoundTripEncryptedFrames()
    {
        LegacyKeyTable keyTable = LegacyKeyTable.CreateSynthetic();
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        int port = ((IPEndPoint)listener.LocalEndpoint).Port;

        Task<JxWireConnection> serverAcceptTask = Task.Run(async () =>
        {
            TcpClient accepted = await listener.AcceptTcpClientAsync();
            return await JxWireConnection.AcceptClientAsync("test-server", accepted, keyTable, CancellationToken.None);
        });

        await using JxWireConnection client = await JxWireConnection.ConnectUpstreamAsync(
            "test-client",
            "127.0.0.1",
            port,
            keyTable,
            CancellationToken.None);
        await using JxWireConnection server = await serverAcceptTask;

        Assert.Equal(server.ClientKey, client.ClientKey);
        Assert.Equal(server.ServerKey, client.ServerKey);
        Assert.NotEqual(0u, server.ClientKey);
        Assert.NotEqual(0u, server.ServerKey);

        byte[] c2sPayload = [0x42, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16];
        await client.WriteEncryptedFrameAsync(c2sPayload, CancellationToken.None);
        JxWireFrame? receivedC2s = await server.ReadDecryptedFrameAsync(CancellationToken.None);

        Assert.NotNull(receivedC2s);
        Assert.Equal(0x42, receivedC2s.Opcode);
        Assert.Equal(c2sPayload, receivedC2s.DecryptedPayload);

        byte[] s2cPayload = [0x43, 0xAA, 0xBB, 0xCC, 0xDD];
        await server.WriteEncryptedFrameAsync(s2cPayload, CancellationToken.None);
        JxWireFrame? receivedS2c = await client.ReadDecryptedFrameAsync(CancellationToken.None);

        Assert.NotNull(receivedS2c);
        Assert.Equal(0x43, receivedS2c.Opcode);
        Assert.Equal(s2cPayload, receivedS2c.DecryptedPayload);
    }
}
