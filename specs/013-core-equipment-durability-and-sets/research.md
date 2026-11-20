# Ground-Truth Research & Technical Rationale: Core Equipment Durability, Golden Gear & Set Bonuses

**Feature Branch**: `013-core-equipment-durability-and-sets`  
**Date**: 2026-11-11  
**Spec**: [spec.md](spec.md)

---

## 1. Authoritative Ground-Truth Sources Inspected

All mechanics, probabilities, wire frames, and set activation formulas in this feature were verified directly against the workspace's primary ground-truth artifacts (`Constitution Principle III`):

1. **Legacy 6.0 Settings & Reference Behavior**:
   - `abraderate.ini` (`1–88`): Authoritative legacy 6.0 repair price scales (`ItemPriceScale=50`, `MagicPriceScale=16`, `WarningBaseline=5`) and per-slot `Attack`, `Defend`, and `Move` abrasion `nRandRange` denominators.
   - `repairitem.lua` (`18–92`, `175–198`): Authoritative legacy 6.0 Normal/Blue repair price formula and Golden Cheap vs. Costly repair price & max-durability loss formulas.
   - **Golden Item `0x5F` Encoding & Set Activation**: On `0x5F` (`s2c_syncitem`, 80 bytes), `Quality == 1` (`Golden`) packs `Pattern = 0x10` (`(Quality << 4) | Genre`), writes `(short)(GoldRowId - 1)` (`0`-based `goldequip.txt` index) into `+0x06 DetailType`, writes `-1` (`0xFFFF`) for indestructible durability, and supports Classic Golden Suites (`suiteId 1..82` vs `83..500`) alongside Extended Suites (`extSuiteId`, `extSuitePieceIndex`, `extHiddenAffixIds`), emitting 13-byte `0x9B` (`s2c_itemrepair` / `<BIii`) updates.
2. **Early Legacy Compatibility Baseline**:
   - Indestructible affix check (`indestructible_b`, attribute `38`), probabilistic `1 / nRandRange` abrasion per hit/cast/region-step, normal equipment repair quotes, and Five-Elements adjacency links triggered across combat attack, net damage defense, and spatial region crossings.
3. **Canonical Game Data & Existing Codebase (repository root & `jx-swarm`)**:
   - `schemas/item-catalog.schema.json` (`lines 97–154, 193–295`), `data/items/golden-equipment.items.json`, `data/items/sets-objects-recipes.items.json`, and `data/items/magic-affixes.items.json` (`affixPool: "goldenFixed"`).
   - `src/JXReborn.Adapter.Legacy/Packets/ProtocolOpcodes.cs` (`line 160`: `s2c_itemrepair = 0x9B // 155 (13B)`) and `SyncItemS2CPacket.cs` (`lines 68–135`).
   - `src/JXReborn.Domain/Characters/CharacterItem.cs` (`lines 131–143`) and `src/JXReborn.ZoneEngine/Items/PlayerItemSlot.cs` (`lines 231–243`).

---

## 2. Equipment Durability Abrasion Probabilities & Trigger Sites (legacy 6.0)

During slot abrasion checks, if `nRandRange <= 0` or the item is indestructible (`m_nCurrentDur == -1`), no roll occurs. Otherwise, `Random.Next(nRandRange) == 0` (`1 / nRandRange` probability) deducts `1` point from `CurrentDurability`.

Crucially, legacy 6.0 (`server1/settings/item/abraderate.ini:6–75`) lowered wear rates significantly compared to early beta defaults (`64`) and removed `Foot` (`Boots`) from defensive abrasion. Note the distinction between `EquipmentPartKind` (`itempart_*` slot index in `_equipSlots[0..14]`) and `ItemTemplate.DetailType` (`equip_*` item type):

