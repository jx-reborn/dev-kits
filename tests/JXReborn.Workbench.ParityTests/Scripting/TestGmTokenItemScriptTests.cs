using System;
using System.Linq;
using System.Threading.Tasks;
using JXReborn.GameAssets.Items;
using JXReborn.Primitives.Characters;
using JXReborn.Primitives.Items;
using JXReborn.Primitives.WorldZones;
using JXReborn.Scripts.Dialogs;
using JXReborn.Scripts.Items;
using JXReborn.Workbench.Scripts.Items;
using JXReborn.TestBase.SimulationHarnesses;
using JXReborn.ZoneEngine.Items;
using JXReborn.ZoneEngine.PlayerSessions;
using JXReborn.ZoneEngine.Scripting;
using JXReborn.ZoneEngine.WorldZones;
using Xunit;

namespace JXReborn.ZoneEngine.Tests.Scripting;

public sealed class TestGmTokenItemScriptTests
{
    private static (ZoneSimulationHarness Harness, ZoneExecutionContext Context) CreateHarness(bool enableGmToken)
    {
        var harness = new ZoneSimulationHarness(new ZoneSimulationHarnessOptions());
        MapZoneWorker worker = harness.Workers.First();
        worker.ExecutionContext.Options.EnableTestGmToken = enableGmToken;
        worker.ExecutionContext.Options.AutoGrantGmTokenOnLogin = enableGmToken;
        worker.ExecutionContext.Options.TestGmTokenItemCode = TestGmTokenItemScript.GmTokenItemCode;
        worker.ExecutionContext.Options.TestGmTokenScriptCode = TestGmTokenItemScript.GmTokenScriptPath;
        return (harness, worker.ExecutionContext);
    }

    private static PlayerZoneState CreatePlayer(byte level = 1, int money = 0) =>
        new(
            runtimeNpcId: 1001u,
            accountId: Guid.NewGuid(),
            characterId: Guid.NewGuid(),
            leaseEpoch: 1L,
            roleNameUnicode: "GmTokenTester",
            gender: CharacterGender.Male,
            series: ElementSeries.Earth,
            level: level,
            experience: 0,
            leadExperience: 0,
            mapId: 53,
            currentMpsX: 51200,
            currentMpsY: 102400,
            currentLife: 90,
            maxLife: 100,
            currentMana: 20,
            maxMana: 40,
            currentStamina: 80,
            maxStamina: 100,
            strength: 20,
            dexterity: 15,
            vitality: 25,
            energy: 20,
            lucky: 0,
            unallocatedAttributePoints: 0,
            unallocatedSkillPoints: 0,
            inventoryMoney: money,
            repositoryMoney: 0,
            currentFactionId: 255,
            firstFactionId: 255,
            factionJoinCount: 0,
            initialTick: 1L,
            outboundSink: new PlayerOutboundEventRecorder());

    private static (ZoneScriptItemContext ItemCtx, PlayerItemSlot TokenSlot) CreateGmTokenContext(
        PlayerZoneState player,
        ZoneExecutionContext execCtx,
        bool addToBag = false)
    {
        var tpl = new ItemTemplate(
            Id: 4779,
            Code: TestGmTokenItemScript.GmTokenItemCode,
            Name: "Cẩm nang Tân Thủ",
            Genre: ItemGenreKind.MagicScript,
            Quality: ItemQualityKind.Normal,
            DetailType: 1,
            ParticularType: TestGmTokenItemScript.GmTokenParticularType,
            Level: 1,
            ElementSeries: ElementSeries.Metal,
            GridWidthCells: 1,
            GridHeightCells: 1,
            ObjDataId: 38,
            AppearanceResId: 0,
            PriceGold: 100,
            MaxStackCount: 1,
            BaseAttributes: [],
            Requirements: []);

        var slot = new PlayerItemSlot(
            runtimeItemId: player.Inventory.AllocateRuntimeItemId(),
            domainItemId: Guid.NewGuid(),
            template: tpl,
            genre: ItemGenreKind.MagicScript,
            detailType: 1,
            particularType: TestGmTokenItemScript.GmTokenParticularType,
            level: 1,
            series: ElementSeries.Metal,
            quality: ItemQualityKind.Normal,
            randomSeed: 0,
            generatorVersion: 1,
            luck: 0,
            currentDurability: 0,
            maxDurability: 0,
            stackCount: 1,
            containerPlace: ItemContainerPlace.EquipRoom,
            gridX: 0,
            gridY: 0,
            bindState: ItemBindState.BoundPermanent);

        if (addToBag)
        {
            _ = player.Inventory.TryAddOrAutoPlaceItem(slot, ItemContainerPlace.EquipRoom, 0, 0, player);
        }

        return (new ZoneScriptItemContext(player, slot, targetNpcId: 0, execCtx), slot);
    }

