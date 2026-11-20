using System.Buffers.Binary;
using System.Globalization;
using JXReborn.Adapter.Legacy.Packets;
using JXReborn.ClientDriver.Tests.DifferentialParity;
using JXReborn.Contracts.Characters;
using JXReborn.Domain.Characters;
using JXReborn.GameAssets.WorldMaps;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace JXReborn.ClientDriver.Tests.WorldJourneys;

/// <summary>
/// Single-run headless <c>game.exe</c> smoke verification covering Ba Lăng Huyện QA checks:
/// entry <c>0xCC</c> resist sync, valid starter items, <c>0x7E</c>/<c>0x9F</c> portrait update + SQLite relog persistence,
/// <c>0x79</c>/<c>0x96</c> ladder query, <c>0xFB 0x20</c> relay whisper, Pharmacist Shop <c>39</c> (wire <c>38</c>) medicine purchase,
/// and stationary North Gate dummy combat.
/// </summary>
[Collection(HeadlessGameExeCollectionDefinition.Name)]
public sealed class BaLangQaHeadlessGameExeTests
{
    private const ushort BaLangHuyenMapId = 53;
    private const uint NorthGateTrapId = 2476324320u;
    private const uint RawOpQueryRankInformation = 0x36u;
    private const uint RawOpQueryRankParamPtr = 0x006384C8u; // Aligned uint32(1) in game.exe .rdata
    private const uint RawOpSetPortrait = 0x3Fu;
    private const int TestPortraitId = 7;
    private const int PharmacistMpsX = 51150;
    private const int PharmacistMpsY = 102713;
    private const int SandbagDummyMpsX = 52459;
    private const int SandbagDummyMpsY = 95180;
    private const int WoodenDummyMpsX = 53162;
    private const int WoodenDummyMpsY = 95035;
    private const string WhisperMessageText = "Chao Ba Lang QA";
    private const string NearbyMessageText = "Chao Phu Can QA";

    private readonly ITestOutputHelper _output;

    public BaLangQaHeadlessGameExeTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    [Trait("Category", "Headless")]
    public async Task RealGameExe_EnterBaLangVerifyResistPortraitLadderWhisperPharmacistAndDummy_ShouldSucceedEndToEnd()
    {
        await using var sutHarness = await HeadlessTestServerHarness.StartAsync(
            enableTestGmToken: true,
            autoGrantGmTokenOnLogin: true);
        var mapCatalog = sutHarness.Services.GetRequiredService<IMapCatalog>();
        ClientPakObstacleReader? clientPakObstacles = ClientPakObstacleReader.TryOpenFromTemplate();

        var sutRes = await ExecuteBaLangQaJourneyAsync(
            label: "SUT",
            host: "127.0.0.1",
            bishopPort: sutHarness.BishopPort,
            password: "pass123",
            accountA: "gmqahdl1",
            roleNameA: "BaLangHdlA",
            accountB: "gmqahdl2",
            roleNameB: "BaLangHdlB",
            mapCatalog: mapCatalog,
            clientPakObstacles: clientPakObstacles);

        Assert.True(sutRes.SawResistSync0xCC, "Expected 102-byte s2c_syncresist (0xCC) upon entering Map 53.");
        Assert.Equal(-25, sutRes.BaseFireResist);
        Assert.Equal(25, sutRes.BasePoisonResist);
        Assert.Equal(0, sutRes.BasePhysicsResist);
        Assert.True(sutRes.HasMetalStarterAxe, "Expected starter Metal axe (0,0,4) in inventory.");
        Assert.False(sutRes.HasUnknownStarterItem, "Starter inventory must not contain any 'Bất minh' unknown item.");
        Assert.Equal("9F0500000007000000", sutRes.PortraitHex0x9F.ToUpperInvariant());
        Assert.Equal(466, sutRes.LadderPacketLen0x96);
        Assert.InRange(sutRes.LadderFirstValue, 0L, 1_000_000L);
        Assert.True(sutRes.NearbyDelivered, "Expected nearby chat to reach recipient B in Map 53.");
        Assert.True(sutRes.WhisperDelivered, "Expected relay private whisper (0xFB 0x20) to reach recipient B.");
        Assert.True(sutRes.NovicePkSwitchedAllModes, "Expected Novice (Camp == 0) in Ba Lang Huyen to switch Peace -> Fight -> Slaughter -> Peace.");
        Assert.True(sutRes.NovicePartyFormed, "Expected Novice party invite/accept to form a 2-member party and disband cleanly.");
        Assert.Equal(38, sutRes.PharmacistWireShopId);
        Assert.True(sutRes.BoughtMedicinePresent, "Expected Pharmacist shop purchase to land in EquipRoom (Place == 3).");
        Assert.Equal(1, sutRes.BoughtItemGenre);
        Assert.True(sutRes.EnteredNorthWilderness, "Expected North Gate (2476324320) transition to FightMode == 1.");
        Assert.True(sutRes.FoundDummy, "Expected live wooden dummy / sandbag outside North Gate.");
        Assert.Equal(0.0, sutRes.DummyMovedDistance);
        Assert.InRange(sutRes.DummyExpDelta, 1L, long.MaxValue);
        Assert.True(sutRes.SawExpSystemMessage, "Expected EXP system message notification after defeating dummy.");
        Assert.Equal(0, sutRes.NewDropsNearDummy);

        // Verify SQLite immediately persisted PortraitId = 7 AND relogging emits 0x9F + UI 10287 ladder has clean entry
        var characterRepo = sutHarness.Services.GetRequiredService<ICharacterRepository>();
        Character? persistedA = await characterRepo.FindByNormalizedRoleNameAsync("BALANGHDLA");
        Assert.NotNull(persistedA);
        Assert.Equal((byte)TestPortraitId, persistedA!.PortraitId);

        var characterAppService = sutHarness.Services.GetRequiredService<ICharacterAppService>();
        IReadOnlyList<LadderRoleEntryDto> uiWorldLevel = await characterAppService.GetLadderTopTenAsync(10287);
        Assert.NotEmpty(uiWorldLevel);

        await using JxHeadlessClient relogClientA = await JxHeadlessClient.SpawnAsync(new JxHeadlessOptions
        {
            ServerIp = "127.0.0.1",
            GatewayPort = sutHarness.BishopPort
        });
        WorldDomSnapshot relogSnapA = await relogClientA.LoginAndEnterWorldAsync(
            "127.0.0.1", sutHarness.BishopPort, "gmqahdl1", "pass123", "BaLangHdlA", gender: 0, series: 0, nativePlaceId: BaLangHuyenMapId);
        S2cPacketSnapshot? relogPortrait = relogSnapA.RecentS2c.LastOrDefault(
            static p => p.Opcode == ProtocolOpcodes.s2c_setportrait && p.Length == 9);
        Assert.NotNull(relogPortrait);
        Assert.Equal("9F0500000007000000", relogPortrait!.HexPreview.ToUpperInvariant());
    }

