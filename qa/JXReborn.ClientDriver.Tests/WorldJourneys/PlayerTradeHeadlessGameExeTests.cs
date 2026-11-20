using JXReborn.Adapter.Legacy.Packets;
using JXReborn.ClientDriver.Tests.DifferentialParity;
using Xunit;

namespace JXReborn.ClientDriver.Tests.WorldJourneys;

[Collection(HeadlessGameExeCollectionDefinition.Name)]
[Trait("Category", "Headless")]
public sealed class PlayerTradeHeadlessGameExeTests
{
    private const ushort BaLangHuyenMapId = 53;
    private const int SpotAMpsX = 51520;
    private const int SpotAMpsY = 102177;
    private const int SpotBMpsX = 51552;
    private const int SpotBMpsY = 102177;

    [Fact]
    public async Task GameExe_OpenAndCloseTradeState_ShouldBroadcastSyncMenuState0x76AndTradeState0x75()
    {
        await using var sutHarness = await HeadlessTestServerHarness.StartAsync();

        await using JxHeadlessClient clientA = await JxHeadlessClient.SpawnAsync(new JxHeadlessOptions
        {
            GatewayPort = sutHarness.BishopPort,
        });
        await using JxHeadlessClient clientB = await JxHeadlessClient.SpawnAsync(new JxHeadlessOptions
        {
            GatewayPort = sutHarness.BishopPort,
        });

        WorldDomSnapshot snapA = await clientA.LoginAndEnterWorldAsync("127.0.0.1", sutHarness.BishopPort, "tradeknba", "123123", "TradeKnbA", 0, 0, BaLangHuyenMapId);
        WorldDomSnapshot snapB = await clientB.LoginAndEnterWorldAsync("127.0.0.1", sutHarness.BishopPort, "tradeknbb", "123123", "TradeKnbB", 0, 0, BaLangHuyenMapId);

        _ = await clientA.MoveToAsync(SpotAMpsX, SpotAMpsY, mode: 1);
        _ = await clientB.MoveToAsync(SpotBMpsX, SpotBMpsY, mode: 1);
        snapA = await clientA.WaitUntilAsync(
            s => double.Hypot(s.Player.MpsX - SpotAMpsX, s.Player.MpsY - SpotAMpsY) <= 128.0 && s.Npcs.Any(n => !n.IsSelf && n.DwId == snapB.Player.DwId),
            TimeSpan.FromSeconds(10));
        snapB = await clientB.WaitUntilAsync(
            s => double.Hypot(s.Player.MpsX - SpotBMpsX, s.Player.MpsY - SpotBMpsY) <= 128.0 && s.Npcs.Any(n => !n.IsSelf && n.DwId == snapA.Player.DwId),
            TimeSpan.FromSeconds(10));

        uint s2cSeqA = snapA.RecentS2c.Count > 0 ? snapA.RecentS2c.Max(p => p.Seq) : 0u;
        uint s2cSeqB = snapB.RecentS2c.Count > 0 ? snapB.RecentS2c.Max(p => p.Seq) : 0u;

        // 1st UI click on Trade button: Switch([[trade]]) -> KCoreShell::OperationRequest(0x14, 0, 1) @ 0x0053c62a
        // Must emit C2S 0x69 -> S2C 0x75 (Trade.State == 2) + S2C 0x76 (MenuState == 2 -> menustate02.spr KNB icon on head!)
        _ = await clientA.RawOperationAsync(0x14, 0, 1);

        snapA = await clientA.WaitUntilAsync(
            s => s.Trade.State == 2 && s.Player.MenuState == 2 && s.RecentS2c.Any(p => p.Seq > s2cSeqA && p.Opcode == ProtocolOpcodes.s2c_syncmenustate),
            TimeSpan.FromSeconds(5));
        snapB = await clientB.WaitUntilAsync(
            s => s.RecentS2c.Any(p => p.Seq > s2cSeqB && p.Opcode == ProtocolOpcodes.s2c_syncmenustate) &&
                 s.Npcs.Any(n => !n.IsSelf && n.DwId == snapA.Player.DwId && n.MenuState == 2),
            TimeSpan.FromSeconds(5));

        Assert.Equal(2, snapA.Trade.State);
        Assert.Equal(2, snapA.Player.MenuState);
        Assert.EndsWith("menustate02.spr", snapA.Player.MenuStateSpr, StringComparison.OrdinalIgnoreCase);

        NpcSnapshot observedOpenA = Assert.Single(snapB.Npcs, n => !n.IsSelf && n.DwId == snapA.Player.DwId);
        Assert.Equal(2, observedOpenA.MenuState);
        Assert.EndsWith("menustate02.spr", observedOpenA.MenuStateSpr, StringComparison.OrdinalIgnoreCase);

        // Late-joining observer entering AOI must also receive MenuState == 2 via S2C 0x4C (KNpc::SendSyncData @ 0x080994b0)
        await using JxHeadlessClient clientC = await JxHeadlessClient.SpawnAsync(new JxHeadlessOptions
        {
            GatewayPort = sutHarness.BishopPort,
        });
        WorldDomSnapshot snapC = await clientC.LoginAndEnterWorldAsync("127.0.0.1", sutHarness.BishopPort, "tradeknbc", "123123", "TradeKnbC", 0, 0, BaLangHuyenMapId);
        _ = await clientC.MoveToAsync(SpotBMpsX, SpotBMpsY, mode: 1);
        snapC = await clientC.WaitUntilAsync(
            s => s.Npcs.Any(n => !n.IsSelf && n.DwId == snapA.Player.DwId),
            TimeSpan.FromSeconds(10));
        NpcSnapshot lateObservedA = Assert.Single(snapC.Npcs, n => !n.IsSelf && n.DwId == snapA.Player.DwId);
        Assert.Equal(2, lateObservedA.MenuState);
        Assert.EndsWith("menustate02.spr", lateObservedA.MenuStateSpr, StringComparison.OrdinalIgnoreCase);

        uint closeSeqA = snapA.RecentS2c.Max(p => p.Seq);
        uint closeSeqB = snapB.RecentS2c.Max(p => p.Seq);
        uint c2sCloseSeqA = snapA.RecentC2s.Count > 0 ? snapA.RecentC2s.Max(p => p.Seq) : 0u;

        // 2nd UI click on Trade button: Switch([[trade]]) -> KCoreShell::OperationRequest(0x14, 0, 1) @ 0x0053c641
        // Because SelfNpc.GetMenuState() == 2, game.exe MUST send C2S 0x6A (c2s_tradeapplystateclose) and return to Normal (0, empty spr)!
        _ = await clientA.RawOperationAsync(0x14, 0, 1);

        snapA = await clientA.WaitUntilAsync(
            s => s.Trade.State == 0 && s.Player.MenuState == 0 && s.RecentS2c.Any(p => p.Seq > closeSeqA && p.Opcode == ProtocolOpcodes.s2c_syncmenustate),
            TimeSpan.FromSeconds(5));
        snapB = await clientB.WaitUntilAsync(
            s => s.RecentS2c.Any(p => p.Seq > closeSeqB && p.Opcode == ProtocolOpcodes.s2c_syncmenustate) &&
                 s.Npcs.Any(n => !n.IsSelf && n.DwId == snapA.Player.DwId && n.MenuState == 0),
            TimeSpan.FromSeconds(5));

        Assert.Contains(snapA.RecentC2s, p => p.Seq > c2sCloseSeqA && p.Opcode == ProtocolOpcodes.c2s_tradeapplystateclose);
        Assert.Equal(0, snapA.Trade.State);
        Assert.Equal(0, snapA.Player.MenuState);
        Assert.Equal(string.Empty, snapA.Player.MenuStateSpr);

        NpcSnapshot observedClosedA = Assert.Single(snapB.Npcs, n => !n.IsSelf && n.DwId == snapA.Player.DwId);
        Assert.Equal(0, observedClosedA.MenuState);
        Assert.Equal(string.Empty, observedClosedA.MenuStateSpr);
    }

