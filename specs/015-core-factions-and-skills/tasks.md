# Tasks: Factions, Skill Trees & Point Washing (`015-core-factions-and-skills`)

**Input**: Design documents from `/specs/015-core-factions-and-skills/` (`spec.md`, `plan.md`, `research.md`, `data-model.md`, `contracts/`, `quickstart.md`)  
**Prerequisites**: Spec `012` (Scripting Host: `IScriptDialogContext`, `IScriptItemContext`, `INpcDialogScript`, `IItemUsageScript`) and Spec `014` (`BaseCamp`/`CurrentCamp` & `0x58` runtime camp broadcast)

**Organization**: Tasks are grouped by user story (`US1`–`US5`) so each story can be implemented, tested, and verified independently while respecting strict 1-type-per-file and `sealed` concrete class rules from Constitution `v1.4.0`.

## Format: `[ID] [P?] [Story?] Description`

- **[P]**: Can execute in parallel (different files, no unmet in-phase dependencies)
- **[Story]**: Maps task to `US1`, `US2`, `US3`, `US4`, or `US5` in [spec.md](spec.md)
- Every implementation and test task specifies its exact file path relative to the repository root

---

## Phase 1: Setup (Shared Constants & Canonical Sect Asset Data)

**Purpose**: Establish shared faction/book/wash constants in `JXReborn.Primitives` and fix canonical sect `skillIds` and `factiontitle.txt` `titles` in `PlayerDataExporter` and `progression.players.json`.

- [X] T001 Add `ClassicFactionCount = 10`, level gates (`MinFactionJoinLevel = 10`, `MinFactionGraduateLevel = 60`, `MinMarrowWashLevel = 50`, `MinAdvancedBookLevel = 80`, `MinSkill120BookLevel = 120`), book caps (`MaxBonusBookUses = 15`, `XiSuiJingBonusPointsPerBook = 5`), and persistent task IDs (`WuLinMiJiTaskId = 80`, `XiSuiJingTaskId = 81`, `BonusBaseAttrTaskId = 88`, `Skill120BookTaskId = 2463`) in `src/JXReborn.Primitives/Characters/CharacterConsts.cs`
- [X] T002 [P] Update `ExportFactions` to emit canonical 10-sect base `skillIds` (`skills_table.lua`) and fix the 1-based `factiontitle.txt` `FACTION` column mapping (`1->0, 2->8, 3->4, 4->9, 5->2, 6->5, 7->6, 8->1, 9->3, 10->7`) in `tools/JXReborn.DataExporter/Exporters/PlayerDataExporter.cs`
- [X] T003 Populate all 10 classic sects' `skillIds` and corrected rank `titles` (preserving schema compliance against `schemas/player-template.schema.json`) in `data/players/progression.players.json`

---

## Phase 2: Foundational (Asset Catalogs, Persistence Snapshots, `LearnedSkillSlot` & Wire Framing)

**Purpose**: Core static catalog, Control-Plane snapshot persistence, in-zone `BaseLevel == 0` skill storage, and Legacy 6.0 wire packets required by all user stories.

**⚠️ CRITICAL**: Complete Phase 2 before starting User Story phases (`Phase 3`–`Phase 7`).

### Static Asset Catalog (`JXReborn.GameAssets` & `DataExporter`)

