# Implementation Plan: Core Equipment Durability, Golden Gear & Set Bonuses

**Branch**: `013-core-equipment-durability-and-sets` | **Date**: 2026-11-11 | **Spec**: [spec.md](spec.md)  
**Input**: Feature specification from `/specs/013-core-equipment-durability-and-sets/spec.md` and ground-truth verification in `/specs/013-core-equipment-durability-and-sets/research.md`

---

## Summary

Deliver **013-core-equipment-durability-and-sets** across the `JXReborn` primitives, static game asset pipeline, persistence layer, 18-Hz zone simulation engine, and Legacy 6.0 wire adapter in strict compliance with **Constitution v1.4.0** and **JX Reborn Data Standard v1**:

1. **legacy 6.0 Combat & Movement Durability Abrasion**: Implement zero-allocation per-action abrasion evaluation (`EquipmentAbrasionCalculator`) hooked into `MapZoneWorker` on combat skill casts (`Attack`: Weapon `1/256` when `FightMode != 0`), net-damage hits (`Defend`: Head, Body, Belt, Cuff `1/320` each; Boots `0`), and spatial region transitions (`Move`: Foot/Boots `1/2560`, Horse `1/5120`, Mask `0`), exempting indestructible gear (`MaxDurability == 0` or `indestructible_b` `AttributeId == 38`) and emitting 13-byte `0x9B` (`ItemDurabilityChangeS2CPacket`) frames on every change.
2. **Broken-in-Slot Stat Suspension & Core Repair Engine**: Keep `0`-durability gear safely in `ItemContainerPlace.Equip` marked `IsBroken = true` with 100% of base stats, magic affixes, Five-Elements links, and set piece counts suspended, clamp current Life/Mana/Stamina to lowered maximums, fix `CharacterItem.SetDurability` and `PlayerItemSlot.SetDurability` to clear `IsBroken` when `CurrentDurability > 0`, and deliver `EquipmentRepairCalculator` + `RepairEquipmentItemZoneCommand` supporting Normal/Blue and Golden (Cheap vs Costly) repair formulas.
3. **Golden Equipment Catalog, Generation & `0x5F` Wire Fidelity**: Extend `ItemTemplate` (`RowId`, `FixedMagicAffixes`, `SuiteBinding`), `GameAssetsSnapshotBundle`, and `ItemTemplateCatalog` to load Golden fixed affixes and suite definitions from `data/items/*.items.json` & `artifacts/jxreborn-data.pack`; add `ItemGenerator.GenerateGoldEquipment`; persist `GoldRowId` on `CharacterItem`; and serialize `(short)(GoldRowId - 1)` into `+0x06 DetailType` on `0x5F` (`SyncItemS2CPacket`) for `Quality == Golden` alongside `-1` (`0xFFFF`) durability for indestructible items.
4. **Dual Equipment Set Bonus Evaluation (Classic + Extended)**: Extend `PlayerInventoryGrid.RecalculateEquipmentAndFiveElementsActivation` to evaluate both **System A (Classic Golden Suites `suiteId 1..500`, including auto-derived `ReBuildGoldEquipmentSuite` thresholds for `1..82`)** unlocking all 3 hidden slots unconditionally at full distinct `GoldRowId` count, and **System B (Extended Suites `extSuiteId`)** unlocking `extHiddenAffixIds[0]` at partial distinct `extSuitePieceIndex` count and `extHiddenAffixIds[0..1]` at full count.

---

## Technical Context

- **Language/Version**: C# 14 on .NET 10 (`net10.0`), `Nullable=enable`, `TreatWarningsAsErrors=true`, `AnalysisLevel=latest`, `AllowUnsafeBlocks=true`.
- **Primary Dependencies**:
  - `MemoryPack` (zero-copy Tier 2 `artifacts/jxreborn-data.pack` binary snapshot compilation/loading in `JXReborn.GameAssets` and `JXReborn.DataExporter`)
  - `EF Core 10` (`SQL Server` production + `SQLite` / `InMemory` testing in `JXReborn.Infrastructure`)
  - `System.Buffers.Binary` / `Span<byte>` (zero-allocation packet framing in `JXReborn.Adapter.Legacy`)