    private static async Task ExecutePathAsync(
        TestGmTokenItemScript script,
        ZoneScriptItemContext itemCtx,
        ZoneScriptDialogContext dialogCtx,
        PlayerZoneState player,
        params int[] optionPath)
    {
        _ = await script.OnUseItemAsync(itemCtx);
        Assert.True(player.TryGetFeatureSlot(PlayerDialogFeatureSlot.SlotKey, out PlayerDialogFeatureSlot? dialogSlot));
        foreach (int idx in optionPath)
        {
            Assert.True(dialogSlot!.TrySnapshotAndClearOptionCallback(idx, out var cb), $"Option index {idx} not found.");
            await cb!(dialogCtx);
        }
    }

    [Fact]
    public async Task OnUseItemAsync_WhenEnableTestGmTokenIsFalse_ShouldBeNoOpAndOpenNoDialog()
    {
        var (_, execCtx) = CreateHarness(enableGmToken: false);
        PlayerZoneState player = CreatePlayer();
        var (itemCtx, _) = CreateGmTokenContext(player, execCtx);
        var script = new TestGmTokenItemScript();

        ItemUsageResult res = await script.OnUseItemAsync(itemCtx);

        Assert.Equal(ItemUsageResult.KeepItem, res);
        Assert.False(player.TryGetFeatureSlot(PlayerDialogFeatureSlot.SlotKey, out _));
    }

    [Fact]
    public async Task OnUseItemAsync_CharacterEconomyAndDurabilityAtomics_ShouldMutateSingleAxesOnly()
    {
        var (_, execCtx) = CreateHarness(enableGmToken: true);
        PlayerZoneState player = CreatePlayer(level: 1, money: 0);

        var weaponTpl = new ItemTemplate(
            Id: 1,
            Code: "starter-sword",
            Name: "Thiết Kiếm",
            Genre: ItemGenreKind.Equipment,
            Quality: ItemQualityKind.Normal,
            DetailType: 0,
            ParticularType: 0,
            Level: 1,
            ElementSeries: ElementSeries.Metal,
            GridWidthCells: 1,
            GridHeightCells: 3,
            ObjDataId: 1,
            AppearanceResId: 1,
            PriceGold: 100,
            MaxStackCount: 1,
            BaseAttributes: [],
            Requirements: []);
        var weaponSlot = new PlayerItemSlot(
            runtimeItemId: player.Inventory.AllocateRuntimeItemId(),
            domainItemId: Guid.NewGuid(),
            template: weaponTpl,
            genre: ItemGenreKind.Equipment,
            detailType: 0,
            particularType: 0,
            level: 1,
            series: ElementSeries.Metal,
            quality: ItemQualityKind.Normal,
            randomSeed: 0,
            generatorVersion: 1,
            luck: 0,
            currentDurability: 20,
            maxDurability: 20,
            stackCount: 1,
            containerPlace: ItemContainerPlace.EquipRoom,
            gridX: 0,
            gridY: 0);
        _ = player.Inventory.TryAddOrAutoPlaceItem(weaponSlot, ItemContainerPlace.EquipRoom, 0, 0, player);

        var (itemCtx, _) = CreateGmTokenContext(player, execCtx);
        var dialogCtx = new ZoneScriptDialogContext(player, sourceNpc: null, execCtx);
        var script = new TestGmTokenItemScript();

        // [0] Character -> [0] Level -> [1] Level 10
        await ExecutePathAsync(script, itemCtx, dialogCtx, player, 0, 0, 1);
        Assert.Equal(10, player.Level);
        Assert.Equal(45, player.UnallocatedAttributePoints);
        Assert.Equal(9, player.UnallocatedSkillPoints);
        Assert.Equal(0, player.InventoryMoney); // No side-effect on economy

        // [2] Economy -> [0] +50,000 silver
        await ExecutePathAsync(script, itemCtx, dialogCtx, player, 2, 0);
        Assert.Equal(50_000, player.InventoryMoney);
        Assert.Equal(20, weaponSlot.CurrentDurability); // No side-effect on durability

        // [3] Items -> [3] Durability -> [0] Degrade -5
        await ExecutePathAsync(script, itemCtx, dialogCtx, player, 3, 3, 0);
        Assert.Equal(15, weaponSlot.CurrentDurability);

        // [3] Items -> [3] Durability -> [1] Set durability = 1
        await ExecutePathAsync(script, itemCtx, dialogCtx, player, 3, 3, 1);
        Assert.Equal(1, weaponSlot.CurrentDurability);
    }