- [X] T004 [P] Create immutable `MemoryPackable` `FactionRankTitleTemplate` record in `src/JXReborn.GameAssets/PlayerTemplates/FactionRankTitleTemplate.cs`
- [X] T005 [P] Create immutable `MemoryPackable` `FactionTemplate` record (`Id`, `Code`, `Name`, `Series`, `Camp`, `AllowedGenders`, `SkillIds`, `Titles`) in `src/JXReborn.GameAssets/PlayerTemplates/FactionTemplate.cs`
- [X] T006 Extend `IPlayerTemplateCatalog` and `PlayerTemplateCatalog` with `AllFactions`, `TryGetFactionById`, `TryGetFactionByCode`, and `TryGetStarterTemplate` (`FrozenDictionary` backed) in `src/JXReborn.GameAssets/PlayerTemplates/IPlayerTemplateCatalog.cs` and `src/JXReborn.GameAssets/PlayerTemplates/PlayerTemplateCatalog.cs`
- [X] T007 Update `GameAssetsSnapshotBundle`, `GameAssetsSnapshotLoader`, and `MemoryPackSnapshotCompiler` to serialize and load `IReadOnlyList<FactionTemplate>` in `src/JXReborn.GameAssets/Snapshots/GameAssetsSnapshotBundle.cs`, `src/JXReborn.GameAssets/Snapshots/GameAssetsSnapshotLoader.cs`, and `tools/JXReborn.DataExporter/Compilation/MemoryPackSnapshotCompiler.cs`

### Control-Plane Snapshot & Domain Persistence (`Contracts`, `Domain`, `Application`)

- [X] T008 [P] Create `CharacterSkillSnapshotDto` (`SkillId`, `BaseLevel`, `ExpPercent`, `uint SkillExp = 0`) in `src/JXReborn.Contracts/Characters/CharacterSkillSnapshotDto.cs`
- [X] T009 Update `CharacterSkillDto` (`uint SkillExp = 0`) and `UpdateCharacterWorldSnapshotDto` (`CurrentFactionId`, `FirstFactionId`, `FactionJoinCount`, `LearnedSkills`) in `src/JXReborn.Contracts/Characters/CharacterSkillDto.cs` and `src/JXReborn.Contracts/Characters/UpdateCharacterWorldSnapshotDto.cs`
- [X] T010 Update `CharacterSkillValue` (allow `BaseLevel >= 0` and store `uint SkillExp`) and `Character.UpdateWorldSnapshot` (persist `CurrentFactionId`, `FirstFactionId`, `FactionJoinCount`, and level-`0` skills) in `src/JXReborn.Domain/Characters/CharacterSkillValue.cs` and `src/JXReborn.Domain/Characters/Character.cs`
- [X] T011 Update `CharacterAppService` (`ApplyWorldSnapshot` and character read DTO projection) to round-trip `CurrentFactionId`, `FirstFactionId`, `FactionJoinCount`, `BaseLevel == 0`, and `SkillExp` in `src/JXReborn.Application/Characters/CharacterAppService.cs`

### Zone Player Skill/Faction State & Outbound Sinks (`JXReborn.ZoneEngine`)

- [X] T012 [P] Create `LearnedSkillSlot` (`readonly record struct` with `int SkillId`, `ushort BaseLevel`, `uint SkillExp`) in `src/JXReborn.ZoneEngine/PlayerSessions/LearnedSkillSlot.cs`
- [X] T013 Update `PlayerZoneState` to store `FirstFactionId`, `FactionJoinCount`, computed `EffectiveFactionId`, and `Dictionary<int, LearnedSkillSlot>` permitting `BaseLevel == 0` in `src/JXReborn.ZoneEngine/PlayerSessions/PlayerZoneState.cs`
- [X] T014 [P] Add `SendPlayerFactionDataSync` (`0x7B`) and `SendNpcChangeBaseCampBroadcast` (`0x59`) to `IWorldOutboundSink`, `NoOpWorldOutboundSink`, and `SelectivePlayerOutboundSink` in `src/JXReborn.ZoneEngine/PlayerSessions/IWorldOutboundSink.cs`, `src/JXReborn.ZoneEngine/PlayerSessions/NoOpWorldOutboundSink.cs`, and `src/JXReborn.ZoneEngine/PlayerSessions/SelectivePlayerOutboundSink.cs`
- [X] T015 [P] Create `FactionProgressionResult` status/result type in `src/JXReborn.ZoneEngine/Factions/FactionProgressionResult.cs`

### Legacy 6.0 Wire Protocol Framing & Packet Fix (`JXReborn.Adapter.Legacy`)

