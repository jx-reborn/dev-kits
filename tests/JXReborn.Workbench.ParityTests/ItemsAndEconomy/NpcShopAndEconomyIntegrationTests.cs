using JXReborn.Contracts.Characters;
using JXReborn.GameAssets.Items;
using JXReborn.GameAssets.Shops;
using JXReborn.Workbench.ParityTests.Collections;
using JXReborn.Workbench.ParityTests.Fixtures;
using JXReborn.Primitives.Characters;
using JXReborn.Primitives.Items;
using JXReborn.Primitives.Shops;
using JXReborn.TestBase.SimulationHarnesses;
using JXReborn.ZoneEngine.Items;
using JXReborn.ZoneEngine.PlayerSessions;
using JXReborn.ZoneEngine.Shops;
using JXReborn.ZoneEngine.WorldZones;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace JXReborn.Integration.Tests.ItemsAndEconomy;

/// <summary>
/// Integration verification of NPC merchant shop purchase and sell-back against exported <c>npc-shops.shops.json</c>
/// (<c>server1/settings/buysell.txt</c> + <c>goods.txt</c>) and <c>KBuySell::Sell</c> (<c>0x081fbd50</c>: <c>price / 4</c>).
/// </summary>
[Collection(NonParallelParityCollection.Name)]
public sealed class NpcShopAndEconomyIntegrationTests
{
    private static CharacterDto CreateTestCharacter(Guid id, int inventoryMoney) =>
        new(
            Id: id,
            AccountId: Guid.NewGuid(),
            SlotIndex: 0,
            RoleNameUnicode: "ShopCustomer",
            Gender: CharacterGender.Male,
            Series: ElementSeries.Metal,
            Level: 20,
            Experience: 0,
            LeadExperience: 0,
            MapId: 53,
            RevivalMapId: 53,
            RevivalPointId: 19,
            MapMpsX: 50756,
            MapMpsY: 100944,
            CurrentLife: 1000,
            MaxLife: 1000,
            CurrentMana: 500,
            MaxMana: 500,
            CurrentStamina: 300,
            MaxStamina: 300,
            Strength: 100,
            Dexterity: 80,
            Vitality: 100,
            Energy: 50,
            Lucky: 0,
            UnallocatedAttributePoints: 0,
            UnallocatedSkillPoints: 0,
            InventoryMoney: inventoryMoney,
            RepositoryMoney: 0,
            CurrentFactionId: 255,
            FirstFactionId: 255,
            FactionJoinCount: 0,
            Skills: [new CharacterSkillDto(1, 1, 0)]);

    [Fact]
    [Trait("Category", "LegacyData")]
    public async Task NpcShopPurchaseAndSellBack_WhenBuyingShop1Slot2_ShouldDeduct2700AndCredit675Silver()
    {
        // Ground-truth provenance:
        // - server1/settings/buysell.txt row 1 -> Shop 1 slot 2 = GoodsId 28 (Thanh Phong Kiếm, 0,0,0, lv=3, price=2700)
        // - jx_linux_y KBuySell::Sell @ 0x081fbe75 -> sellCredit = 2700 / 4 = 675
        await using var serverHarness = await WorkbenchIntegrationServerHarness.StartAsync();
        var shopCatalog = serverHarness.Services.GetRequiredService<IShopCatalog>();
        var itemCatalog = serverHarness.Services.GetRequiredService<IItemTemplateCatalog>();

        var zoneHarness = new ZoneSimulationHarness(new ZoneSimulationHarnessOptions
        {
            Assets = new ZoneAssetServices
            {
                ItemTemplateCatalog = itemCatalog,
                ShopCatalog = shopCatalog
            }
        });

        Guid charId = Guid.NewGuid();
        CharacterDto charDto = CreateTestCharacter(charId, 100_000);
        SimulatedPlayerHandle handle = await zoneHarness.AttachSimulatedPlayerAsync(charDto);
        PlayerZoneState player = handle.GetZoneState()!;
        player.InventoryMoney = 100_000;

        MapZoneWorker worker = zoneHarness.GetZone(charDto.MapId);
        var context = new ZoneExecutionContext(worker);
        var handler = new NpcShopCommandHandler(shopCatalog);

        // 1. Open Shop 1 at 100% price scale
        await handler.HandleAsync(new OpenNpcShopZoneCommand(charId, 1L, ShopId: 1, ShopCurrencyType.Gold, 100), context);

        // 2. Buy slot 2 (Thanh Phong Kiếm, 2,700 silver) into EquipRoom (4, 5)
        await handler.HandleAsync(new BuyShopGoodZoneCommand(charId, 1L, BuyIndex: 2, ItemContainerPlace.EquipRoom, TargetX: 4, TargetY: 5), context);

        Assert.Equal(97_300, player.InventoryMoney);
        PlayerItemSlot purchased = Assert.Single(player.Inventory.GetBagItems());
        Assert.Equal(ItemGenreKind.Equipment, purchased.Genre);
        Assert.Equal(0, purchased.DetailType);
        Assert.Equal(0, purchased.ParticularType);
        Assert.Equal(3, purchased.Level);
        Assert.Equal(4, purchased.GridX);
        Assert.Equal(5, purchased.GridY);

        // 3. Sell item back -> 2,700 / 4 = 675 silver credited (97,300 + 675 = 97,975)
        await handler.HandleAsync(new SellItemBackZoneCommand(charId, 1L, purchased.RuntimeItemId), context);

        Assert.Equal(97_975, player.InventoryMoney);
        Assert.Empty(player.Inventory.GetBagItems());
    }

