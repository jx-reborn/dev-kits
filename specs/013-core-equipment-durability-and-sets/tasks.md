# Tasks: Core Equipment Durability, Golden Gear & Set Bonuses

**Input**: Design documents from `/specs/013-core-equipment-durability-and-sets/`  
**Prerequisites**: [plan.md](plan.md), [spec.md](spec.md), [research.md](research.md), [data-model.md](data-model.md), [contracts/](contracts/), [quickstart.md](quickstart.md)

**Tests**: Included across all three mandatory Constitution verification gates (`JXReborn.Protocol.Tests`, `JXReborn.ZoneEngine.Tests`, and `JXReborn.Integration.Tests`).

**Organization**: Tasks are grouped by foundational architecture and user story (`US1`–`US4`) so each increment compiles cleanly (`-warnaserror`), respects **1-type-per-file** and **`sealed` concrete classes**, and can be verified independently.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies on incomplete tasks in the same phase)
- **[Story]**: Maps task to a specific user story (`[US1]`, `[US2]`, `[US3]`, `[US4]`)
- Exact repository-relative file paths included in every task description

---

## Phase 1: Setup (Shared Primitives & Constants)

**Purpose**: Establish zero-dependency primitives for legacy 6.0 equipment abrasion triggers, probabilities, attribute IDs, and repair price scale constants.

- [X] T001 [P] Create `EquipmentAbradeKind` enum (`Attack = 0`, `Defend = 1`, `Move = 2`) in `src/JXReborn.Primitives/Items/EquipmentAbradeKind.cs`
- [X] T002 [P] Create `EquipmentAbrasionConsts` static class (`WeaponAttackRange = 256`, `ArmorDefendRange = 320`, `BootsMoveRange = 2560`, `HorseMoveRange = 5120`, `DurabilityAttributeId = 31`, `IndestructibleAttributeId = 38`, `NormalRepairItemPriceScale = 50`, `NormalRepairMagicPriceScale = 16`, Golden repair multipliers, `WireIndestructibleDurability = -1`) in `src/JXReborn.Primitives/Items/EquipmentAbrasionConsts.cs`

---

## Phase 2: Foundational (Persistence, Asset Snapshot Pipeline, Outbound Sink & `0x9B` Wire Frame)

**Purpose**: Core cross-layer infrastructure (`CharacterItem` `GoldRowId` & `IsBroken` fix, `MemoryPack` Golden/Set catalog templates, `IInventoryOutboundSink.SendItemDurabilitySync`, and `0x9B` 13-byte packet) that MUST be in place before user story simulation slices.

**⚠️ CRITICAL**: Complete Phase 2 before starting Phases 3–6.