    [Fact]
    public async Task OnUseItemAsync_FactionSkillMaterialsAndCombatAtomics_ShouldComposeEndgameAndCraftingStates()
    {
        var (_, execCtx) = CreateHarness(enableGmToken: true);
        PlayerZoneState player = CreatePlayer(level: 1, money: 0);
        var (itemCtx, _) = CreateGmTokenContext(player, execCtx);
        var dialogCtx = new ZoneScriptDialogContext(player, sourceNpc: null, execCtx);
        var script = new TestGmTokenItemScript();

        // Level 80: [0, 0, 5]
        await ExecutePathAsync(script, itemCtx, dialogCtx, player, 0, 0, 5);
        // Camp Free: [1, 0, 4]
        await ExecutePathAsync(script, itemCtx, dialogCtx, player, 1, 0, 4);
        // +500 Repute: [1, 3, 0]; +3M LeadExp: [1, 3, 1]; Transmigration=1: [1, 4, 0]
        await ExecutePathAsync(script, itemCtx, dialogCtx, player, 1, 3, 0);
        await ExecutePathAsync(script, itemCtx, dialogCtx, player, 1, 3, 1);
        await ExecutePathAsync(script, itemCtx, dialogCtx, player, 1, 4, 0);
        // Learn Skill 14 Lv5: [1, 2, 0]
        await ExecutePathAsync(script, itemCtx, dialogCtx, player, 1, 2, 0);
        // Materials: 3x Huyen Tinh [3, 2, 0], Raw Ore [3, 2, 1], Attr Ore [3, 2, 2], Nhac Vuong Kiem [3, 2, 3], Sat Thu Gian [3, 2, 4]
        await ExecutePathAsync(script, itemCtx, dialogCtx, player, 3, 2, 0);
        await ExecutePathAsync(script, itemCtx, dialogCtx, player, 3, 2, 1);
        await ExecutePathAsync(script, itemCtx, dialogCtx, player, 3, 2, 2);
        await ExecutePathAsync(script, itemCtx, dialogCtx, player, 3, 2, 3);
        await ExecutePathAsync(script, itemCtx, dialogCtx, player, 3, 2, 4);
        // Equipment: Blue Ring [3, 1, 3], Purple Sword [3, 1, 4]
        await ExecutePathAsync(script, itemCtx, dialogCtx, player, 3, 1, 3);
        await ExecutePathAsync(script, itemCtx, dialogCtx, player, 3, 1, 4);
        // Combat: FightMode=1 [4, 0], PkValue=10 [4, 3]
        await ExecutePathAsync(script, itemCtx, dialogCtx, player, 4, 0);
        await ExecutePathAsync(script, itemCtx, dialogCtx, player, 4, 3);

        Assert.Equal(80, player.Level);
        Assert.Equal(EntityCamp.Free, player.BaseCamp);
        Assert.Equal(EntityCamp.Free, player.CurrentCamp);
        Assert.Equal(500, player.Repute);
        Assert.Equal(3_000_000, player.LeadExperience);
        Assert.Equal(1, player.GetTaskValue(99));
        Assert.True(player.TryGetLearnedSkill(14, out var learnedSkill));
        Assert.Equal((ushort)5, learnedSkill.BaseLevel);
        Assert.Equal((byte)1, player.FightMode);
        Assert.Equal((sbyte)10, player.PkValue);
        Assert.Equal(3, player.Inventory.GetBagItems().Count(static i => i.Genre == ItemGenreKind.MagicScript && i.ParticularType == 147 && i.Level == 3));
        Assert.Contains(player.Inventory.GetBagItems(), static i => i.Genre == ItemGenreKind.Equipment && i.DetailType == 3 && i.HasAnyMagicAffix);
        Assert.Contains(player.Inventory.GetBagItems(), static i => i.Genre == ItemGenreKind.MagicScript && i.ParticularType == 149);
        Assert.Contains(player.Inventory.GetBagItems(), static i => i.Genre == ItemGenreKind.Equipment && i.Quality == ItemQualityKind.Purple);
        Assert.Contains(player.Inventory.GetBagItems(), static i => i.Genre == ItemGenreKind.MagicScript && i.ParticularType == 200 && i.MagicAttributes[0].AttributeId == 126);
        Assert.Contains(player.Inventory.GetBagItems(), static i => i.Genre == ItemGenreKind.QuestKey && i.DetailType == 195);
        Assert.Contains(player.Inventory.GetBagItems(), static i => i.Genre == ItemGenreKind.MagicScript && i.ParticularType == 400 && i.Level == 90);
    }

    [Fact]
    public async Task OnUseItemAsync_BagBoundaryFillAndClear_ShouldReachZeroFreeCellsAndKeepGmTokenOnClear()
    {
        var (_, execCtx) = CreateHarness(enableGmToken: true);
        PlayerZoneState player = CreatePlayer(level: 10, money: 0);
        var (itemCtx, tokenSlot) = CreateGmTokenContext(player, execCtx, addToBag: true);
        var dialogCtx = new ZoneScriptDialogContext(player, sourceNpc: null, execCtx);
        var script = new TestGmTokenItemScript();

        // [3] Items -> [4] Bag Boundary -> [1] Fill Bag to 0 free cells
        await ExecutePathAsync(script, itemCtx, dialogCtx, player, 3, 4, 1);
        Assert.Equal(0, dialogCtx.FreeBagCellCount);

        // [3] Items -> [4] Bag Boundary -> [0] Clear Bag keeping GM Token
        await ExecutePathAsync(script, itemCtx, dialogCtx, player, 3, 4, 0);
        PlayerItemSlot[] remaining = player.Inventory.GetBagItems().ToArray();
        Assert.Single(remaining);
        Assert.Equal(tokenSlot.RuntimeItemId, remaining[0].RuntimeItemId);
    }
}
