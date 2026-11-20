using JXReborn.Contracts.Characters;
using JXReborn.Domain.Characters;
using JXReborn.GameAssets.WorldMaps;
using JXReborn.Workbench.ParityTests.Collections;
using JXReborn.Workbench.ParityTests.Fixtures;
using JXReborn.Primitives.Characters;
using JXReborn.Primitives.ErrorCodes;
using JXReborn.Primitives.Items;
using JXReborn.TestBase.SimulationHarnesses;
using JXReborn.ZoneEngine.WorldZones;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace JXReborn.Integration.Tests.ItemsAndEconomy;

[Collection(NonParallelParityCollection.Name)]
public sealed class RepositoryBoxAndRevivalIntegrationTests
{
    private static Character CreateTestCharacter(Guid id, Guid accountId, string name, int invMoney, int repMoney)
    {
        var ch = new Character(
            id: id,
            accountId: accountId,
            slotIndex: 0,
            roleName: name,
            gender: CharacterGender.Male,
            series: ElementSeries.Metal,
            mapId: 53,
            revivalMapId: 53,
            revivalPointId: 19,
            mapMpsX: 52000,
            mapMpsY: 101696,
            strength: 50,
            dexterity: 50,
            vitality: 50,
            energy: 50,
            lucky: 0,
            maxLife: 300,
            maxMana: 200,
            maxStamina: 150);

        if (repMoney > 0)
        {
            ch.AdjustInventoryMoney(repMoney);
            ch.DepositMoney(repMoney);
        }

        if (invMoney > 0)
        {
            ch.AdjustInventoryMoney(invMoney);
        }

        return ch;
    }

    [Fact]
    public async Task CharacterDepositAndWithdrawMoney_ShouldAccuratelyUpdateBalancesInPersistence()
    {
        await using var harness = await WorkbenchIntegrationServerHarness.StartAsync();
        var charRepo = harness.Services.GetRequiredService<ICharacterRepository>();

        Guid charId = Guid.NewGuid();
        Guid accId = Guid.NewGuid();
        Character ch = CreateTestCharacter(charId, accId, "RepoPersist", 100000, 20000);
        await charRepo.InsertAsync(ch);

        // Deposit 30,000
        ch.DepositMoney(30000);
        await charRepo.UpdateAsync(ch);

        Character? updated = await charRepo.FindAsync(charId);
        Assert.NotNull(updated);
        Assert.Equal(70000, updated.InventoryMoney);
        Assert.Equal(50000, updated.RepositoryMoney);

        // Withdraw 15,000
        updated.WithdrawMoney(15000);
        await charRepo.UpdateAsync(updated);

        Character? reloaded = await charRepo.FindAsync(charId);
        Assert.NotNull(reloaded);
        Assert.Equal(85000, reloaded.InventoryMoney);
        Assert.Equal(35000, reloaded.RepositoryMoney);
    }

    [Fact]
    public async Task CharacterSetRevivalPoint_ShouldPersistNewMapAndRevivalCoordinates()
    {
        await using var harness = await WorkbenchIntegrationServerHarness.StartAsync();
        var charRepo = harness.Services.GetRequiredService<ICharacterRepository>();

        Guid charId = Guid.NewGuid();
        Guid accId = Guid.NewGuid();
        Character ch = CreateTestCharacter(charId, accId, "RevivalPersist", 10000, 0);
        await charRepo.InsertAsync(ch);

        // Bind to Biện Kinh (Map 1, Revival Point 5)
        ch.SetRevivalPoint(1, 5);
        await charRepo.UpdateAsync(ch);

        Character? updated = await charRepo.FindAsync(charId);
        Assert.NotNull(updated);
        Assert.Equal(1, updated.RevivalMapId);
        Assert.Equal(5, updated.RevivalPointId);
    }