- [X] T003 Update `CharacterItem` to add `ushort GoldRowId = 0` property/constructor parameter and fix `SetDurability` so `IsBroken` resets to `false` when `currentDurability > 0` in `src/JXReborn.Domain/Characters/CharacterItem.cs`
- [X] T004 [P] Update `CharacterItemDto` and verify snapshot item payload compatibility in `src/JXReborn.Contracts/Characters/CharacterItemDto.cs` and `src/JXReborn.Contracts/Characters/UpdateCharacterWorldSnapshotDto.cs`
- [X] T005 [P] Configure `CharacterItem.GoldRowId` column (`HasDefaultValue((ushort)0)`) in `src/JXReborn.Infrastructure/EntityFrameworkCore/JXRebornDbContext.cs`
- [X] T006 Map `GoldRowId`, `CurrentDurability`, `MaxDurability`, and `IsBroken` across character DTO mapping and `ApplyWorldSnapshot` in `src/JXReborn.Application/Characters/CharacterAppService.cs`
- [X] T007 [P] Create `[MemoryPackable] sealed partial record GoldenFixedAffixBindingTemplate` in `src/JXReborn.GameAssets/Items/GoldenFixedAffixBindingTemplate.cs`
- [X] T008 [P] Create `[MemoryPackable] sealed partial record EquipmentSuiteBindingTemplate` in `src/JXReborn.GameAssets/Items/EquipmentSuiteBindingTemplate.cs`
- [X] T009 [P] Create `[MemoryPackable] sealed partial record EquipmentSetTemplate` in `src/JXReborn.GameAssets/Items/EquipmentSetTemplate.cs`
- [X] T010 Extend `ItemTemplate` with `RowId`, `FixedMagicAffixes`, and `SuiteBinding` default-valued parameters in `src/JXReborn.GameAssets/Items/ItemTemplate.cs`
- [X] T011 Extend `GameAssetsSnapshotBundle` with `IReadOnlyList<EquipmentSetTemplate>? EquipmentSets = null`, parse `rowId`, `fixedMagicAffixes`, `suiteBinding`, and `equipmentSets` from canonical JSON in `src/JXReborn.GameAssets/Snapshots/GameAssetsSnapshotBundle.cs` and `src/JXReborn.GameAssets/Snapshots/GameAssetsSnapshotLoader.cs`, and verify snapshot compilation in `tools/JXReborn.DataExporter/Compilation/MemoryPackSnapshotCompiler.cs`
- [X] T012 [P] Add `SendItemDurabilitySync(uint runtimeItemId, int maxDurability, int currentDurability)` to `src/JXReborn.ZoneEngine/PlayerSessions/IInventoryOutboundSink.cs`, `src/JXReborn.ZoneEngine/PlayerSessions/NoOpInventoryOutboundSink.cs`, and `src/JXReborn.ZoneEngine/PlayerSessions/SelectivePlayerOutboundSink.cs`
- [X] T013 [P] Create `ItemDurabilityChangeS2CPacket` (`0x9B`, 13 bytes `<BIii`) in `src/JXReborn.Adapter.Legacy/Packets/ItemDurabilityChangeS2CPacket.cs` and implement `SendItemDurabilitySync` in `src/JXReborn.Adapter.Legacy/GameServer/GameServerClientSession.InventorySink.cs`

**Checkpoint**: Foundation ready — persistence, `MemoryPack` asset bundle, outbound sink port, and `0x9B` wire framing compile cleanly.

---

## Phase 3: User Story 1 - Combat & Movement Equipment Durability Abrasion (Priority: P1) 🎯 MVP

**Goal**: Wear down equipped weapons on combat skill casts (`1/256`), armor slots (`Head, Body, Belt, Cuff` `1/320`; `Boots = 0`) on net HP damage taken, and `Boots` (`1/2560`) / `Horse` (`1/5120`) on spatial region transitions while exempting `Mask` and indestructible gear (`MaxDurability == 0` or `indestructible_b` `AttributeId == 38`) and emitting `0x9B` on every durability drop.

**Independent Test**: Run `EquipmentDurabilityAndGoldenPacketTests` (`0x9B` & indestructible `0x5F` vectors) and `EquipmentDurabilityAndGoldenSetTests` abrasion scenarios verifying exact per-slot wear probabilities and indestructible immunity.

### Implementation for User Story 1

- [X] T014 [US1] Extend `PlayerItemSlot` with `GoldRowId`, `IsIndestructible` (`MaxDurability == 0` or `AttributeId == 38`), and `SetDurability` `IsBroken` reset invariant in `src/JXReborn.ZoneEngine/Items/PlayerItemSlot.cs`
- [X] T015 [P] [US1] Update `PlayerItemSyncSnapshot` and `SyncItemS2CPacket` to serialize `MaxDurability = -1` and `CurrentDurability = -1` (`0xFFFF`) for indestructible items in `src/JXReborn.ZoneEngine/PlayerSessions/PlayerItemSyncSnapshot.cs` and `src/JXReborn.Adapter.Legacy/Packets/SyncItemS2CPacket.cs`
- [X] T016 [P] [US1] Create `EquipmentAbrasionCalculator` implementing legacy 6.0 `GetAbradeRandRange` and `TryAbradeSlot` in `src/JXReborn.ZoneEngine/Items/EquipmentAbrasionCalculator.cs`
- [X] T017 [US1] Hook `Attack` (once per skill cast when `FightMode != 0`), `Defend` (when net HP loss `< 0` on damage taken), and `Move` (when `oldRegionId != newRegionId`) abrasion checks and `SendItemDurabilitySync` notifications in `src/JXReborn.ZoneEngine/WorldZones/MapZoneWorker.cs`
- [X] T018 [P] [US1] Add `0x9B` (`13B`) abrasion/break vectors and indestructible `0x5F` (`-1` durability) round-trip tests in `tests/JXReborn.Protocol.Tests/GameServer/EquipmentDurabilityAndGoldenPacketTests.cs`
- [X] T019 [US1] Add deterministic `ZoneSimulationHarness` tests for `Attack`, `Defend` (`Boots == 0`), `Move` (`Mask == 0`), Peace-mode immunity, and `indestructible_b` immunity in `tests/JXReborn.ZoneEngine.Tests/Items/EquipmentDurabilityAndGoldenSetTests.cs`