- **Storage**: `CharacterItems` relational table (`JXRebornDbContext`) extended with `GoldRowId` (`ushort`, default `0`) alongside existing `CurrentDurability`, `MaxDurability`, and `IsBroken`; Tier 1 canonical JSON (`data/items/golden-equipment.items.json`, `sets-objects-recipes.items.json`, `magic-affixes.items.json`) compiled to Tier 2 `artifacts/jxreborn-data.pack`.
- **Testing**: `xUnit` across 3 verification tiers:
  1. `tests/JXReborn.Protocol.Tests/GameServer/EquipmentDurabilityAndGoldenPacketTests.cs` (`0x9B` 13B & Golden/Indestructible `0x5F` 80B symmetric round-trip vectors)
  2. `tests/JXReborn.ZoneEngine.Tests/Items/EquipmentDurabilityAndGoldenSetTests.cs` (`ZoneSimulationHarness` combat/move abrasion to `0`, vital clamping, Golden generation, Classic + Extended set thresholds, and repair restoration)
  3. `tests/JXReborn.Integration.Tests/ItemsAndEconomy/EquipmentDurabilityAndSetIntegrationTests.cs` (live TCP encrypted `0x5F` + `0x9B` wire sync and EF Core snapshot persistence round-trip)
- **Target Platform**: Cross-platform .NET 10 Server (Windows / Linux x64 & ARM64)
- **Project Type**: Modular Monolith / Sharded 18-Hz Real-Time MMORPG Server
- **Performance Goals**: Zero heap allocations on per-cast/per-hit/per-region-crossing abrasion checks and equipment set recalculation (`stackalloc` scratch buffers for distinct piece counting); preserve `< 55.5 ms` 18-Hz frame tick budget.
- **Constraints**:
  - Strict **1-type-per-file** rule and **`sealed` concrete classes** across all projects.
  - Zero modifications outside `specs/013-core-equipment-durability-and-sets/` during specification; implementation touches only the scoped files below.
  - Interactive NPC shop repair UI (`0x7A` `c2s_repairitem` packet handler & Blacksmith dialog script) belongs to Phase 2 `017-module-npc-shops`; `013` delivers the complete `RepairEquipmentItemZoneCommand`, `EquipmentRepairCalculator`, and `InventoryLootCommandHandler` execution path.

---

## Constitution Check (v1.4.0)

*GATE: Must pass before Phase 0 research and re-verified after Phase 1 design.*

| Principle | Status | Compliance Verification |
| :--- | :---: | :--- |
| **I. Hybrid Distributed Control Plane & Sharded 18-Hz Zone Architecture** | **PASS** | All abrasion rolls, broken-slot stat recalculations, set bonus evaluations, and repair command executions run single-threaded inside `MapZoneWorker` with zero DB/IO calls on the 18-Hz loop; `GoldRowId` and durability persist asynchronously via write-behind `UpdateCharacterWorldSnapshotDto`. |
| **II. Multi-Protocol Transport Abstraction & Evolutionary Client Compatibility** | **PASS** | `JXReborn.ZoneEngine` communicates durability changes via protocol-agnostic `IInventoryOutboundSink.SendItemDurabilitySync(uint runtimeItemId, int maxDurability, int currentDurability)` and `PlayerItemSyncSnapshot`; `0x9B` (`13B`) and Golden `0x5F` (`GoldRowId - 1`) wire rules stay isolated in `JXReborn.Adapter.Legacy`. |
| **III. Zero-Hallucination Evidence & Ground-Truth Fidelity** | **PASS** | Every constant, probability (`256`, `320`, `2560`, `5120`), wire offset (`0x9B` `<BIii`, `0x5F` `+0x06 = GoldRowId - 1`), repair price equation (`ItemPriceScale=50`, `MagicPriceScale=16`, `10*L^2*FixDur`), and Classic/Extended set rule matches legacy 6.0 `abraderate.ini`, `repairitem.lua`, and item sync wire specifications. |
| **IV. Zero-Dupe Economy & Lease-Fenced Session Integrity** | **PASS** | `RepairEquipmentItemZoneCommand` validates `LeaseEpoch` before mutating player gold or durability atomically on the single-threaded zone loop; Broken items stay in `Equip` rather than dropping to the ground when bags are full, preserving unique `DomainItemId` (`Guid`) identity. |
| **V. UTF-8 Domain Core, Dual-Tier Game Data Standard & Boundary-Only Transcoding** | **PASS** | Golden fixed affixes and set definitions load from canonical UTF-8 `data/items/*.items.json` (Tier 1) and compile into `MemoryPack` `artifacts/jxreborn-data.pack` with SHA-256 verification (Tier 2). |
| **VI. Clean Modular Boundaries, ABP DDD Role Separation & C# 14** | **PASS** | Strict inward dependency flow (`Primitives` → `GameAssets`/`ZoneEngine` → `Domain`/`Contracts`/`Application`/`Infrastructure` → `Adapter.Legacy`); 1 type per `.cs` file; `sealed` concrete classes; pluralized `Items/`, `Characters/`, `PlayerSessions/`, `Packets/` folders. |

