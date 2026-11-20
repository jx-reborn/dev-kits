# Internal Engine Contracts: Equipment Abrasion, Repair Pricing, Golden Generation & Set Evaluation

**Feature Branch**: `013-core-equipment-durability-and-sets`  
**Date**: 2026-11-11  
**Compatibility Sources**: `abraderate.ini`, `repairitem.lua`, legacy 6.0 repair pricing & set aggregation rules, `golden-equipment.items.json`, `sets-objects-recipes.items.json`

---

## 1. Equipment Abrasion Contract (`EquipmentAbrasionCalculator`)

**File**: `src/JXReborn.ZoneEngine/Items/EquipmentAbrasionCalculator.cs`

```csharp
namespace JXReborn.ZoneEngine.Items;

public static class EquipmentAbrasionCalculator
{
    /// <summary>
    /// Returns the legacy 6.0 abrasion random range denominator (<c>1 / nRandRange</c>), or <c>0</c> if the slot never abrades for <paramref name="kind"/>.
    /// </summary>
    public static int GetAbradeRandRange(EquipmentAbradeKind kind, EquipmentPartKind part) =>
        (kind, part) switch
        {
            (EquipmentAbradeKind.Attack, EquipmentPartKind.Weapon) => EquipmentAbrasionConsts.WeaponAttackRange, // 256
            (EquipmentAbradeKind.Defend, EquipmentPartKind.Head)   => EquipmentAbrasionConsts.ArmorDefendRange,  // 320
            (EquipmentAbradeKind.Defend, EquipmentPartKind.Body)   => EquipmentAbrasionConsts.ArmorDefendRange,  // 320
            (EquipmentAbradeKind.Defend, EquipmentPartKind.Belt)   => EquipmentAbrasionConsts.ArmorDefendRange,  // 320
            (EquipmentAbradeKind.Defend, EquipmentPartKind.Cuff)   => EquipmentAbrasionConsts.ArmorDefendRange,  // 320
            (EquipmentAbradeKind.Move,   EquipmentPartKind.Foot)   => EquipmentAbrasionConsts.BootsMoveRange,    // 2560
            (EquipmentAbradeKind.Move,   EquipmentPartKind.Horse)  => EquipmentAbrasionConsts.HorseMoveRange,    // 5120
            _ => 0,
        };

    /// <summary>
    /// Evaluates abrasion for a single equipped item. Returns <c>true</c> if durability decreased by <c>1</c>
    /// (<paramref name="becameBroken"/> is <c>true</c> when transitioning <c>1 -&gt; 0</c>).
    /// Never mutates indestructible or already-broken (<c>CurrentDurability == 0</c>) items.
    /// </summary>
    public static bool TryAbradeSlot(
        PlayerItemSlot item,
        EquipmentAbradeKind kind,
        EquipmentPartKind part,
        Random random,
        out bool becameBroken);
}
```

