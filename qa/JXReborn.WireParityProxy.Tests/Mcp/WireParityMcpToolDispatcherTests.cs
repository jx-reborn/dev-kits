using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Nodes;
using JXReborn.Adapter.Legacy.Ciphers;
using JXReborn.Adapter.Legacy.Packets;
using JXReborn.DualParityServer.Diffing;
using JXReborn.WireParityProxy.Configuration;
using JXReborn.WireParityProxy.Driver;
using JXReborn.WireParityProxy.Mcp;
using JXReborn.WireParityProxy.Proxy;
using Xunit;

namespace JXReborn.WireParityProxy.Tests.Mcp;

public sealed class WireParityMcpToolDispatcherTests
{
    [Fact]
    public async Task InitializeListAndCallStatus_ShouldFollowMcpJsonRpcContract()
    {
        LegacyKeyTable keyTable = LegacyKeyTable.CreateSynthetic();
        var options = new WireParityProxyOptions
        {
            BishopListenPort = 0,
            GameServerListenPort = 0,
            AllowSyntheticKeyTableFallback = true
        };
        var handoffMap = new HandoffTicketMap();
        var globalFindings = new ConcurrentQueue<ParityFinding>();

        await using var driverBridge = new PrimaryClientDriverBridge();
        await using var gsProxy = new GameServerShadowProxy(options, keyTable, handoffMap);
        await using var bishopProxy = new BishopShadowProxy(options, keyTable, handoffMap, () => gsProxy.BoundPort);

        var dispatcher = new WireParityMcpToolDispatcher(
            options,
            bishopProxy,
            gsProxy,
            driverBridge,
            globalFindings);

        JsonObject? initResp = await dispatcher.HandleRequestAsync(new JsonObject
        {
            ["jsonrpc"] = "2.0",
            ["id"] = 1,
            ["method"] = "initialize"
        });
        Assert.NotNull(initResp);
        Assert.Equal("jxreborn-wire-parity-proxy", initResp["result"]?["serverInfo"]?["name"]?.GetValue<string>());

        JsonObject? listResp = await dispatcher.HandleRequestAsync(new JsonObject
        {
            ["jsonrpc"] = "2.0",
            ["id"] = 2,
            ["method"] = "tools/list"
        });
        Assert.NotNull(listResp);
        JsonArray? tools = listResp["result"]?["tools"] as JsonArray;
        Assert.NotNull(tools);
        Assert.Equal(8, tools.Count);

        JsonObject? callResp = await dispatcher.HandleRequestAsync(new JsonObject
        {
            ["jsonrpc"] = "2.0",
            ["id"] = 3,
            ["method"] = "tools/call",
            ["params"] = new JsonObject
            {
                ["name"] = "get_proxy_status",
                ["arguments"] = new JsonObject()
            }
        });
        Assert.NotNull(callResp);
        Assert.False(callResp["result"]?["isError"]?.GetValue<bool>());
        string statusJson = callResp["result"]?["content"]?[0]?["text"]?.GetValue<string>() ?? string.Empty;
        using JsonDocument doc = JsonDocument.Parse(statusJson);
        Assert.Equal("legacy-linux", doc.RootElement.GetProperty("primaryBackend").GetString());
        Assert.Equal("reborn-server", doc.RootElement.GetProperty("shadowBackend").GetString());
    }

    [Fact]
    public async Task StdioHost_ShouldProcessLineDelimitedJsonRpcAndIgnoreNotifications()
    {
        LegacyKeyTable keyTable = LegacyKeyTable.CreateSynthetic();
        var options = new WireParityProxyOptions { AllowSyntheticKeyTableFallback = true };
        var handoffMap = new HandoffTicketMap();

        await using var driverBridge = new PrimaryClientDriverBridge();
        await using var gsProxy = new GameServerShadowProxy(options, keyTable, handoffMap);
        await using var bishopProxy = new BishopShadowProxy(options, keyTable, handoffMap, () => gsProxy.BoundPort);

        var dispatcher = new WireParityMcpToolDispatcher(
            options,
            bishopProxy,
            gsProxy,
            driverBridge,
            new ConcurrentQueue<ParityFinding>());
        var host = new WireParityMcpStdioHost(dispatcher);

        string inputLines =
            "{\"jsonrpc\":\"2.0\",\"method\":\"notifications/initialized\"}\n" +
            "{\"jsonrpc\":\"2.0\",\"id\":10,\"method\":\"ping\"}\n";
        using var reader = new StringReader(inputLines);
        using var writer = new StringWriter();

        await host.RunAsync(reader, writer);

        string output = writer.ToString().Trim();
        Assert.Single(output.Split('\n'));
        using JsonDocument doc = JsonDocument.Parse(output);
        Assert.Equal(10, doc.RootElement.GetProperty("id").GetInt32());
    }
}