- [X] T016 [P] Add `c2s_playerapplyfactiondata = 0x55`, `s2c_playerfactiondata = 0x7B`, fix `s2c_npcchgcamp = 0x59`, and register `table[0x55] = 1` in `src/JXReborn.Adapter.Legacy/Packets/ProtocolOpcodes.cs` and `src/JXReborn.Adapter.Legacy/Framing/ProtocolSizeTable.cs`
- [X] T017 [P] Create `PlayerApplyFactionDataC2SPacket` (`0x55`, `1`B), `PlayerFactionDataS2CPacket` (`0x7B`, `8`B), and `NpcChangeCampS2CPacket` (`0x59`, `6`B) in `src/JXReborn.Adapter.Legacy/Packets/PlayerApplyFactionDataC2SPacket.cs`, `src/JXReborn.Adapter.Legacy/Packets/PlayerFactionDataS2CPacket.cs`, and `src/JXReborn.Adapter.Legacy/Packets/NpcChangeCampS2CPacket.cs`
- [X] T018 [P] Fix swapped field layout in `PlayerSkillLevelS2CPacket` (`0x5E`, `15`B: `[9..12] int32 UnallocatedSkillPoints`, `[13..14] ushort SkillExpPercent`) and update its call sites in `src/JXReborn.Adapter.Legacy/Packets/PlayerSkillLevelS2CPacket.cs`, `src/JXReborn.Adapter.Legacy/GameServer/GameServerClientSession.CombatSink.cs`, and `tests/JXReborn.Protocol.Tests/GameServer/ProgressionAndRevivalPacketTests.cs`
- [X] T019 Wire `SendPlayerFactionDataSync` (`0x7B`) and `SendNpcChangeBaseCampBroadcast` (`0x59`) in `src/JXReborn.Adapter.Legacy/GameServer/GameServerClientSession.WorldSink.cs`
- [X] T020 [P] Author symmetric serialization, framing size, and hex test-vector unit tests for `0x55`, `0x7B`, `0x59`, `0x58`, `0x45`, `0x5D`, and `0x5E` in `tests/JXReborn.Protocol.Tests/GameServer/FactionAndSkillPacketTests.cs`

**Checkpoint**: Static sect templates, `BaseLevel == 0` persistence, and wire packets are verified and ready for User Story implementation.

---

## Phase 3: User Story 1 — Five-Elements & Gender-Gated Sect Induction & Base Skill Tree Grant (Priority: P1) 🎯 MVP

**Goal**: Allow eligible Level 10+ novices to join one of the 10 classic sects (`0..9`), enforce Five-Elements and gender gates, grant all base sect skills at `BaseLevel = 0`, synchronize `0x7B`/`0x45`/`0x59`/`0x58`, respond to `0x55` client refresh requests, and persist everything across relogs.

**Independent Test**: Run `ZoneSimulationHarness` across all `5 × 2 × 10 = 100` `(Series, Gender, FactionId)` combinations to verify exact induction acceptance/rejection, `BaseLevel = 0` skill grants, camp updates, `0x55 -> 0x7B` sync, and logout/re-attach persistence.