| Trigger (`EquipmentAbradeKind`) | Simulation Call Site | Equipped Slot (`EquipmentPartKind`) | Item `DetailType` (`equip_*`) | 6.0 `nRandRange` (`abraderate.ini`) | Probability (`-1` Dur) |
| :--- | :--- | :--- | :---: | :---: | :---: |
| **`Attack` (`0`)** | Once per skill cast when `player.FightMode != 0` | `EquipmentPartKind.Weapon` (`3`) | `0` (Melee) / `1` (Range) | `256` | `1 / 256` |
| **`Attack` (`0`)** | — | All other slots (`0..2, 4..14`) | All other types | `0` | `0` (Never) |
| **`Defend` (`1`)** | On damage taken when net HP change `< 0` (`hpAfter < hpBefore`) | `EquipmentPartKind.Head` (`0`) | `7` (`equip_helm`) | `320` | `1 / 320` |
| **`Defend` (`1`)** | Same hit (independent roll) | `EquipmentPartKind.Body` (`1`) | `2` (`equip_armor`) | `320` | `1 / 320` |
| **`Defend` (`1`)** | Same hit (independent roll) | `EquipmentPartKind.Belt` (`2`) | `6` (`equip_belt`) | `320` | `1 / 320` |
| **`Defend` (`1`)** | Same hit (independent roll) | `EquipmentPartKind.Cuff` (`5`) | `8` (`equip_cuff`) | `320` | `1 / 320` |
| **`Defend` (`1`)** | — | `EquipmentPartKind.Foot` (`4`, Boots) & all others | `5` (`equip_boots`), jewelry, etc. | `0` | `0` (**Boots = 0 on Defend in 6.0!**) |
| **`Move` (`2`)** | On spatial region crossing (`oldRegionId != newRegionId`) | `EquipmentPartKind.Foot` (`4`) | `5` (`equip_boots`) | `2560` | `1 / 2560` |
| **`Move` (`2`)** | Same region crossing (independent roll) | `EquipmentPartKind.Horse` (`10`) | `10` (`equip_horse`) | `5120` | `1 / 5120` |
| **`Move` (`2`)** | — | `EquipmentPartKind.Mask` (`11`) & all others | `11` (`equip_mask`), etc. | `0` | `0` (**Mask always skipped!**) |

---

## 3. Indestructible Items & Wire Representation

An equipment item is **Indestructible** (`PlayerItemSlot.IsIndestructible == true`) when **either** condition holds:
1. **No Finite Template Durability**: Its `ItemTemplate.BaseAttributes` lacks `durability_v` (`attributeId == 31`) or rolls `MaxDurability == 0` (e.g., horses without durability, masks, special event rings/amulets).
2. **Indestructible Magic Affix (`indestructible_b`)**: Any populated slot in `MagicAttributes[0..5]` has `AttributeId == 38` (`indestructible_b` / `"indestructible_b"`).

**Runtime & Wire Rules**:
- Indestructible items **never** abrade (`EquipmentAbrasionCalculator` short-circuits before rolling), **never** transition to `IsBroken = true`, and return `CanRepair = false` (`PriceGold = 0`).
- On `0x5F` (`s2c_syncitem`, 80 bytes), `PlayerItemSyncSnapshot` maps indestructible items to `MaxDurability = -1` (`0xFFFF`) and `CurrentDurability = -1` (`0xFFFF`) at byte offsets `+65..66` and `+67..68`, causing the legacy client tooltip to render *"Không thể phá hủy"* (Indestructible) instead of `0 / 0`.

---

## 4. Authoritative `0x9B` (`s2c_itemrepair` / `s2c_itemdurabilitychange`) 13-Byte Wire Frame

Whereas early 2003 protocol revisions used a 9-byte delta struct, **legacy 6.0 (`ProtocolOpcodes.cs:160`)** unified both combat/movement abrasion updates and shop/script repair updates onto a single **13-byte (`<BIii`)** `0x9B` packet (`s2c_itemrepair`):

- `[0]`: `byte Opcode = 0x9B` (`ProtocolOpcodes.s2c_itemrepair`)
- `[1..4]`: `uint32 RuntimeItemId` (little-endian ephemeral item ID matching `0x5F`)
- `[5..8]`: `int32 MaxDurability` (little-endian current max durability; updated on Cheap Golden repair)
- `[9..12]`: `int32 CurrentDurability` (little-endian new current durability, `0..MaxDurability`)

Sending both `MaxDurability` and `CurrentDurability` in one 13-byte frame prevents cumulative delta drift on packet reordering and updates both `CurrentDurability` (on abrasion or Costly/Normal repair) and reduced `MaxDurability` (on Cheap Golden repair) atomically.