**Checkpoint**: User Story 1 (`US1`) is complete and independently verified.

---

## Phase 4: User Story 2 - Broken Equipment Stat Suspension & Repair Restoration (Priority: P1)

**Goal**: Retain `0`-durability gear safely in its `Equip` slot marked `IsBroken = true` with 100% of base stats, magic affixes, and Five-Elements links suspended, clamp current Life/Mana/Stamina to lowered maximums, prevent re-equipping broken items from the bag, and restore durability/stats via `EquipmentRepairCalculator` + `RepairEquipmentItemZoneCommand` (Normal/Blue & Golden Cheap/Costly formulas).

**Independent Test**: Wear equipped life-granting armor to `0` durability with a full `6x10` bag, verify broken-in-slot retention, stat suspension, and vital clamping, then execute `RepairEquipmentItemZoneCommand` and verify gold deduction, `0x9B` sync, `IsBroken == false`, and full stat restoration.

### Implementation for User Story 2

- [X] T020 [P] [US2] Create `EquipmentRepairCalculator` implementing Normal/Blue (`ItemPriceScale = 50`, `MagicPriceScale = 16`) and Golden (`Cheap` `10 * Level^2 * FixDur` with `LostDur = max(1, FixDur / 10)` vs `Costly` `30 * Level^2 * FixDur`) quotes in `src/JXReborn.ZoneEngine/Items/EquipmentRepairCalculator.cs`
- [X] T021 [P] [US2] Create `RepairEquipmentItemZoneCommand` (`CharacterId, LeaseEpoch, RuntimeItemId, IsCheapGoldenRepair`) in `src/JXReborn.ZoneEngine/Items/RepairEquipmentItemZoneCommand.cs`
- [X] T022 [US2] Add `PlayerItemSlot.Repair` in `src/JXReborn.ZoneEngine/Items/PlayerItemSlot.cs` and add vital clamping (`CurrentLife`, `CurrentMana`, `CurrentStamina` clamped to updated `EffectiveMax*`) upon broken stat suspension in `src/JXReborn.ZoneEngine/Items/PlayerInventoryGrid.cs`
- [X] T023 [US2] Register `IZoneCommandHandler<RepairEquipmentItemZoneCommand>` in `src/JXReborn.ZoneEngine/Items/InventoryLootCommandHandler.cs` and implement `HandleRepairEquipmentItem` + broken-transition stat/vital sync in `src/JXReborn.ZoneEngine/WorldZones/MapZoneWorker.cs`
- [X] T024 [US2] Add `ZoneSimulationHarness` tests for mid-combat armor breaking with a full bag, vital clamping, broken re-equip rejection, Normal/Blue repair pricing, and Golden Cheap vs Costly repair in `tests/JXReborn.ZoneEngine.Tests/Items/EquipmentDurabilityAndGoldenSetTests.cs`

**Checkpoint**: User Stories 1 and 2 (`US1` + `US2`) are complete — full durability wear, broken suspension, and repair lifecycle verified.

---

## Phase 5: User Story 3 - Golden Equipment Generation & Fixed Affix Activation (Priority: P1)

**Goal**: Index Golden templates by 1-based `RowId` and `"goldenFixed"` affixes in `ItemTemplateCatalog`, deterministically roll named Golden items with up to 6 fixed magic affixes (`0, 2, 4` explicit; `1, 3, 5` hidden) in `ItemGenerator.GenerateGoldEquipment`, and encode `Pattern = 0x10` + `DetailType = (short)(GoldRowId - 1)` on `0x5F` (`SyncItemS2CPacket`).

**Independent Test**: Generate Golden items from `golden-equipment.items.json`, verify rolled affix bounds, verify `0x5F` byte `+6..7` equals `GoldRowId - 1`, and verify single-piece Five-Elements activation.