    private async Task<(
        bool SawResistSync0xCC,
        int BaseFireResist,
        int BasePoisonResist,
        int BasePhysicsResist,
        bool HasMetalStarterAxe,
        bool HasUnknownStarterItem,
        string PortraitHex0x9F,
        int LadderPacketLen0x96,
        long LadderFirstValue,
        bool NearbyDelivered,
        bool WhisperDelivered,
        bool NovicePkSwitchedAllModes,
        bool NovicePartyFormed,
        int PharmacistWireShopId,
        bool BoughtMedicinePresent,
        int BoughtItemGenre,
        bool EnteredNorthWilderness,
        bool FoundDummy,
        double DummyMovedDistance,
        long DummyExpDelta,
        bool SawExpSystemMessage,
        int NewDropsNearDummy)> ExecuteBaLangQaJourneyAsync(
        string label,
        string host,
        int bishopPort,
        string password,
        string accountA,
        string roleNameA,
        string accountB,
        string roleNameB,
        IMapCatalog mapCatalog,
        ClientPakObstacleReader? clientPakObstacles)
    {
        await using JxHeadlessClient clientA = await JxHeadlessClient.SpawnAsync(new JxHeadlessOptions
        {
            ServerIp = host,
            GatewayPort = bishopPort
        });
        await using JxHeadlessClient clientB = await JxHeadlessClient.SpawnAsync(new JxHeadlessOptions
        {
            ServerIp = host,
            GatewayPort = bishopPort
        });

        WorldDomSnapshot snapA = await clientA.LoginAndEnterWorldAsync(
            host, bishopPort, accountA, password, roleNameA, gender: 0, series: 0, nativePlaceId: BaLangHuyenMapId);
        S2cPacketSnapshot? resistPacket = snapA.RecentS2c.LastOrDefault(
            static p => p.Opcode == ProtocolOpcodes.s2c_syncresist && p.Length == SyncResistS2CPacket.PayloadSize);
        bool sawResistSync0xCC = resistPacket is not null;
        int baseFireResist = 17;
        int basePoisonResist = 17;
        int basePhysicsResist = 17;
        if (resistPacket is not null &&
            TryParseHexBytes(resistPacket.HexPreview, out byte[] resistBytes) &&
            SyncResistS2CPacket.TryParse(resistBytes, out SyncResistS2CPacket parsedResist))
        {
            baseFireResist = parsedResist.BaseFireResist;
            basePoisonResist = parsedResist.BasePoisonResist;
            basePhysicsResist = parsedResist.BasePhysicsResist;
        }

        bool hasMetalStarterAxe = snapA.Items.Any(
            static i => i.Genre == 0 && i.DetailType == 0 && i.ParticularType == 4);
        bool hasUnknownStarterItem = snapA.Items.Any(
            static i => i.Name.Contains("Bất minh", StringComparison.OrdinalIgnoreCase));

        WorldDomSnapshot snapB = await clientB.LoginAndEnterWorldAsync(
            host, bishopPort, accountB, password, roleNameB, gender: 1, series: 2, nativePlaceId: BaLangHuyenMapId);

        snapA = await clientA.DismissSystemLoginHintIfOpenAsync();
        snapB = await clientB.DismissSystemLoginHintIfOpenAsync();
        if (snapA.Player.MapId != BaLangHuyenMapId || snapA.Player.FightMode != 0)
        {
            snapA = await TestGmTokenDriver.TeleportToCityAsync(clientA, TestGmTokenDriver.TeleportBaLang53, BaLangHuyenMapId);
        }

        if (snapB.Player.MapId != BaLangHuyenMapId || snapB.Player.FightMode != 0)
        {
            snapB = await TestGmTokenDriver.TeleportToCityAsync(clientB, TestGmTokenDriver.TeleportBaLang53, BaLangHuyenMapId);
        }

        // 1. Portrait update (GOI_SET_PORTRAIT = 0x3F -> C2S 0x7E -> S2C 0x9F 9B)
        uint s2cBeforePortrait = GetMaxS2cSeq(snapA);
        _ = await clientA.RawOperationAsync(RawOpSetPortrait, 0u, TestPortraitId);
        snapA = await TryWaitUntilOrLatestAsync(
            clientA,
            s => s.RecentS2c.Any(p => p.Seq > s2cBeforePortrait && p.Opcode == ProtocolOpcodes.s2c_setportrait && p.Length == 9),
            TimeSpan.FromSeconds(6));
        S2cPacketSnapshot? portraitPacket = snapA.RecentS2c.LastOrDefault(
            p => p.Seq > s2cBeforePortrait && p.Opcode == ProtocolOpcodes.s2c_setportrait && p.Length == 9);
        string portraitHex = portraitPacket?.HexPreview ?? string.Empty;

        // 2. Ladder query (GOI_QUERY_RANK_INFORMATION = 0x36, &1 in .rdata = 0x006384C8 -> C2S 0x79 -> S2C 0x96 466B)
        uint s2cBeforeLadder = GetMaxS2cSeq(snapA);
        _ = await clientA.RawOperationAsync(RawOpQueryRankInformation, RawOpQueryRankParamPtr, 1);
        snapA = await TryWaitUntilOrLatestAsync(
            clientA,
            s => s.RecentS2c.Any(p => p.Seq > s2cBeforeLadder && p.Opcode == ProtocolOpcodes.s2c_ladderresult && p.Length == 466),
            TimeSpan.FromSeconds(6));
        S2cPacketSnapshot? ladderPacket = snapA.RecentS2c.LastOrDefault(
            p => p.Seq > s2cBeforeLadder && p.Opcode == ProtocolOpcodes.s2c_ladderresult && p.Length == 466);
        int ladderLen = ladderPacket?.Length ?? 0;
        long ladderFirstValue = -1L;
        if (ladderPacket is not null &&
            TryParseHexBytes(ladderPacket.HexPreview, out byte[] ladderBytes) &&
            ladderBytes.Length >= 46)
        {
            ladderFirstValue = BinaryPrimitives.ReadInt64LittleEndian(ladderBytes.AsSpan(6 + 32, 8));
        }

        // 3. Bring A & B within Ba Lang town AOI, verify Nearby chat + Relay Whisper + Novice PK + Novice Party
        _ = await clientA.MoveToAsync(51200, 102400, mode: 1);
        _ = await clientB.MoveToAsync(51232, 102400, mode: 1);
        snapB = await clientB.WaitUntilAsync(s => s.Npcs.Any(n => !n.IsSelf && n.DwId == snapA.Player.DwId), TimeSpan.FromSeconds(8));
        snapA = await clientA.WaitUntilAsync(s => s.Npcs.Any(n => !n.IsSelf && n.DwId == snapB.Player.DwId), TimeSpan.FromSeconds(8));

        snapA = await WaitTicksAsync(clientA, 24);
        snapB = await clientB.GetStateAsync();
        uint chatBeforeNearby = GetMaxChatSeq(snapB);
        _ = await clientA.SendChatAsync(channel: 2, messageUnicode: NearbyMessageText);
        snapB = await TryWaitUntilOrLatestAsync(
            clientB,
            s => s.ChatMessages.Any(m => m.Seq > chatBeforeNearby && m.Text.Contains(NearbyMessageText, StringComparison.Ordinal)),
            TimeSpan.FromSeconds(6));
        bool nearbyDelivered = snapB.ChatMessages.Any(m => m.Seq > chatBeforeNearby && m.Text.Contains(NearbyMessageText, StringComparison.Ordinal));

        // Grant 50,000 silver via GM Token before whisper so clientA satisfies chatcost.ini (10 silver for PrivateWhisper)
        snapA = await TestGmTokenDriver.InvokePathAsync(clientA, TestGmTokenDriver.PathMoney50k);
        snapA = await WaitTicksAsync(clientA, 24);
        snapB = await clientB.GetStateAsync();
        uint chatBeforeWhisper = GetMaxChatSeq(snapB);
        _ = await clientA.SendWhisperAsync(roleNameB, WhisperMessageText);
        snapB = await TryWaitUntilOrLatestAsync(
            clientB,
            s => s.ChatMessages.Any(m =>
                m.Seq > chatBeforeWhisper &&
                m.ChannelId == 3u &&
                m.Text.Contains(WhisperMessageText, StringComparison.Ordinal)),
            TimeSpan.FromSeconds(6));
        bool whisperDelivered = snapB.ChatMessages.Any(m =>
            m.Seq > chatBeforeWhisper &&
            m.ChannelId == 3u &&
            m.Text.Contains(WhisperMessageText, StringComparison.Ordinal));

        // 3b. Novice PK switch (Peace 0 -> Fight 1 -> Slaughter 2 -> Peace 0)
        bool novicePkSwitchedAllModes = false;
        if (snapB.Player.Camp == 0 && snapB.Player.FightMode == 0)
        {
            _ = await clientB.SwitchPkAsync(1);
            snapB = await TryWaitUntilOrLatestAsync(clientB, static s => s.Player.PkFlag == 1, TimeSpan.FromSeconds(4));
            bool toFight = snapB.Player.PkFlag == 1;
            _ = await clientB.SwitchPkAsync(2);
            snapB = await TryWaitUntilOrLatestAsync(clientB, static s => s.Player.PkFlag == 2, TimeSpan.FromSeconds(4));
            bool toSlaughter = snapB.Player.PkFlag == 2;
            _ = await clientB.SwitchPkAsync(0);
            snapB = await TryWaitUntilOrLatestAsync(clientB, static s => s.Player.PkFlag == 0, TimeSpan.FromSeconds(4));
            bool backToPeace = snapB.Player.PkFlag == 0;
            novicePkSwitchedAllModes = toFight && toSlaughter && backToPeace;
        }

        // 3c. Novice Party Create -> Invite -> Accept -> Verify 2 members -> Leave cleanly
        if (!snapA.Team.InTeam)
        {
            _ = await clientA.CreateTeamAsync();
            snapA = await TryWaitUntilOrLatestAsync(clientA, static s => s.Team.InTeam && s.Team.IsCaptain, TimeSpan.FromSeconds(4));
        }

        if (snapB.Team.InTeam)
        {
            _ = await clientB.LeaveTeamAsync();
            snapB = await TryWaitUntilOrLatestAsync(clientB, static s => !s.Team.InTeam, TimeSpan.FromSeconds(4));
        }

        _ = await clientA.InviteToTeamAsync(snapB.Player.DwId);
        snapB = await TryWaitUntilOrLatestAsync(clientB, static s => s.Team.PendingInvites.Count > 0, TimeSpan.FromSeconds(5));
        bool novicePartyFormed = false;
        if (snapB.Team.PendingInvites.Count > 0)
        {
            _ = await clientB.ReplyTeamInviteAsync(accept: true);
            snapA = await TryWaitUntilOrLatestAsync(clientA, static s => s.Team.InTeam && s.Team.MemberCount == 2, TimeSpan.FromSeconds(5));
            snapB = await TryWaitUntilOrLatestAsync(clientB, static s => s.Team.InTeam && s.Team.MemberCount == 2, TimeSpan.FromSeconds(5));
            novicePartyFormed = snapA.Team.InTeam && snapA.Team.MemberCount == 2 && snapB.Team.InTeam && snapB.Team.MemberCount == 2;
            _ = await clientB.LeaveTeamAsync();
            _ = await clientA.LeaveTeamAsync();
            snapA = await TryWaitUntilOrLatestAsync(clientA, static s => !s.Team.InTeam, TimeSpan.FromSeconds(4));
            snapB = await TryWaitUntilOrLatestAsync(clientB, static s => !s.Team.InTeam, TimeSpan.FromSeconds(4));
        }

        // 4. Ensure clientA is in Map 53 town (with GM silver already granted above), then buy medicine from Pharmacist
        if (snapA.Player.MapId != BaLangHuyenMapId || snapA.Player.FightMode != 0)
        {
            snapA = await TestGmTokenDriver.TeleportToCityAsync(clientA, TestGmTokenDriver.TeleportBaLang53, BaLangHuyenMapId);
        }

        IReadOnlyList<(int MpsX, int MpsY)> pharmWaypoints = MapObstaclePathfinder.FindWaypointsNearPosition(
            mapCatalog,
            BaLangHuyenMapId,
            snapA.Player.MpsX,
            snapA.Player.MpsY,
            PharmacistMpsX,
            PharmacistMpsY,
            stopRadiusCells: 1,
            minDistanceCells: 1,
            clientPakObstacles: clientPakObstacles);
        foreach ((int wx, int wy) in pharmWaypoints)
        {
            snapA = await MoveToPointAsync(clientA, BaLangHuyenMapId, wx, wy);
        }

        snapA = await clientA.WaitUntilAsync(
            s => s.Npcs.Any(n => !n.IsSelf && n.Kind == 3 && ComputeDistance(n.MpsX, n.MpsY, PharmacistMpsX, PharmacistMpsY) <= 192.0),
            TimeSpan.FromSeconds(8));
        NpcSnapshot pharmacist = snapA.Npcs
            .Where(n => !n.IsSelf && n.Kind == 3 && ComputeDistance(n.MpsX, n.MpsY, PharmacistMpsX, PharmacistMpsY) <= 192.0)
            .OrderBy(n => ComputeDistance(n.MpsX, n.MpsY, PharmacistMpsX, PharmacistMpsY))
            .First();

        uint prevDialogSeq = snapA.Dialog.Sequence;
        _ = await clientA.InteractNpcByDwIdAsync(pharmacist.DwId);
        snapA = await clientA.WaitUntilAsync(
            s => s.Dialog.IsOpen && s.Dialog.Sequence > prevDialogSeq,
            TimeSpan.FromSeconds(6));

        uint s2cBeforeShop = GetMaxS2cSeq(snapA);
        _ = await clientA.SelectDialogAsync(0);
        snapA = await TryWaitUntilOrLatestAsync(
            clientA,
            s => s.RecentS2c.Any(p => p.Seq > s2cBeforeShop && p.Opcode == ProtocolOpcodes.s2c_opensalebox && p.Length == 13),
            TimeSpan.FromSeconds(6));
        S2cPacketSnapshot? openSale = snapA.RecentS2c.LastOrDefault(
            p => p.Seq > s2cBeforeShop && p.Opcode == ProtocolOpcodes.s2c_opensalebox && p.Length == 13);
        int pharmacistWireShopId = -1;
        if (openSale is not null &&
            TryParseHexBytes(openSale.HexPreview, out byte[] openSaleBytes) &&
            OpenSaleBoxS2CPacket.TryParse(openSaleBytes, out OpenSaleBoxS2CPacket parsedSale))
        {
            pharmacistWireShopId = parsedSale.ShopId;
        }

        (int freeGridX, int freeGridY) = FindFreeBagCell(snapA.Items, place: 3);
        HashSet<uint> existingBagIds = snapA.Items.Where(static i => i.Place == 3).Select(static i => i.ServerItemId).ToHashSet();
        _ = await clientA.BuyShopItemAsync(buyIndex: 0, place: 3, gridX: freeGridX, gridY: freeGridY);
        snapA = await WaitTicksAsync(clientA, 18);
        InventoryItemSnapshot? boughtMedicine = snapA.Items.FirstOrDefault(i => i.Place == 3 && !existingBagIds.Contains(i.ServerItemId));

        _output.WriteLine(
            $"[{label}] 0xCC={sawResistSync0xCC} 0x9F='{portraitHex}' 0x96Len={ladderLen} Whisper={whisperDelivered} " +
            $"SaleHex={openSale?.HexPreview} ShopId={pharmacistWireShopId} Bought={boughtMedicine?.Name} Genre={boughtMedicine?.Genre}");

        // 5. Exit North Gate (2476324320) using Level-1 clientB -> verify stationary dummy and kill for EXP + \S message + 0 drops
        IReadOnlyList<(int MpsX, int MpsY)> gateWaypoints = MapObstaclePathfinder.FindWaypointsToTrap(
            mapCatalog,
            BaLangHuyenMapId,
            snapB.Player.MpsX,
            snapB.Player.MpsY,
            NorthGateTrapId,
            clientPakObstacles);
        for (int i = 0; i < gateWaypoints.Count - 1; i++)
        {
            snapB = await MoveToPointAsync(clientB, BaLangHuyenMapId, gateWaypoints[i].MpsX, gateWaypoints[i].MpsY);
        }

        _ = await clientB.MoveToAsync(gateWaypoints[^1].MpsX, gateWaypoints[^1].MpsY, mode: 1);
        snapB = await TryWaitUntilOrLatestAsync(clientB, static s => s.Player.FightMode == 1, TimeSpan.FromSeconds(10));
        bool enteredNorthWilderness = snapB.Player.FightMode == 1;

        IReadOnlyList<(int MpsX, int MpsY)> dummyWaypoints = MapObstaclePathfinder.FindWaypointsNearPosition(
            mapCatalog,
            BaLangHuyenMapId,
            snapB.Player.MpsX,
            snapB.Player.MpsY,
            SandbagDummyMpsX,
            SandbagDummyMpsY,
            stopRadiusCells: 2,
            minDistanceCells: 1,
            clientPakObstacles: clientPakObstacles);
        foreach ((int wx, int wy) in dummyWaypoints)
        {
            snapB = await MoveToPointAsync(clientB, BaLangHuyenMapId, wx, wy);
        }

        snapB = await TryWaitUntilOrLatestAsync(
            clientB,
            s => s.Npcs.Any(IsTargetDummy),
            TimeSpan.FromSeconds(8));
        NpcSnapshot? dummy = snapB.Npcs
            .Where(IsTargetDummy)
            .OrderBy(n => ComputeDistance(snapB.Player.MpsX, snapB.Player.MpsY, n.MpsX, n.MpsY))
            .FirstOrDefault();

        bool foundDummy = dummy is not null;
        double dummyMovedDistance = -1.0;
        long dummyExpDelta = 0L;
        bool sawExpSysMsg = false;
        int newDropsNearDummy = -1;

        if (dummy is not null)
        {
            int startDummyX = dummy.MpsX;
            int startDummyY = dummy.MpsY;
            snapB = await WaitTicksAsync(clientB, 20);
            NpcSnapshot? afterWaitDummy = snapB.Npcs.FirstOrDefault(n => n.DwId == dummy.DwId && n.Life > 0);
            if (afterWaitDummy is not null)
            {
                dummyMovedDistance = ComputeDistance(startDummyX, startDummyY, afterWaitDummy.MpsX, afterWaitDummy.MpsY);
                if (ComputeDistance(snapB.Player.MpsX, snapB.Player.MpsY, afterWaitDummy.MpsX, afterWaitDummy.MpsY) > 128.0)
                {
                    IReadOnlyList<(int MpsX, int MpsY)> closeWps = MapObstaclePathfinder.FindWaypointsNearPosition(
                        mapCatalog,
                        BaLangHuyenMapId,
                        snapB.Player.MpsX,
                        snapB.Player.MpsY,
                        afterWaitDummy.MpsX,
                        afterWaitDummy.MpsY,
                        stopRadiusCells: 1,
                        minDistanceCells: 0,
                        clientPakObstacles: clientPakObstacles);
                    foreach ((int wx, int wy) in closeWps)
                    {
                        snapB = await MoveToPointAsync(clientB, BaLangHuyenMapId, wx, wy);
                    }
                }
            }

            long expBefore = snapB.Player.Exp;
            int lvBefore = snapB.Player.Level;
            uint sysSeqBefore = GetMaxSysMsgSeq(snapB);
            HashSet<int> groundIdsBefore = snapB.GroundObjects.Select(static g => g.ObjectId).ToHashSet();
            int skillId = SelectBasicAttackSkillId(snapB);

            for (int swing = 0; swing < 20; swing++)
            {
                snapB = await clientB.GetStateAsync();
                NpcSnapshot? liveDummy = snapB.Npcs.FirstOrDefault(n => n.DwId == dummy.DwId);
                if (IsDummyDeadOrMissing(liveDummy))
                {
                    break;
                }

                if (ComputeDistance(snapB.Player.MpsX, snapB.Player.MpsY, liveDummy!.MpsX, liveDummy.MpsY) > 56.0)
                {
                    _ = await clientB.MoveToAsync(liveDummy.MpsX, liveDummy.MpsY, mode: 2);
                    uint moveTick = snapB.TickCount;
                    snapB = await TryWaitUntilOrLatestAsync(
                        clientB,
                        s =>
                        {
                            NpcSnapshot? m = s.Npcs.FirstOrDefault(n => n.DwId == dummy.DwId);
                            return m is null ||
                                   ComputeDistance(s.Player.MpsX, s.Player.MpsY, m.MpsX, m.MpsY) <= 44.0 ||
                                   (s.TickCount > moveTick && (s.TickCount - moveTick) >= 12u);
                        },
                        TimeSpan.FromSeconds(3));
                    liveDummy = snapB.Npcs.FirstOrDefault(n => n.DwId == dummy.DwId);
                    if (IsDummyDeadOrMissing(liveDummy))
                    {
                        break;
                    }
                }

                uint castTick = snapB.TickCount;
                _ = await clientB.UseSkillOnTargetAsync(skillId, dummy.DwId);
                snapB = await TryWaitUntilOrLatestAsync(
                    clientB,
                    s =>
                    {
                        NpcSnapshot? m = s.Npcs.FirstOrDefault(n => n.DwId == dummy.DwId);
                        if (IsDummyDeadOrMissing(m))
                        {
                            return true;
                        }

                        return s.TickCount > castTick && (s.TickCount - castTick) >= 14u;
                    },
                    TimeSpan.FromSeconds(3));
            }

            uint postKillTick = snapB.TickCount;
            snapB = await TryWaitUntilOrLatestAsync(
                clientB,
                s => s.Player.Exp > expBefore ||
                     s.Player.Level > lvBefore ||
                     (s.TickCount > postKillTick && (s.TickCount - postKillTick) >= 12u),
                TimeSpan.FromSeconds(3));
            snapB = await WaitTicksAsync(clientB, 4);

            dummyExpDelta = snapB.Player.Level > lvBefore
                ? Math.Max(1L, snapB.Player.Exp + 100L - expBefore)
                : (snapB.Player.Exp - expBefore);
            sawExpSysMsg = snapB.SystemMessages.Any(m => m.Seq > sysSeqBefore);
            newDropsNearDummy = snapB.GroundObjects.Count(g =>
                !groundIdsBefore.Contains(g.ObjectId) &&
                ComputeDistance(g.MpsX, g.MpsY, startDummyX, startDummyY) <= 192.0);

            _output.WriteLine(
                $"[{label}:Dummy] B_Lv={lvBefore}->{snapB.Player.Level} Moved={dummyMovedDistance:F1} " +
                $"ExpDelta={dummyExpDelta} SysMsg={sawExpSysMsg} Drops={newDropsNearDummy}");
        }

        if (snapB.Player.MapId != BaLangHuyenMapId || snapB.Player.FightMode != 0)
        {
            _ = await TestGmTokenDriver.TeleportToCityAsync(clientB, TestGmTokenDriver.TeleportBaLang53, BaLangHuyenMapId);
        }

        return (
            SawResistSync0xCC: sawResistSync0xCC,
            BaseFireResist: baseFireResist,
            BasePoisonResist: basePoisonResist,
            BasePhysicsResist: basePhysicsResist,
            HasMetalStarterAxe: hasMetalStarterAxe,
            HasUnknownStarterItem: hasUnknownStarterItem,
            PortraitHex0x9F: portraitHex,
            LadderPacketLen0x96: ladderLen,
            LadderFirstValue: ladderFirstValue,
            NearbyDelivered: nearbyDelivered,
            WhisperDelivered: whisperDelivered,
            NovicePkSwitchedAllModes: novicePkSwitchedAllModes,
            NovicePartyFormed: novicePartyFormed,
            PharmacistWireShopId: pharmacistWireShopId,
            BoughtMedicinePresent: boughtMedicine is not null,
            BoughtItemGenre: boughtMedicine?.Genre ?? -1,
            EnteredNorthWilderness: enteredNorthWilderness,
            FoundDummy: foundDummy,
            DummyMovedDistance: dummyMovedDistance,
            DummyExpDelta: dummyExpDelta,
            SawExpSystemMessage: sawExpSysMsg,
            NewDropsNearDummy: newDropsNearDummy);
    }