---

## 5. Broken Equipment Gameplay Decision (`0 / Max` Durability)

### Decision: Remain Equipped in Slot as `IsBroken = true` with 100% Stat & Set Suspension
- **Historical Context**: Early 2003 protocol revisions permanently deleted items at `0` durability, whereas later 6.0 revisions converted items or tried moving broken gear to the bag, creating notorious edge cases when a player's `6x10` `EquipRoom` was full mid-combat (stranding gear on `pos_hand` cursor lock or dropping valuable Golden gear onto the ground).
- **Approved `JXReborn` Rule (`Clarifications 2026-11-11`)**:
  1. When an equipped destructible item (`MaxDurability > 0` and `!IsIndestructible`) abrades from `1` to `0`, it **remains in `ItemContainerPlace.Equip`** with `CurrentDurability = 0` and `IsBroken = true`.
  2. `0x9B` (`MaxDurability`, `CurrentDurability = 0`) is sent immediately so the client renders the red/broken overlay on the slot.
  3. `PlayerInventoryGrid.RecalculateEquipmentAndFiveElementsActivation(player)` runs immediately: because `equip.IsBroken == true`, it skips the item's `BaseAttributes`, skips all 6 `MagicAttributes`, excludes the item as a Five-Elements generator (`!itemA.IsBroken`, `!itemB.IsBroken`), and excludes the item from both Classic (`suiteId`) and Extended (`extSuiteId`) set piece counts.
  4. **Vital Clamping**: If suspending armor/amulet/set bonuses reduces `EffectiveMaxLife`, `EffectiveMaxMana`, or `EffectiveMaxStamina` below `player.CurrentLife / CurrentMana / CurrentStamina`, current vitals are clamped (`CurrentLife = Math.Min(CurrentLife, Math.Max(1, EffectiveMaxLife))` when alive, `CurrentMana = Math.Min(CurrentMana, EffectiveMaxMana)`, `CurrentStamina = Math.Min(CurrentStamina, EffectiveMaxStamina)`) and synced to the client.
  5. **No Re-Equipping While Broken**: `PlayerInventoryGrid.CanEquipItem` (`line 383`) already checks `|| item.IsBroken`, so if a player manually unequips a Broken item into `EquipRoom`, it cannot be re-equipped until repaired.
- **Bug Fix Required in Existing Entities**:
  Both `CharacterItem.SetDurability` (`src/JXReborn.Domain/Characters/CharacterItem.cs:131–143`) and `PlayerItemSlot.SetDurability` (`src/JXReborn.ZoneEngine/Items/PlayerItemSlot.cs:231–243`) currently set `IsBroken = true` when `MaxDurability > 0 && currentDurability == 0`, **but fail to reset `IsBroken = false` when `currentDurability > 0`!** Both methods MUST be updated to `IsBroken = !IsIndestructible && MaxDurability > 0 && currentDurability == 0;` (and `IsBroken = MaxDurability > 0 && currentDurability == 0;` on `CharacterItem`) so repairing an item deterministically clears `IsBroken`.

---

## 6. Critical Golden Item `0x5F` (`s2c_syncitem`, 80B) Wire Rule & Fixed Affix Generation

### 6.1 `0x5F` Golden Wire Encoding
In `data/items/golden-equipment.items.json`, every Golden item defines:
- `"rowId": 1..N` (**1-based** row index from `goldequip.txt`, e.g., `"rowId": 1` = *Mộng Long Chính Hồng Tăng Mão*, `"detailType": 7`).
- Internally, `PlayerItemSlot.DetailType` MUST remain `7` (`equip_helm`) so `PlayerInventoryGrid.IsMatchingEquipmentSlot` equips it onto `EquipmentPartKind.Head` (`0`), while `PlayerItemSlot.GoldRowId` (`ushort`) stores the 1-based `rowId` (`1`).
- **On the `0x5F` wire packet (`SyncItemS2CPacket`)**:
  - Offset `+5` (`Pattern`): `Genre = 0 (Equipment)`, `Quality = 1 (Golden)` → `Pattern = 0x10`.
  - Offset `+6..7` (`DetailType`, `int16`): When `Quality == ItemQualityKind.Golden` and `GoldRowId > 0`, the `0x5F` serializer writes **`(short)(GoldRowId - 1)`** (`0`-based index into client `goldequip.txt`!) instead of `equip_helm (7)`! If `DetailType = 7` were sent on `0x5F`, the legacy client would look up row `8` of `goldequip.txt` and display the wrong Golden item!
  - Offset `+13..24` (`MagicLevel[0..5]`, `6 x int16`): Carries the rolled concrete parameter `Value0` for each of the 6 Golden fixed magic affixes so the client renders exact rolled stats.