    [Fact]
    public async Task GameExe_InviteAcceptLockAndConfirmTrade_ShouldDeliver0x8B_0x81_0x78AndResetMenuStateToNormal()
    {
        await using var sutHarness = await HeadlessTestServerHarness.StartAsync();

        await using JxHeadlessClient clientA = await JxHeadlessClient.SpawnAsync(new JxHeadlessOptions
        {
            GatewayPort = sutHarness.BishopPort,
        });
        await using JxHeadlessClient clientB = await JxHeadlessClient.SpawnAsync(new JxHeadlessOptions
        {
            GatewayPort = sutHarness.BishopPort,
        });

        WorldDomSnapshot snapA = await clientA.LoginAndEnterWorldAsync("127.0.0.1", sutHarness.BishopPort, "tradeswapa", "123123", "TradeSwapA", 0, 0, BaLangHuyenMapId);
        WorldDomSnapshot snapB = await clientB.LoginAndEnterWorldAsync("127.0.0.1", sutHarness.BishopPort, "tradeswapb", "123123", "TradeSwapB", 0, 0, BaLangHuyenMapId);

        _ = await clientA.MoveToAsync(SpotAMpsX, SpotAMpsY, mode: 1);
        _ = await clientB.MoveToAsync(SpotBMpsX, SpotBMpsY, mode: 1);
        snapA = await clientA.WaitUntilAsync(
            s => double.Hypot(s.Player.MpsX - SpotAMpsX, s.Player.MpsY - SpotAMpsY) <= 128.0 && s.Npcs.Any(n => !n.IsSelf && n.DwId == snapB.Player.DwId),
            TimeSpan.FromSeconds(10));
        snapB = await clientB.WaitUntilAsync(
            s => double.Hypot(s.Player.MpsX - SpotBMpsX, s.Player.MpsY - SpotBMpsY) <= 128.0 && s.Npcs.Any(n => !n.IsSelf && n.DwId == snapA.Player.DwId),
            TimeSpan.FromSeconds(10));

        // Only B clicks Trade button (KNB open = 2); A stays Normal (0) and invites B!
        _ = await clientB.RawOperationAsync(0x14, 0, 1);
        snapB = await clientB.WaitUntilAsync(s => s.Player.MenuState == 2, TimeSpan.FromSeconds(5));

        NpcSnapshot bInA = Assert.Single(snapA.Npcs, n => !n.IsSelf && n.DwId == snapB.Player.DwId);
        _ = await clientA.InviteTradeAsync(snapB.Player.DwId, bInA.ArrayIndex);

        snapB = await clientB.WaitUntilAsync(s => s.Trade.PendingInvites.Count > 0, TimeSpan.FromSeconds(5));
        PendingTradeInviteSnapshot invite = Assert.Single(snapB.Trade.PendingInvites);
        Assert.Equal(snapA.Player.DwId, invite.InviterNpcDwId);

        _ = await clientB.ReplyTradeInviteAsync(accept: true, inviterPlayerIdx: invite.InviterPlayerIdx);

        // Both enter Trading (Trade.State == 3, MenuState == 3 -> menustate03.spr)
        snapA = await clientA.WaitUntilAsync(s => s.Trade.State == 3 && s.Player.MenuState == 3, TimeSpan.FromSeconds(5));
        snapB = await clientB.WaitUntilAsync(s => s.Trade.State == 3 && s.Player.MenuState == 3, TimeSpan.FromSeconds(5));

        _ = await clientA.LockTradeAsync(true);
        _ = await clientB.LockTradeAsync(true);

        snapA = await clientA.WaitUntilAsync(s => s.Trade.SelfLocked && s.Trade.OtherLocked, TimeSpan.FromSeconds(5));
        snapB = await clientB.WaitUntilAsync(s => s.Trade.SelfLocked && s.Trade.OtherLocked, TimeSpan.FromSeconds(5));

        _ = await clientA.ConfirmTradeAsync(accept: true);
        _ = await clientB.ConfirmTradeAsync(accept: true);

        // Trade completes via S2C 0x78 (1) + 0x75 (0) + 0x76 (0): both return to Normal (Trade.State == 0, MenuState == 0, empty spr)!
        snapA = await clientA.WaitUntilAsync(s => s.Trade.State == 0 && s.Player.MenuState == 0 && s.Trade.LastDecisionResult == 1, TimeSpan.FromSeconds(5));
        snapB = await clientB.WaitUntilAsync(s => s.Trade.State == 0 && s.Player.MenuState == 0 && s.Trade.LastDecisionResult == 1, TimeSpan.FromSeconds(5));

        Assert.Equal(string.Empty, snapA.Player.MenuStateSpr);
        Assert.Equal(string.Empty, snapB.Player.MenuStateSpr);
    }
}