    private static bool IsDummyDeadOrMissing(NpcSnapshot? npc) =>
        npc is null || npc.Life <= 0 || npc.Doing == 21;

    private static int SelectBasicAttackSkillId(WorldDomSnapshot snapshot)
    {
        SkillSlotSnapshot? fistSkill = snapshot.Skills.FirstOrDefault(static s => s.SkillId == 53 && s.CurLevel > 0);
        if (fistSkill is not null)
        {
            return fistSkill.SkillId;
        }

        SkillSlotSnapshot? basic1 = snapshot.Skills.FirstOrDefault(static s => (s.SkillId == 1 || s.SkillId == 2) && s.CurLevel > 0);
        if (basic1 is not null)
        {
            return basic1.SkillId;
        }

        return snapshot.Skills.First(static s => s.CurLevel > 0).SkillId;
    }

    private static bool IsTargetDummy(NpcSnapshot n) =>
        !n.IsSelf &&
        n.Kind == 0 &&
        n.Life > 0 &&
        (ComputeDistance(n.MpsX, n.MpsY, SandbagDummyMpsX, SandbagDummyMpsY) <= 384.0 ||
         ComputeDistance(n.MpsX, n.MpsY, WoodenDummyMpsX, WoodenDummyMpsY) <= 384.0);