    [Fact]
    public async Task NpcShopSellBack_WhenShopSessionIsClosed_ShouldRejectWithoutRemovingItemOrCreditingMoney()
    {
        // Ground-truth provenance:
        // - jx_linux_y KBuySell::Sell @ 0x081fbd50 / KPlayer::SellItem @ 0x080c5f33 passes Player.m_BuyInfo.m_nBuyIdx
        //   (-1 when no shop is open) and rejects selling when shop session is not open.
        await using var serverHarness = await WorkbenchIntegrationServerHarness.StartAsync();
        var shopCatalog = serverHarness.Services.GetRequiredService<IShopCatalog>();
        var itemCatalog = serverHarness.Services.GetRequiredService<IItemTemplateCatalog>();

        var zoneHarness = new ZoneSimulationHarness(new ZoneSimulationHarnessOptions
        {
            Assets = new ZoneAssetServices
            {
                ItemTemplateCatalog = itemCatalog,
                ShopCatalog = shopCatalog
            }
        });

        Guid charId = Guid.NewGuid();
        SimulatedPlayerHandle handle = await zoneHarness.AttachSimulatedPlayerAsync(CreateTestCharacter(charId, 10_000));
        PlayerZoneState player = handle.GetZoneState()!;
        player.InventoryMoney = 10_000;

        Assert.True(itemCatalog.TryGetItem(ItemGenreKind.Equipment, 0, 0, 3, out ItemTemplate? swordTpl) && swordTpl is not null);
        var bagItem = new PlayerItemSlot(
            runtimeItemId: player.Inventory.AllocateRuntimeItemId(),
            domainItemId: Guid.NewGuid(),
            template: swordTpl!,
            genre: ItemGenreKind.Equipment,
            detailType: 0,
            particularType: 0,
            level: 3,
            series: ElementSeries.Metal,
            quality: ItemQualityKind.Normal,
            randomSeed: 1u,
            generatorVersion: ItemConsts.GeneratorVersion,
            luck: 0,
            currentDurability: 40,
            maxDurability: 40,
            stackCount: 1,
            containerPlace: ItemContainerPlace.EquipRoom,
            gridX: 0,
            gridY: 0);
        Assert.True(player.Inventory.TryPlaceItemDirect(bagItem, ItemContainerPlace.EquipRoom, 0, 0, player));

        var context = new ZoneExecutionContext(zoneHarness.GetZone(53));
        var handler = new NpcShopCommandHandler(shopCatalog);

        // Player has NOT opened any shop -> selling must be rejected!
        await handler.HandleAsync(new SellItemBackZoneCommand(charId, 1L, bagItem.RuntimeItemId), context);

        Assert.Equal(10_000, player.InventoryMoney);
        Assert.Single(player.Inventory.GetBagItems());
    }
}