### Simulation Hook Invariants (`MapZoneWorker`)
1. **`Attack` Hook**: Executed once per skill cast inside combat skill execution **only when `player.FightMode != 0`**.
2. **`Defend` Hook**: Executed when incoming missile/melee/DoT damage inflicts **net HP loss (`hpAfter < hpBefore`)** on a living player. Iterates `Head (0)`, `Body (1)`, `Belt (2)`, `Cuff (5)` independently (`Foot (4)` is `0`!).
3. **`Move` Hook**: Executed when a player's spatial region changes (`oldRegionId != newRegionId`). Iterates `Foot (4)` and `Horse (10)` (`Mask (11)` is `0`!).
4. **Side Effects on `TryAbradeSlot == true`**:
   - Emit `player.OutboundSink.SendItemDurabilitySync(item.RuntimeItemId, item.MaxDurability, item.CurrentDurability)`.
   - If `becameBroken == true`: call `player.Inventory.RecalculateEquipmentAndFiveElementsActivation(player)` (which suspends the broken item's stats/links/set counts and clamps `CurrentLife`, `CurrentMana`, `CurrentStamina`) and sync updated player vitals/attributes.

---

## 2. Equipment Repair Pricing & Zone Command Contract

### 2.1 `EquipmentRepairCalculator` (`src/JXReborn.ZoneEngine/Items/EquipmentRepairCalculator.cs`)

```csharp
namespace JXReborn.ZoneEngine.Items;

public static class EquipmentRepairCalculator
{
    public static (bool CanRepair, int PriceGold, ushort NewCurrentDurability, ushort NewMaxDurability) ComputeRepairQuote(
        PlayerItemSlot item,
        bool isCheapGoldenRepair = false);
}
```

**Exact Arithmetic Rules**:
- **Guard Clauses**: Return `(false, 0, item.CurrentDurability, item.MaxDurability)` when:
  - `item.IsIndestructible` is `true`, OR
  - `item.Genre != ItemGenreKind.Equipment && item.Genre != ItemGenreKind.PlatinaEquipment`, OR
  - `item.MaxDurability == 0` or `item.CurrentDurability >= item.MaxDurability`.
- **Case 1 — Golden Equipment (`item.Quality == ItemQualityKind.Golden`)** (`repairitem.lua:42-57`):
  - `int fixDur = item.MaxDurability - item.CurrentDurability;`
  - `long lvl = Math.Max(1, (int)item.Level);`
  - `long cheapPrice = EquipmentAbrasionConsts.GoldenCheapRepairLevelMultiplier * lvl * lvl * fixDur;` (`10 * Level^2 * FixDur`)
  - If `isCheapGoldenRepair`:
    - `int lostDur = Math.Max(1, fixDur / EquipmentAbrasionConsts.GoldenCheapRepairLostDurabilityDivisor);` (`max(1, fixDur / 10)`)
    - `ushort newMax = (ushort)Math.Max(1, item.MaxDurability - lostDur);`
    - Return `(true, (int)Math.Clamp(cheapPrice, 0, int.MaxValue), newMax, newMax)`.
  - Else (**Costly Repair**):
    - `long costlyPrice = cheapPrice * EquipmentAbrasionConsts.GoldenCostlyRepairMultiplier;` (`30 * Level^2 * FixDur`)
    - Return `(true, (int)Math.Clamp(costlyPrice, 0, int.MaxValue), item.MaxDurability, item.MaxDurability)`.
- **Case 2 — Normal / Blue Equipment (`item.Quality == ItemQualityKind.Normal`)** (`repairitem.lua:180`):
  - `int missingDur = item.MaxDurability - item.CurrentDurability;`
  - `int sumMagic = 0;` summing `Math.Max(0, (int)slot.EnchaseLevel)` across populated `item.MagicAttributes[0..5]`.
  - Using `long` intermediate division matching C++/Lua integer truncation step-by-step:
    ```csharp
    long step1 = ((long)Math.Max(0, item.Template.PriceGold) * EquipmentAbrasionConsts.NormalRepairItemPriceScale) / 100L;
    long step2 = (step1 * missingDur) / item.MaxDurability;
    long price = (step2 * (EquipmentAbrasionConsts.NormalRepairMagicPriceScale + sumMagic)) / EquipmentAbrasionConsts.NormalRepairMagicPriceScale;
    ```
  - Return `(true, (int)Math.Clamp(price, 0, int.MaxValue), item.MaxDurability, item.MaxDurability)`.

### 2.2 `RepairEquipmentItemZoneCommand` (`src/JXReborn.ZoneEngine/Items/RepairEquipmentItemZoneCommand.cs`)

```csharp
namespace JXReborn.ZoneEngine.Items;

public sealed record RepairEquipmentItemZoneCommand(
    Guid CharacterId,
    long LeaseEpoch,
    uint RuntimeItemId,
    bool IsCheapGoldenRepair = false) : IZoneCommand;
```

**Execution Contract (`MapZoneWorker.HandleRepairEquipmentItem`)**:
1. Validate active `PlayerZoneState` and `LeaseEpoch`.
2. Lookup `RuntimeItemId` in `player.Inventory.TryGetItemByRuntimeId`.
3. Compute quote via `EquipmentRepairCalculator.ComputeRepairQuote(item, command.IsCheapGoldenRepair)`. Abort if `!CanRepair` or `player.InventoryMoney < PriceGold`.
4. Deduct `player.InventoryMoney -= PriceGold` and call `item.Repair(NewCurrentDurability, NewMaxDurability)` (setting `IsBroken = false`).
5. Emit `SendMoneySync(player.InventoryMoney, player.RepositoryMoney)` and `SendItemDurabilitySync(item.RuntimeItemId, item.MaxDurability, item.CurrentDurability)`.
6. If `item.ContainerPlace == ItemContainerPlace.Equip`: invoke `player.Inventory.RecalculateEquipmentAndFiveElementsActivation(player)` and synchronize updated character attributes/vitals to the client.

---

## 3. Catalog, Golden Generation & Set Evaluation Contracts

### 3.1 `IItemTemplateCatalog` Extensions (`src/JXReborn.GameAssets/Items/IItemTemplateCatalog.cs`)

```csharp
IReadOnlyCollection<EquipmentSetTemplate> AllEquipmentSets { get; }
bool TryGetEquipmentSet(bool isExtendedSuite, int setId, out EquipmentSetTemplate? equipmentSet);
int GetClassicSuiteRequiredPieces(int suiteId);
```

- During `ItemTemplateCatalog` initialization (`ReBuildGoldEquipmentSuite` rule):
  - Load explicit `EquipmentSetTemplate` entries from `bundle.EquipmentSets` (`sets-objects-recipes.items.json`) into `_equipmentSetsByKindAndId[(set.IsExtendedSuite, set.Id)]`.
  - Group all `ItemTemplate` entries where `Quality == ItemQualityKind.Golden && SuiteBinding is { SuiteId: > 0 }` by `SuiteId`, counting distinct `RowId`s. For any `suiteId` (notably `1..82`) absent from `_equipmentSetsByKindAndId[(false, suiteId)]`, register the derived piece count so `GetClassicSuiteRequiredPieces(suiteId)` returns the exact Classic Golden set piece requirement!

### 3.2 `ItemGenerator.GenerateGoldEquipment` (`src/JXReborn.GameAssets/Items/ItemGenerator.cs`)

```csharp
public GeneratedItemData GenerateGoldEquipment(
    int goldRowId,
    uint? randomSeedOverride = null);
```

- Resolves `_itemCatalog.TryGetItemByRowId(ItemQualityKind.Golden, goldRowId, out ItemTemplate? tpl)`.
- Also updates `GenerateFromItemCode`: when `tpl.Quality == ItemQualityKind.Golden && tpl.RowId > 0`, delegates directly to `GenerateGoldEquipment(tpl.RowId, randomSeedOverride)`!
- Rolls `MaxDurability` (`0` if `tpl.BaseAttributes` has no `durability_v` `AttributeId == 31`, otherwise seeded `[MinValue, MaxValue]`).
- Populates `MagicAttributes[0..5]` from `tpl.FixedMagicAffixes`:
  - Even indices `0, 2, 4` are explicit (`IsHiddenSeriesSlot == false`); odd indices `1, 3, 5` are hidden (`IsHiddenSeriesSlot == true`).
  - Rolls `Value0`, `Value1`, `Value2` deterministically within `[Param0Min, Param0Max]`, `[Param1Min, Param1Max]`, `[Param2Min, Param2Max]` using `new Random((int)seed)`.

### 3.3 Outbound Sink Port (`IInventoryOutboundSink`)

```csharp
void SendItemDurabilitySync(uint runtimeItemId, int maxDurability, int currentDurability) { }
```
Implemented on `NoOpInventoryOutboundSink`, forwarded on `SelectivePlayerOutboundSink`, and serialized to `ItemDurabilityChangeS2CPacket` (`0x9B`, 13 bytes) in `GameServerClientSession.InventorySink.cs`.
