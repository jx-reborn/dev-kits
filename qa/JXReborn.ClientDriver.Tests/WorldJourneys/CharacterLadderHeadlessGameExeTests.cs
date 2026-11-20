using JXReborn.Adapter.Legacy.Packets;
using JXReborn.ClientDriver.Tests.DifferentialParity;
using Xunit;

namespace JXReborn.ClientDriver.Tests.WorldJourneys;

[Collection(HeadlessGameExeCollectionDefinition.Name)]
[Trait("Category", "Headless")]
public sealed class CharacterLadderHeadlessGameExeTests
{
    private const uint RawOpQueryRankInformation = 0x36;
    private const uint RawOpQueryRankParamPtr = 0x006384C8;

    private readonly ITestOutputHelper _output;

    public CharacterLadderHeadlessGameExeTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public async Task GameExe_EnterWorldAndQueryLadder_ShouldPopulateWorldRankAndReceive0x96()
    {
        await using var sutHarness = await HeadlessTestServerHarness.StartAsync();

        await using JxHeadlessClient client = await JxHeadlessClient.SpawnAsync(new JxHeadlessOptions
        {
            GatewayPort = sutHarness.BishopPort,
        });

        WorldDomSnapshot snap = await client.LoginAndEnterWorldAsync(
            "127.0.0.1",
            sutHarness.BishopPort,
            account: "ladder_qa",
            password: "123123",
            roleName: "LadderQA",
            gender: 0,
            series: 0,
            nativePlaceId: 53);

        _output.WriteLine($"Entered world: WorldRank={snap.Player.WorldRank}, Level={snap.Player.Level}");
        Assert.True(snap.Player.WorldRank >= 1, $"Expected Player.WorldRank >= 1 from 0x44 WorldStat, got {snap.Player.WorldRank}.");

        uint s2cBefore = snap.RecentS2c.Count > 0 ? snap.RecentS2c.Max(p => p.Seq) : 0u;
        _ = await client.RawOperationAsync(RawOpQueryRankInformation, RawOpQueryRankParamPtr, 1);

        snap = await client.WaitUntilAsync(
            s => s.RecentS2c.Any(p => p.Seq > s2cBefore && p.Opcode == ProtocolOpcodes.s2c_ladderresult && p.Length == 466),
            TimeSpan.FromSeconds(6));

        S2cPacketSnapshot ladderPkt = snap.RecentS2c.Last(p => p.Seq > s2cBefore && p.Opcode == ProtocolOpcodes.s2c_ladderresult && p.Length == 466);
        _output.WriteLine($"Received 0x96 ladder result: Len={ladderPkt.Length}, Hex={ladderPkt.HexPreview}");
        Assert.Equal(466, ladderPkt.Length);
    }
}