### 6.2 Golden Fixed Magic Affixes (`fixedMagicAffixes` → `affixPool: "goldenFixed"`)
In `golden-equipment.items.json`, each Golden item defines up to 6 entries in `"fixedMagicAffixes"` (indices `0..5`):
- **Slots `0, 2, 4`** (`isHiddenSeriesSlot: false`): Explicit magic affixes — always active while equipped and `!IsBroken`.
- **Slots `1, 3, 5`** (`isHiddenSeriesSlot: true`): Hidden magic affixes — activated either progressively via Five-Elements adjacency (`ActivatingPartsByEquipSlot`) OR all 3 simultaneously when the item's Classic Golden Set (`suiteId`) reaches full piece completion!
- `ItemGenerator.GenerateGoldEquipment(int goldRowId, uint? randomSeedOverride = null)` resolves the Golden `ItemTemplate` via `_itemCatalog.TryGetItemByRowId(ItemQualityKind.Golden, goldRowId, out tpl)`, computes deterministic `MaxDurability` from `durability_v`, and for each slot `i` (`0..5`) in `tpl.FixedMagicAffixes`:
  - Resolves fallback metadata via `_itemCatalog.TryGetMagicAffixById(binding.AffixRowId, out affix, "goldenFixed")` if `binding.AttributeId == 0`.
  - Deterministically rolls `Value0` in `[binding.Param0Min, binding.Param0Max]`, `Value1` in `[binding.Param1Min, binding.Param1Max]`, and `Value2` in `[binding.Param2Min, binding.Param2Max]` using the seeded PRNG (`RandomSeed`).

---

## 7. Dual Equipment Set Bonus Systems (`sets-objects-recipes.items.json`)

Inspection of `data/items/golden-equipment.items.json`, `data/items/sets-objects-recipes.items.json`, confirms **two coexisting set bonus mechanisms** that can even apply to the same Golden item simultaneously (e.g., `golden-equipment.items.json:53380` carries `"suiteId": 61` AND `"extSuiteId": 2`):

### System A — Classic Golden Suite (`suiteId > 0`, `isExtendedSuite: false`)
- **Threshold Resolution**:
  1. **`suiteId 1..82` (Classic Faction Golden Sets, e.g., *Mộng Long* `suiteId = 1`)**: Not listed in `sets-objects-recipes.items.json`; catalog loading derives `pieceCount` at startup by counting distinct Golden `ItemTemplate` entries in `golden-equipment.items.json` whose `SuiteBinding.SuiteId == suiteId`.
  2. **`suiteId 83..500` (Catalog Sets, e.g., *Bộ Thanh Câu* `suiteId = 83`, `pieceCount = 7`)**: Explicitly defined in `sets-objects-recipes.items.json` with `"isExtendedSuite": false` and `"pieceCount"`.
- **Runtime Evaluation**:
  - Count **distinct `GoldRowId`** (or `Template.RowId`) among equipped, non-broken (`!equip.IsBroken`) items sharing `suiteId > 0` (so equipping two identical Golden rings with the same `GoldRowId` counts as `1`, whereas two distinct rings of the same set with different `GoldRowId`s count as `2`).
  - When `equippedDistinctPieces >= requiredPieceCount` (`requiredPieceCount > 0`), **ALL 3 hidden slots (`ActiveHiddenSlots[0] = true, [1] = true, [2] = true` → magic slots `1, 3, 5`)** on every intact equipped piece of that `suiteId` activate unconditionally, bypassing Five-Elements `Generates` checks!
  - When `equippedDistinctPieces < requiredPieceCount`, hidden slots `1, 3, 5` fall back to standard Five-Elements adjacency (`ActivatingPartsByEquipSlot`).