    [Fact]
    public async Task CharacterDepositMoney_WhenExceedingMaxDepositLimit_ShouldBeRejectedAndRetainPersistedBalance()
    {
        await using var harness = await WorkbenchIntegrationServerHarness.StartAsync();
        var charRepo = harness.Services.GetRequiredService<ICharacterRepository>();

        Guid charId = Guid.NewGuid();
        Guid accId = Guid.NewGuid();
        Character ch = CreateTestCharacter(charId, accId, "RepoOverflowPersist", 1_500_000_000, 1_500_000_000);
        await charRepo.InsertAsync(ch);

        // Attempt to deposit 1,000,000,000 (total would be 2.5B, exceeding 2B limit)
        var ex = Assert.Throws<BusinessException>(() => ch.DepositMoney(1_000_000_000));
        Assert.Equal(JXRebornErrorCodes.CharacterRepositoryLimitExceeded, ex.Code);

        // Verify persisted balances remain strictly untouched
        Character? updated = await charRepo.FindAsync(charId);
        Assert.NotNull(updated);
        Assert.Equal(1_500_000_000, updated.InventoryMoney);
        Assert.Equal(1_500_000_000, updated.RepositoryMoney);
    }

    [Fact]
    [Trait("Category", "LegacyData")]
    public async Task BaLangHuyenMapZone_WhenInitializedFromExportedMapAssets_ShouldSpawnStaticStorageBoxAt51593And101357()
    {
        // Ground-truth provenance:
        // - data/maps/053_ba-lang-huyen.map.json -> mapObjects: objTemplateId=1 ('bao-ruong-1') at (51593, 101357),
        //   scriptPath='\script\两湖区\巴陵县\obj\巴陵县-储物箱1.lua'
        // - Live jx-oracle-test (J10Repo): spawns Kind=3 storage box at (51593, 101357) and handles c2s_objmouseclick (0x72) -> s2c_openstorebox (0x88: 88 00 00).
        await using var harness = await WorkbenchIntegrationServerHarness.StartAsync();
        var mapCatalog = harness.Services.GetRequiredService<IMapCatalog>();
        Assert.True(mapCatalog.TryGetMapDescriptor(53, out MapDescriptor? map53) && map53 is not null);

        var zoneHarness = new ZoneSimulationHarness(new ZoneSimulationHarnessOptions
        {
            InitialMaps = { map53! },
            Assets = new ZoneAssetServices
            {
                MapCatalog = mapCatalog
            }
        });

        MapZoneWorker worker = zoneHarness.GetZone(53);
        JXReborn.ZoneEngine.Items.GroundObjectSlot boxObj = Assert.Single(
            worker.GroundObjectSystem.ObjectsById.Values,
            static obj => obj.ObjDataId == 1 && obj.Kind == GroundObjectKind.Prop && obj.MpsX == 51593 && obj.MpsY == 101357);

        // Clicking bao-ruong-1 within <= 200 Mps opens the storage box (88 00 00) and binds revival point 19
        var recorder = new PlayerOutboundEventRecorder();
        var charDto = new CharacterDto(
            Id: Guid.NewGuid(),
            AccountId: Guid.NewGuid(),
            SlotIndex: 0,
            RoleNameUnicode: "BoxClicker",
            Gender: CharacterGender.Male,
            Series: ElementSeries.Metal,
            Level: 10,
            Experience: 0,
            LeadExperience: 0,
            MapId: 53,
            RevivalMapId: 53,
            RevivalPointId: 1,
            MapMpsX: 51632,
            MapMpsY: 101392,
            CurrentLife: 500,
            MaxLife: 500,
            CurrentMana: 200,
            MaxMana: 200,
            CurrentStamina: 200,
            MaxStamina: 200,
            Strength: 50,
            Dexterity: 50,
            Vitality: 50,
            Energy: 50,
            Lucky: 0,
            UnallocatedAttributePoints: 0,
            UnallocatedSkillPoints: 0,
            InventoryMoney: 10_000,
            RepositoryMoney: 5_000,
            CurrentFactionId: 255,
            FirstFactionId: 255,
            FactionJoinCount: 0,
            Skills: [new CharacterSkillDto(1, 1, 0)]);

        SimulatedPlayerHandle handle = await zoneHarness.AttachSimulatedPlayerAsync(charDto, customSink: recorder);
        int openCountBefore = recorder.SentOpenStoreBoxCount;

        Assert.True(zoneHarness.TryDispatchToZone(
            53,
            new JXReborn.ZoneEngine.Items.RequestGroundObjectSyncZoneCommand(charDto.Id, 1L, boxObj.ObjId, IsMouseClick: true)));
        await zoneHarness.StepFramesAsync(1);

        Assert.Equal(openCountBefore + 1, recorder.SentOpenStoreBoxCount);
        Assert.Equal(19, handle.GetZoneState()!.RevivalPointId);
    }
}