### Implementation for User Story 3

- [X] T025 [US3] Extend `IItemTemplateCatalog` and `ItemTemplateCatalog` to index Golden templates by `(ItemQualityKind.Golden, item.RowId)` and expose `EquipmentSetTemplate` & Classic suite threshold lookups in `src/JXReborn.GameAssets/Items/IItemTemplateCatalog.cs` and `src/JXReborn.GameAssets/Items/ItemTemplateCatalog.cs`
- [X] T026 [US3] Implement `ItemGenerator.GenerateGoldEquipment(int goldRowId, uint? randomSeedOverride = null)` (rolling base durability and `0..5` `FixedMagicAffixes` from `"goldenFixed"`) and Golden `GenerateFromItemCode` routing in `src/JXReborn.GameAssets/Items/ItemGenerator.cs`
- [X] T027 [US3] Update `PlayerItemSyncSnapshot` and `SyncItemS2CPacket` to carry `GoldRowId`, encode `+6..7 DetailType = (short)(GoldRowId - 1)` and rolled `Value0` in `MagicLevel0..5` when `Quality == ItemQualityKind.Golden` in `src/JXReborn.ZoneEngine/PlayerSessions/PlayerItemSyncSnapshot.cs` and `src/JXReborn.Adapter.Legacy/Packets/SyncItemS2CPacket.cs`
- [X] T028 [P] [US3] Add byte-exact Golden `0x5F` (`Pattern = 0x10`, `WireDetailType = GoldRowId - 1`) test vectors in `tests/JXReborn.Protocol.Tests/GameServer/EquipmentDurabilityAndGoldenPacketTests.cs`
- [X] T029 [US3] Add Golden item generation determinism, fixed affix `[Min, Max]` bound assertions, and single-piece Five-Elements activation tests in `tests/JXReborn.ZoneEngine.Tests/Items/EquipmentDurabilityAndGoldenSetTests.cs`

**Checkpoint**: User Story 3 (`US3`) is complete — Golden item generation and `0x5F` wire fidelity verified.

---

## Phase 6: User Story 4 - Multi-Piece Classic & Extended Equipment Set Bonuses (Priority: P2)

**Goal**: Implement `ReBuildGoldEquipmentSuite` threshold derivation (`suiteId 1..82`) + explicit catalog set loading (`suiteId 83+` and `extSuiteId`) in `ItemTemplateCatalog`, and evaluate both **System A (Classic Golden Suite unlocking all 3 hidden slots unconditionally at full distinct `GoldRowId` count)** and **System B (Extended Suite unlocking `extHiddenAffixIds[0]` at partial distinct `extSuitePieceIndex` count and `extHiddenAffixIds[0..1]` at full count)** in `PlayerInventoryGrid`, excluding Broken pieces.

**Independent Test**: Equip partial and full Classic (`suiteId = 1`, `suiteId = 83`) and Extended (`extSuiteId = 1`, `3`-piece & `5`-piece) sets in `ZoneSimulationHarness`, verify duplicate ring non-stacking, break 1 piece to `0` durability to confirm bonus suspension, and repair it to confirm reactivation.

### Implementation for User Story 4

- [X] T030 [US4] Implement `ReBuildGoldEquipmentSuite` (`suiteId 1..82` distinct Golden piece counting) and composite `(bool IsExtendedSuite, int SetId)` indexing in `src/JXReborn.GameAssets/Items/ItemTemplateCatalog.cs`
- [X] T031 [US4] Extend `PlayerInventoryGrid.RecalculateEquipmentAndFiveElementsActivation` (passing `IItemTemplateCatalog?` from `MapZoneWorker` / `PlayerZoneState`) to evaluate Classic `suiteId` full-set 3-hidden-slot unlock and Extended `extSuiteId` partial/full `extHiddenAffixIds` bonuses while excluding `IsBroken` pieces and duplicate `GoldRowId` / `ExtSuitePieceIndex` rings in `src/JXReborn.ZoneEngine/Items/PlayerInventoryGrid.cs` and `src/JXReborn.ZoneEngine/WorldZones/MapZoneWorker.cs`
- [X] T032 [US4] Add Classic (`suiteId 1..82` & `83+`) and Extended (`extSuiteId`) partial/full set bonus, duplicate ring non-stacking, and break/repair set transition tests in `tests/JXReborn.ZoneEngine.Tests/Items/EquipmentDurabilityAndGoldenSetTests.cs`