### System B — Extended Suite (`extSuiteId > 0`, `isExtendedSuite: true`)
- **Catalog Schema**:
  - In `sets-objects-recipes.items.json` (`lines 5440–5476`): Entries with `"isExtendedSuite": true` (e.g., `id: 1` *Bộ Mở Rộng Mộng Long*, `pieceCount: 5`) define two `activationThresholds`:
    - Partial threshold (`unlockedHiddenSlots == 1`, e.g., `equippedPieces: 3`)
    - Full threshold (`unlockedHiddenSlots == 2`, e.g., `equippedPieces: 5` / `pieceCount`)
  - Note that `id: 1..N` for `isExtendedSuite: true` overlaps numerically with Classic `suiteId: 1..N` (`isExtendedSuite: false`), so `ItemTemplateCatalog` MUST index `EquipmentSetTemplate` by composite key `(bool IsExtendedSuite, int SetId)`!
  - On each participating `ItemTemplate.SuiteBinding`: `ExtSuiteId > 0`, `ExtSuitePieceIndex` (piece slot index within the extended suite), and `ExtHiddenAffixIds` (`[affixId0, affixId1]` referencing `"goldenFixed"` in `magic-affixes.items.json`).
- **Runtime Evaluation**:
  - For each `extSuiteId > 0` worn by the player, count **distinct `ExtSuitePieceIndex`** values across equipped, non-broken (`!equip.IsBroken`) pieces matching `extSuiteId`.
  - If `distinctPieceCount >= partialPieceCount`: resolve `ExtHiddenAffixIds[0]` from `_itemCatalog.TryGetMagicAffixById(id0, out affix0, "goldenFixed")` and apply its attribute (`Param0Max` / `BaseValue`) to `player`.
  - If `distinctPieceCount >= fullPieceCount`: also resolve `ExtHiddenAffixIds[1]` from `"goldenFixed"` and apply its attribute to `player`!

---

## 8. Authoritative Repair Pricing Formulas (`repairitem.lua` & `abraderate.ini`)

### 8.1 Normal / Blue Equipment (`Quality == ItemQualityKind.Normal`)
From `abraderate.ini` (`ItemPriceScale = 50`, `MagicPriceScale = 16`) and `repairitem.lua:180` using 64-bit integer arithmetic to prevent intermediate `int32` overflow on expensive items:
$$\text{MissingDur} = \text{MaxDurability} - \text{CurrentDurability}$$
$$\text{SumMagic} = \sum_{i=0}^{5} \text{MagicAttributeLevel}_i$$
$$\text{RepairPrice} = \left\lfloor \frac{\left\lfloor \frac{\left\lfloor \frac{\text{PriceGold} \times 50}{100} \right\rfloor \times \text{MissingDur}}{\text{MaxDurability}} \right\rfloor \times (16 + \text{SumMagic})}{16} \right\rfloor$$
- Repair sets `CurrentDurability = MaxDurability` (`MaxDurability` unchanged) and clears `IsBroken = false`.

### 8.2 Golden Equipment (`Quality == ItemQualityKind.Golden`)
From `server1/script/item/repairitem.lua` (`lines 42–57`):
- Let $\text{FixDur} = \text{MaxDurability} - \text{CurrentDurability}$ (requires $\text{FixDur} > 0$) and $L = \max(1, \text{ItemLevel})$:
  1. **Cheap / Simple Repair (`isCheapGoldenRepair == true`)**:
     - $\text{PriceGold} = 10 \times L^2 \times \text{FixDur}$
     - $\text{LostMaxDur} = \max(1, \lfloor \text{FixDur} / 10 \rfloor)$
     - $\text{NewMaxDur} = \max(1, \text{MaxDurability} - \text{LostMaxDur})$
     - $\text{NewCurDur} = \text{NewMaxDur}$, `IsBroken = false`.
  2. **Costly / Careful Repair (`isCheapGoldenRepair == false`)**:
     - $\text{PriceGold} = 30 \times L^2 \times \text{FixDur}$ (`3 * CheapPrice`)
     - $\text{NewMaxDur} = \text{MaxDurability}$ (zero max durability loss)
     - $\text{NewCurDur} = \text{MaxDurability}$, `IsBroken = false`.
