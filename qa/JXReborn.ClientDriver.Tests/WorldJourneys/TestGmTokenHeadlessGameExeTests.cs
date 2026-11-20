using JXReborn.ClientDriver.Tests.DifferentialParity;
using Xunit;

namespace JXReborn.ClientDriver.Tests.WorldJourneys;

[Collection(HeadlessGameExeCollectionDefinition.Name)]
[Trait("Category", "Headless")]
public sealed class TestGmTokenHeadlessGameExeTests
{
    private readonly ITestOutputHelper _output;

    public TestGmTokenHeadlessGameExeTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public async Task GameExe_GmTokenCombatAndTradeTestMenus_ShouldUpdatePkFightModeAndGrantItems()
    {
        await using var sutHarness = await HeadlessTestServerHarness.StartAsync(
            enableTestGmToken: true,
            autoGrantGmTokenOnLogin: true);

        await using JxHeadlessClient client = await JxHeadlessClient.SpawnAsync(new JxHeadlessOptions
        {
            GatewayPort = sutHarness.BishopPort,
        });

        WorldDomSnapshot snap;
        try
        {
            snap = await client.LoginAndEnterWorldAsync(
                "127.0.0.1",
                sutHarness.BishopPort,
                account: "gmtokqa1",
                password: "123123",
                roleName: "TestTokQA",
                gender: 0,
                series: 0,
                nativePlaceId: 53);
        }
        catch
        {
            foreach (var entry in sutHarness.LogCollector.SnapshotEntries())
            {
                _output.WriteLine($"[{entry.Level}] {entry.Category}: {entry.Message}");
            }

            throw;
        }

        Assert.NotNull(TestGmTokenDriver.FindGmTokenInBag(snap));
        int initialBagItems = snap.Items.Count(i => i.Place == 3);

        // [4] Combat & PK -> [3] PkValue = 10; [0] FightMode = 1
        snap = await TestGmTokenDriver.InvokePathAsync(client, TestGmTokenDriver.PathPk10);
        snap = await client.WaitUntilAsync(s => s.Player.PkValue == 10, TimeSpan.FromSeconds(5));
        Assert.Equal(10, snap.Player.PkValue);

        snap = await TestGmTokenDriver.InvokePathAsync(client, TestGmTokenDriver.PathFightMode1);
        snap = await client.WaitUntilAsync(s => s.Player.FightMode == 1, TimeSpan.FromSeconds(5));
        Assert.Equal(1, snap.Player.FightMode);

        // [3] Items -> [0] Medicines -> [3] 5 HP + 5 MP unbound medicines
        snap = await TestGmTokenDriver.InvokePathAsync(client, TestGmTokenDriver.PathMedPack5x5);
        snap = await client.WaitUntilAsync(s => s.Items.Count(i => i.Place == 3) >= initialBagItems + 10, TimeSpan.FromSeconds(5));
        Assert.True(snap.Items.Count(i => i.Place == 3) >= initialBagItems + 10);

        // [3] Items -> [4] Bag Boundary -> [0] Clear bag keeping GM Token
        snap = await TestGmTokenDriver.InvokePathAsync(client, TestGmTokenDriver.PathBagClear);
        snap = await client.WaitUntilAsync(s => s.Items.Count(i => i.Place == 3) == 1, TimeSpan.FromSeconds(5));
        Assert.NotNull(TestGmTokenDriver.FindGmTokenInBag(snap));
    }
}