    private static (int GridX, int GridY) FindFreeBagCell(IReadOnlyList<InventoryItemSnapshot> items, int place)
    {
        var occupied = new bool[6, 10];
        foreach (InventoryItemSnapshot item in items)
        {
            if (item.Place != place)
            {
                continue;
            }

            int width = Math.Max(1, item.Width);
            int height = Math.Max(1, item.Height);
            for (int dx = 0; dx < width; dx++)
            {
                for (int dy = 0; dy < height; dy++)
                {
                    int gx = item.GridX + dx;
                    int gy = item.GridY + dy;
                    if (gx >= 0 && gx < 6 && gy >= 0 && gy < 10)
                    {
                        occupied[gx, gy] = true;
                    }
                }
            }
        }

        for (int gx = 0; gx < 6; gx++)
        {
            for (int gy = 0; gy < 10; gy++)
            {
                if (!occupied[gx, gy])
                {
                    return (gx, gy);
                }
            }
        }

        return (0, 0);
    }

    private static bool TryParseHexBytes(string hex, out byte[] bytes)
    {
        if (string.IsNullOrEmpty(hex) || hex.Length % 2 != 0)
        {
            bytes = [];
            return false;
        }

        bytes = new byte[hex.Length / 2];
        for (int i = 0; i < bytes.Length; i++)
        {
            if (!byte.TryParse(hex.AsSpan(i * 2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out bytes[i]))
            {
                return false;
            }
        }

        return true;
    }

    private static async Task<WorldDomSnapshot> MoveToPointAsync(
        JxHeadlessClient client,
        ushort expectedMapId,
        int targetMpsX,
        int targetMpsY)
    {
        WorldDomSnapshot before = await client.GetStateAsync();
        if (before.Player.MapId != expectedMapId || ComputeDistance(before.Player.MpsX, before.Player.MpsY, targetMpsX, targetMpsY) <= 56.0)
        {
            return before;
        }

        _ = await client.MoveToAsync(targetMpsX, targetMpsY, mode: 1);
        int lastX = before.Player.MpsX;
        int lastY = before.Player.MpsY;
        uint lastMovedTick = before.TickCount;

        return await client.WaitUntilAsync(
            s =>
            {
                if (s.Player.MapId != expectedMapId || ComputeDistance(s.Player.MpsX, s.Player.MpsY, targetMpsX, targetMpsY) <= 64.0)
                {
                    return true;
                }

                if (s.Player.MpsX != lastX || s.Player.MpsY != lastY)
                {
                    lastX = s.Player.MpsX;
                    lastY = s.Player.MpsY;
                    lastMovedTick = s.TickCount;
                    return false;
                }

                return s.TickCount > lastMovedTick && (s.TickCount - lastMovedTick) >= 16u;
            },
            TimeSpan.FromSeconds(8));
    }

    private static async Task<WorldDomSnapshot> TryWaitUntilOrLatestAsync(
        JxHeadlessClient client,
        Func<WorldDomSnapshot, bool> predicate,
        TimeSpan timeout)
    {
        try
        {
            return await client.WaitUntilAsync(predicate, timeout);
        }
        catch (TimeoutException)
        {
            return await client.GetStateAsync();
        }
    }

    private static async Task<WorldDomSnapshot> WaitTicksAsync(JxHeadlessClient client, uint deltaTicks)
    {
        WorldDomSnapshot start = await client.GetStateAsync();
        uint targetTick = start.TickCount + deltaTicks;
        return await client.WaitUntilAsync(
            s => s.TickCount >= targetTick,
            TimeSpan.FromSeconds(Math.Max(5, (deltaTicks / 12.0) + 4.0)));
    }

    private static uint GetMaxS2cSeq(WorldDomSnapshot snapshot) =>
        snapshot.RecentS2c.Count == 0 ? 0u : snapshot.RecentS2c.Max(static p => p.Seq);

    private static uint GetMaxChatSeq(WorldDomSnapshot snapshot) =>
        snapshot.ChatMessages.Count == 0 ? 0u : snapshot.ChatMessages.Max(static m => m.Seq);

    private static uint GetMaxSysMsgSeq(WorldDomSnapshot snapshot) =>
        snapshot.SystemMessages.Count == 0 ? 0u : snapshot.SystemMessages.Max(static m => m.Seq);

    private static double ComputeDistance(int x0, int y0, int x1, int y1)
    {
        double dx = x1 - x0;
        double dy = y1 - y0;
        return Math.Sqrt((dx * dx) + (dy * dy));
    }
}