- [X] T021 [P] [US1] Create `JoinFactionZoneCommand` and `RequestFactionDataZoneCommand` in `src/JXReborn.ZoneEngine/Factions/JoinFactionZoneCommand.cs` and `src/JXReborn.ZoneEngine/Factions/RequestFactionDataZoneCommand.cs`
- [X] T022 [P] [US1] Create `sealed` `PlayerApplyFactionDataPacketHandler` dispatching `0x55` to `RequestFactionDataZoneCommand` in `src/JXReborn.Adapter.Legacy/GameServer/PlayerApplyFactionDataPacketHandler.cs`
- [X] T023 [US1] Implement `CanJoinFaction`, `TryJoinFaction`, and deterministic `DeriveCamp` (`Begin` vs. `Faction.Camp` vs. `Free`) in `sealed` `FactionProgressionManager` in `src/JXReborn.ZoneEngine/Factions/FactionProgressionManager.cs`
- [X] T024 [US1] Create `sealed` `FactionProgressionCommandHandler` handling `JoinFactionZoneCommand` and `RequestFactionDataZoneCommand` (`0x7B` unicast, `0x45` skill book sync, `0x59` + `0x58` 3x3 AOI camp broadcasts) in `src/JXReborn.ZoneEngine/Factions/FactionProgressionCommandHandler.cs`
- [X] T025 [US1] Update `MapZoneWorker` and `CoreZoneModule` to derive `BaseCamp`/`CurrentCamp` on attach, emit initial `0x7B` sync, include `BaseLevel == 0` skills in `0x45` and `BuildWorldSnapshotDto`, and dispatch faction commands in `src/JXReborn.ZoneEngine/WorldZones/MapZoneWorker.cs` and `src/JXReborn.ZoneEngine/WorldZones/CoreZoneModule.cs`
- [X] T026 [US1] Add `CurrentFactionId`, `FirstFactionId`, `EffectiveFactionId`, `FactionJoinCount`, `CanJoinFaction`, and `TryJoinFaction` to `IScriptDialogContext` (and zone implementation) in `src/JXReborn.ZoneEngine/Scripting/IScriptDialogContext.cs`
- [X] T027 [US1] Create `sealed` `SectMasterDialogScript` supporting induction prompts and elemental/gender/level feedback for all 10 classic sect masters in `src/JXReborn.Scripts/Factions/SectMasterDialogScript.cs`
- [X] T028 [US1] Add 100-combination `(Series, Gender, FactionId)` induction, `BaseLevel = 0` grant, and `0x55 -> 0x7B` tests in `tests/JXReborn.ZoneEngine.Tests/Factions/FactionJoinGraduateAndWashTests.cs`

**Checkpoint**: User Story 1 is complete and independently testable.

---

## Phase 4: User Story 2 — Sect Graduation, Camp Transitions & Persistent Faction Identity (Priority: P1)

**Goal**: Enable Level 60+ active sect disciples to graduate (*Xuất Sư*) into `Free` (`4`) camp while preserving `FirstFactionId`, `FactionJoinCount`, `EffectiveFactionId`, and learned sect skills, and allow subsequent re-induction into an eligible sect.

**Independent Test**: Verify Level 59 graduation rejection, Level 60 graduation (`CurrentFactionId = 0xFF`, `FirstFactionId` preserved, `BaseCamp = CurrentCamp = 4`), `0x59` + `0x58` AOI camp broadcasts, Free-camp restoration on relog, and sibling-sect re-induction (`FactionJoinCount = 2`).

- [X] T029 [P] [US2] Create `GraduateFactionZoneCommand` in `src/JXReborn.ZoneEngine/Factions/GraduateFactionZoneCommand.cs`
- [X] T030 [US2] Implement `TryGraduateFaction` (`Level >= 60`, `CurrentFactionId = 0xFF`, `FirstFactionId` & `FactionJoinCount` preserved, `BaseCamp = CurrentCamp = Free`) and re-induction preservation in `src/JXReborn.ZoneEngine/Factions/FactionProgressionManager.cs`
- [X] T031 [US2] Wire `GraduateFactionZoneCommand` handling (`0x7B` unicast + `0x59` & `0x58` AOI `Free` camp broadcasts) in `src/JXReborn.ZoneEngine/Factions/FactionProgressionCommandHandler.cs` and `src/JXReborn.ZoneEngine/WorldZones/MapZoneWorker.cs`
- [X] T032 [US2] Add `TryGraduateFaction` to `IScriptDialogContext` and add *Xuất Sư* graduation dialog flow to `SectMasterDialogScript` in `src/JXReborn.ZoneEngine/Scripting/IScriptDialogContext.cs` and `src/JXReborn.Scripts/Factions/SectMasterDialogScript.cs`
- [X] T033 [US2] Add Level 59/60 graduation, `Free` camp AOI broadcast, and sibling-sect re-induction tests in `tests/JXReborn.ZoneEngine.Tests/Factions/FactionJoinGraduateAndWashTests.cs`