---

## Project Structure

### Documentation (this feature)

```text
specs/013-core-equipment-durability-and-sets/
├── spec.md
├── checklists/
│   └── requirements.md
├── research.md
├── plan.md
├── data-model.md
├── contracts/
│   ├── durability-and-golden-item-protocol.md
│   └── equipment-abrasion-and-set-contracts.md
├── quickstart.md
└── tasks.md
```

### Source Code (repository root `the repository root`)

```text
src/
├── JXReborn.Primitives/
│   └── Items/
│       ├── EquipmentAbradeKind.cs                  # [NEW] enum : byte { Attack = 0, Defend = 1, Move = 2 }
│       └── EquipmentAbrasionConsts.cs              # [NEW] static class (256, 320, 2560, 5120, attr 38 & 31, repair scales 50 & 16)
│
├── JXReborn.Domain/
│   └── Characters/
│       └── CharacterItem.cs                        # [MOD] Add GoldRowId (ushort = 0), fix SetDurability/Repair IsBroken reset
│
├── JXReborn.Contracts/
│   └── Characters/
│       ├── CharacterItemDto.cs                     # [MOD] Add ushort GoldRowId = 0 parameter
│       └── UpdateCharacterWorldSnapshotDto.cs      # [MOD] Preserve GoldRowId + IsBroken in CharacterItemDto snapshot payload
│
├── JXReborn.Infrastructure/
│   └── EntityFrameworkCore/
│       └── JXRebornDbContext.cs                    # [MOD] Map CharacterItem.GoldRowId with default value 0
│
├── JXReborn.Application/
│   └── Characters/
│       └── CharacterAppService.cs                  # [MOD] Map GoldRowId & updated durability/IsBroken in ApplyWorldSnapshot & DTO mapping
│
├── JXReborn.GameAssets/
│   ├── Items/
│   │   ├── GoldenFixedAffixBindingTemplate.cs      # [NEW] [MemoryPackable] sealed partial record (6-slot Golden fixed affix bounds)
│   │   ├── EquipmentSuiteBindingTemplate.cs        # [NEW] [MemoryPackable] sealed partial record (SuiteId, ExtSuiteId, ExtSuitePieceIndex, ExtHiddenAffixIds)
│   │   ├── EquipmentSetTemplate.cs                 # [NEW] [MemoryPackable] sealed partial record (Classic & Extended set thresholds)
│   │   ├── ItemTemplate.cs                         # [MOD] Add RowId, FixedMagicAffixes, SuiteBinding
│   │   ├── IItemTemplateCatalog.cs                 # [MOD] Add EquipmentSet lookups & Classic suite threshold query
│   │   ├── ItemTemplateCatalog.cs                  # [MOD] Index Golden RowIds, ReBuildGoldEquipmentSuite (1..82), and EquipmentSets
│   │   └── ItemGenerator.cs                        # [MOD] Add GenerateGoldEquipment(goldRowId, randomSeedOverride) & Golden code support
│   └── Snapshots/
│       ├── GameAssetsSnapshotBundle.cs             # [MOD] Add IReadOnlyList<EquipmentSetTemplate>? EquipmentSets = null
│       └── GameAssetsSnapshotLoader.cs             # [MOD] Parse fixedMagicAffixes, suiteBinding, and equipmentSets from canonical JSON
│
├── JXReborn.ZoneEngine/
│   ├── Items/
│   │   ├── EquipmentAbrasionCalculator.cs          # [NEW] sealed/static calculator for legacy 6.0 Attack/Defend/Move abrasion rolls
│   │   ├── EquipmentRepairCalculator.cs            # [NEW] sealed/static calculator for Normal/Blue & Golden (Cheap/Costly) repair quotes
│   │   ├── RepairEquipmentItemZoneCommand.cs       # [NEW] sealed record : IZoneCommand (CharacterId, LeaseEpoch, RuntimeItemId, IsCheapGoldenRepair)
│   │   ├── PlayerItemSlot.cs                       # [MOD] Add GoldRowId, IsIndestructible, fix SetDurability IsBroken reset, add Repair()
│   │   ├── PlayerInventoryGrid.cs                  # [MOD] Classic suiteId + Extended extSuiteId set evaluation, broken suspension, vital clamping
│   │   └── InventoryLootCommandHandler.cs          # [MOD] Implement IZoneCommandHandler<RepairEquipmentItemZoneCommand>
│   ├── PlayerSessions/
│   │   ├── PlayerItemSyncSnapshot.cs               # [MOD] Add GoldRowId, WireMaxDurability (-1 indestructible), WireCurrentDurability
│   │   ├── IInventoryOutboundSink.cs               # [MOD] Add SendItemDurabilitySync(uint runtimeItemId, int maxDurability, int currentDurability)
│   │   ├── NoOpInventoryOutboundSink.cs            # [MOD] Add no-op SendItemDurabilitySync
│   │   └── SelectivePlayerOutboundSink.cs          # [MOD] Forward SendItemDurabilitySync to _inventorySink
│   └── WorldZones/
│       └── MapZoneWorker.cs                        # [MOD] Hook Attack/Defend/Move abrasion, 0x9B emission, broken stat/vital sync, HandleRepairEquipmentItem
│
└── JXReborn.Adapter.Legacy/
    ├── Packets/
    │   ├── ItemDurabilityChangeS2CPacket.cs        # [NEW] readonly record struct (0x9B, 13B <BIii serializer & parser)
    │   └── SyncItemS2CPacket.cs                    # [MOD] Encode DetailType = (short)(GoldRowId - 1) when Golden & -1 indestructible durability
    └── GameServer/
        └── GameServerClientSession.InventorySink.cs # [MOD] Implement SendItemDurabilitySync emitting encrypted 0x9B (13B)

tools/
└── JXReborn.DataExporter/
    └── Compilation/
        └── MemoryPackSnapshotCompiler.cs           # [MOD] Compile updated GameAssetsSnapshotBundle (EquipmentSets + Golden bindings) into jxreborn-data.pack

tests/
├── JXReborn.Protocol.Tests/
│   └── EquipmentDurabilityAndGoldenPacketTests.cs  # [NEW] Symmetric byte-exact tests for 0x9B (13B) & Golden/Indestructible 0x5F (80B)
├── JXReborn.ZoneEngine.Tests/
│   └── Items/
│       └── EquipmentDurabilityAndGoldenSetTests.cs # [NEW] ZoneSimulationHarness abrasion, broken vital clamp, Golden gen, Classic/Ext sets, repair
└── JXReborn.Integration.Tests/
    └── EquipmentDurabilityAndSetIntegrationTests.cs # [NEW] Live TCP GameServer 0x5F + 0x9B wire sync & EF Core persistence round-trip
```

**Structure Decision**: Extends the existing four-tier `JXReborn` modular architecture (`Primitives` → `GameAssets`/`ZoneEngine` → `Domain`/`Contracts`/`Application`/`Infrastructure` → `Adapter.Legacy`) without introducing new `.csproj` assemblies or generic technical folders. Every new type resides in its dedicated `.cs` file inside pluralized feature folders (`Items/`, `Characters/`, `Snapshots/`, `PlayerSessions/`, `Packets/`).

---

## Complexity Tracking

> No Constitution violations exist. Zero exceptions or structural waivers required.