**Checkpoint**: All four user stories (`US1`–`US4`) are complete and verified in simulation.

---

## Phase 7: Polish & Cross-Cutting Verification

**Purpose**: Recompile `artifacts/jxreborn-data.pack` binary snapshot, validate end-to-end live TCP + EF Core persistence round-trips, and enforce zero-warning build and full regression pass.

- [X] T033 Recompile `artifacts/jxreborn-data.pack` using `tools/JXReborn.DataExporter` so Tier 2 `MemoryPack` snapshot SHA-256 matches `data/` canonical JSON definitions with `EquipmentSets`, `FixedMagicAffixes`, and `SuiteBinding` populated
- [X] T034 [P] Create end-to-end live TCP + EF Core SQLite integration tests (Golden `0x5F` world-entry sync, combat `0x9B` durability abrasion, `RepairEquipmentItemZoneCommand` `0x68` + `0x9B` wire sync, and `GoldRowId` / `IsBroken` logout-login persistence) in `tests/JXReborn.Integration.Tests/ItemsAndEconomy/EquipmentDurabilityAndSetIntegrationTests.cs`
- [X] T035 Execute `dotnet build JXReborn.slnx -warnaserror` and full `dotnet test JXReborn.slnx` regression gate, verifying zero warnings and 100% passing tests across all projects

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1 (Setup)**: Starts immediately (`T001`–`T002` in parallel).
- **Phase 2 (Foundational)**: Depends on Phase 1; blocks Phases 3–6 (`T003`–`T013`).
- **Phase 3 (`US1` — Abrasion)**: Depends on Phase 2 (`T014`–`T019`).
- **Phase 4 (`US2` — Broken Suspension & Repair)**: Depends on Phase 3 (`PlayerItemSlot.IsIndestructible` & abrasion hooks) (`T020`–`T024`).
- **Phase 5 (`US3` — Golden Generation & `0x5F` Wire Sync)**: Depends on Phase 2 and `T014`–`T015` (`T025`–`T029`); can run in parallel with Phase 4 on separate files (`GameAssets` & `0x5F` vs `EquipmentRepairCalculator`).
- **Phase 6 (`US4` — Classic & Extended Set Bonuses)**: Depends on Phase 4 (`PlayerInventoryGrid` broken suspension) and Phase 5 (`ItemTemplateCatalog` & Golden generation) (`T030`–`T032`).
- **Phase 7 (Polish & Integration)**: Depends on Phases 3–6 (`T033`–`T035`).

### Parallel Opportunities

- **Phase 1**: `T001` and `T002` run in parallel.
- **Phase 2**: `T004`, `T005`, `T007`, `T008`, `T009`, `T012`, and `T013` touch independent layers/files and run in parallel once `T003` is defined.
- **Phase 3**: `T015`, `T016`, and `T018` run in parallel after `T014`.
- **Phase 4**: `T020` and `T021` run in parallel before `T023`.

---

## Implementation Strategy

### MVP First (`US1` + `US2` Core Durability Loop)

1. Complete Phase 1 + Phase 2 foundational contracts.
2. Deliver Phase 3 (`US1`) and Phase 4 (`US2`) to achieve complete legacy 6.0 combat/movement durability abrasion, safe broken-in-slot stat suspension, and repair restoration.
3. Validate via `EquipmentDurabilityAndGoldenPacketTests` and `EquipmentDurabilityAndGoldenSetTests`.

### Incremental Delivery (`US3` + `US4` Golden Gear & Dual Set Systems)

1. Deliver Phase 5 (`US3`) to unlock deterministic named Golden equipment generation and `0x5F` `GoldRowId - 1` client rendering.
2. Deliver Phase 6 (`US4`) to unlock Classic (`suiteId 1..500`) and Extended (`extSuiteId`) multi-piece set bonuses reactive to equip, unequip, break, and repair events.
3. Finalize Phase 7 (`jxreborn-data.pack` recompilation + `EquipmentDurabilityAndSetIntegrationTests` live TCP & EF Core round-trip).