**Checkpoint**: User Stories 1 and 2 work seamlessly together and preserve persistent faction heritage.

---

## Phase 5: User Story 3 — Level-Gated Skill Point Allocation & Proficiency Skill Protection (Priority: P1)

**Goal**: Allow `0 -> 1` and up-to-cap (`min(MaxLevel, Level - ReqLevel + 1)`) skill point investment on unlocked point-allocatable skills while blocking starter melee, protected utility, ungranted, and `IsExpSkill` proficiency skills.

**Independent Test**: Allocate points at Levels 10, 15, and 30 across tier-10/20/30 sect skills (`0 -> 1 -> cap`), confirm over-cap and starter/proficiency/ungranted allocation attempts are rejected with zero point loss, and verify aligned `0x5E` sync output.

- [X] T034 [US3] Implement `TryAllocateSkillPoint` (`0 -> 1` support, `LearnedSkills` check, `!IsExpSkill`, starter/utility skill lock `1, 2, 53, 210, 229..232, 400`, and `Math.Min(MaxLevel, Level - ReqLevel + 1)` cap) in `src/JXReborn.ZoneEngine/Factions/FactionProgressionManager.cs`
- [X] T035 [US3] Update `HandleAllocateSkillPoint` in `MapZoneWorker` to delegate to `FactionProgressionManager.TryAllocateSkillPoint`, recompute passive bonuses, and emit aligned `0x5E` `SendPlayerSkillLevelSync` in `src/JXReborn.ZoneEngine/WorldZones/MapZoneWorker.cs`
- [X] T036 [US3] Add `0 -> 1` unlock, `Level - ReqLevel + 1` cap boundary, ungranted skill rejection, and starter/proficiency lock unit tests in `tests/JXReborn.ZoneEngine.Tests/Factions/FactionJoinGraduateAndWashTests.cs`

**Checkpoint**: Full sect induction, graduation, and `0 -> 20` skill tree allocation loop is operational.

---

## Phase 6: User Story 4 — Attribute & Skill Point Reset with Bonus Point Conservation (Priority: P1)

**Goal**: Allow Level 50+ characters to wash base attributes, point-allocated skills, or both via the Marrow-Washing Monk (*Tẩy Tủy Đại Sư*), conserving 100% of scripture bonus points, clamping vitals safely, clearing washed active auras, and leaving starter/utility/proficiency skills untouched.

**Independent Test**: Execute 100 repeated `WashAttributes`, `WashSkills`, and `Both` cycles on characters with *Võ Lâm Mật Tịch* / *Tẩy Tủy Kinh* bonus points, `Task[88]` quest stat bonuses, active sect auras, and trained Level 90 proficiency skills; verify exact point conservation and vital clamping.

- [X] T037 [P] [US4] Create `WashPointsMode` enum (`AttributesOnly = 1`, `SkillsOnly = 2`, `Both = 3`) and `WashPlayerPointsZoneCommand` in `src/JXReborn.ZoneEngine/Factions/WashPointsMode.cs` and `src/JXReborn.ZoneEngine/Factions/WashPlayerPointsZoneCommand.cs`
- [X] T038 [US4] Implement `TryWashPoints` (`Level >= 50`, starter base + `Task[88]` attribute reset, equipment re-evaluation & vital clamping, non-protected `!IsExpSkill` reset to `BaseLevel = 0`, active aura reset) in `src/JXReborn.ZoneEngine/Factions/FactionProgressionManager.cs`
- [X] T039 [US4] Wire `WashPlayerPointsZoneCommand` in `FactionProgressionCommandHandler` and `MapZoneWorker` (`4x` `0x5D` attribute sync, vitals sync, `0x45` full skill sync, and `0x5E` remaining skill points sync) in `src/JXReborn.ZoneEngine/Factions/FactionProgressionCommandHandler.cs` and `src/JXReborn.ZoneEngine/WorldZones/MapZoneWorker.cs`
- [X] T040 [US4] Add `TryWashPoints` to `IScriptDialogContext` and create `sealed` `MarrowWashingMonkDialogScript` (*Tẩy Tủy Đại Sư*) in `src/JXReborn.ZoneEngine/Scripting/IScriptDialogContext.cs` and `src/JXReborn.Scripts/Factions/MarrowWashingMonkDialogScript.cs`
- [X] T041 [US4] Add Level 49 rejection, 100-cycle attribute/skill point conservation, `Task[88]` preservation, aura deactivation, vital clamping, and proficiency-skill preservation tests in `tests/JXReborn.ZoneEngine.Tests/Factions/FactionJoinGraduateAndWashTests.cs`

**Checkpoint**: All P1 stories (`US1`–`US4`) are complete and verified.

---

## Phase 7: User Story 5 — Advanced Sect Skill Books & Permanent Stat/Skill Scriptures (Priority: P2)

**Goal**: Support all 28 Level 90 sect skill books (`Particular 27..59`), the Level 120 sect skill book (`Particular 1125`), *Võ Lâm Mật Tịch* (`6, 1, 26` → `+1` skill point, max `15`), and *Tẩy Tủy Kinh* (`6, 1, 22` → flat `+5` attribute points at Level `80+`, max `15`) for both active disciples and graduated Free-camp martial artists.

**Independent Test**: Verify every single 90x book (`23` at `IniLevel = 1`, `5` at `IniLevel = 0`), all 10 120x sect mappings (`709, 708, 710..717` + `Task[2463]`), and `1..16` uses of *Võ Lâm Mật Tịch* (`Task[80] & 0xF`) and *Tẩy Tủy Kinh* (`Task[81] & 0xF`), confirming failed gates never consume inventory items.

- [X] T042 [US5] Implement `TryGrantBookSkill`, `AddBonusSkillPoints`, and `AddBonusAttributePoints` in `FactionProgressionManager` and expose them on `IScriptItemContext` in `src/JXReborn.ZoneEngine/Factions/FactionProgressionManager.cs` and `src/JXReborn.ZoneEngine/Scripting/IScriptItemContext.cs`
- [X] T043 [P] [US5] Create `sealed` `SkillBook90ItemScript` covering all 28 Level 90 sect books (`Level >= 80`, `EffectiveFactionId == NeedFaction`, `23` `IniLevel = 1` proficiency books + `5` `IniLevel = 0` point-allocated books) in `src/JXReborn.Scripts/Items/SkillBook90ItemScript.cs`
- [X] T044 [P] [US5] Create `sealed` `SkillBook120ItemScript` (`6, 1, 1125`, `Level >= 120`, `EffectiveFactionId in 0..9`, `Task[2463] == 0` -> `709, 708, 710..717` at `BaseLevel = 1`) in `src/JXReborn.Scripts/Items/SkillBook120ItemScript.cs`
- [X] T045 [P] [US5] Create `sealed` `WuLinMiJiItemScript` (`6, 1, 26`, `Level >= 80`, `(Task[80] & 0xF) < 15` -> `+1` skill point, upper bits preserved) in `src/JXReborn.Scripts/Items/WuLinMiJiItemScript.cs`
- [X] T046 [P] [US5] Create `sealed` `XiSuiJingItemScript` (`6, 1, 22`, `Level >= 80`, `(Task[81] & 0xF) < 15` -> flat `+5` attribute points, upper bits preserved) in `src/JXReborn.Scripts/Items/XiSuiJingItemScript.cs`
- [X] T047 [US5] Author exhaustive unit tests for all 28 90x books, all 10 120x mappings (including graduated disciples), 15-book nibble caps, upper-bit preservation, and item retention on failure in `tests/JXReborn.ZoneEngine.Tests/Factions/SkillBooksAndScripturesTests.cs`

**Checkpoint**: All 5 User Stories (`US1`–`US5`) are complete and verified.

---

## Phase 8: Polish & Cross-Cutting Verification

**Purpose**: End-to-end persistence + headless TCP bot integration verification, `players.pack` snapshot compilation check, and zero-warning build gate.

- [X] T048 Author end-to-end snapshot persistence round-trip (`BaseLevel == 0`, `uint SkillExp`, `CurrentFactionId`/`FirstFactionId`/`FactionJoinCount`, `Free` camp) and headless 2-bot TCP `0x55`/`0x7B`/`0x59`/`0x58`/`0x5E` integration tests in `tests/JXReborn.Integration.Tests/FactionsAndScripting/FactionProgressionAndWashIntegrationTests.cs`
- [X] T049 Run `JXReborn.DataExporter` snapshot compilation, validate `data/players/progression.players.json` against `schemas/player-template.schema.json`, and execute all 3 verification test suites in [quickstart.md](quickstart.md) with zero compiler warnings (`dotnet build JXReborn.slnx -c Release && dotnet test JXReborn.slnx`)

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1 (Setup)**: Starts immediately (`T001`–`T003`).
- **Phase 2 (Foundational)**: Depends on Phase 1; blocks all User Story phases (`T004`–`T020`).
- **Phase 3 (`US1` — Sect Induction)**: Depends on Phase 2 (`T021`–`T028`).
- **Phase 4 (`US2` — Sect Graduation)**: Depends on `US1` (`FactionProgressionManager` & `SectMasterDialogScript`) (`T029`–`T033`).
- **Phase 5 (`US3` — Skill Point Allocation)**: Depends on Phase 2 and `US1` (`BaseLevel = 0` sect skill grants) (`T034`–`T036`). Can run in parallel with `US2`.
- **Phase 6 (`US4` — Point Washing)**: Depends on `US1` and `US3` (`T037`–`T041`).
- **Phase 7 (`US5` — Skill Books & Scriptures)**: Depends on `US1` (`EffectiveFactionId` & `FactionProgressionManager`) (`T042`–`T047`). Can run in parallel with `US3`/`US4` once `T042` lands.
- **Phase 8 (Polish & Integration)**: Depends on Phases 3–7 (`T048`–`T049`).

### Parallel Opportunities

- **Phase 2**: `T004` + `T005` (asset records), `T008` (DTO), `T012` + `T014` + `T015` (zone types/sinks), and `T016` + `T017` + `T018` + `T020` (Legacy packets & protocol tests) touch disjoint projects and can run in parallel.
- **Phase 7 (`US5` Item Scripts)**: Once `T042` (`IScriptItemContext` helpers) is complete, all four C# 14 item scripts (`T043` `SkillBook90ItemScript`, `T044` `SkillBook120ItemScript`, `T045` `WuLinMiJiItemScript`, `T046` `XiSuiJingItemScript`) reside in separate files and can be authored concurrently.

---

## Parallel Example: User Story 5 (`Phase 7`)

```bash
# After T042 completes, launch all four book/scripture item scripts concurrently:
Task T043: "src/JXReborn.Scripts/Items/SkillBook90ItemScript.cs"
Task T044: "src/JXReborn.Scripts/Items/SkillBook120ItemScript.cs"
Task T045: "src/JXReborn.Scripts/Items/WuLinMiJiItemScript.cs"
Task T046: "src/JXReborn.Scripts/Items/XiSuiJingItemScript.cs"
```

---

## Implementation Strategy

### MVP First (`Phase 1` + `Phase 2` + `Phase 3 [US1]` + `Phase 5 [US3]`)

1. Complete **Phase 1** & **Phase 2** to unlock `FactionTemplate`, `LearnedSkillSlot` (`BaseLevel == 0`), faction/skill snapshot persistence, and aligned `0x55`/`0x7B`/`0x59`/`0x5E` packets.
2. Complete **Phase 3 (`US1`)** & **Phase 5 (`US3`)** so players can join all 10 classic sects and allocate skill points (`0 -> 20`).
3. Validate persistence and wire sync before layering graduation (`US2`), Marrow-Washing (`US4`), and endgame 90x/120x books & scriptures (`US5`).
