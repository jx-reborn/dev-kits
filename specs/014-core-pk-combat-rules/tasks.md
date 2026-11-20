# Tasks: Core PK & Combat Rules

**Input**: Design documents from `/specs/014-core-pk-combat-rules/`  
**Prerequisites**: [plan.md](plan.md) (required), [spec.md](spec.md) (required for user stories), [research.md](research.md), [data-model.md](data-model.md), [contracts/](contracts/)

**Tests**: Included across all 3 verification tiers (`JXReborn.Protocol.Tests`, `JXReborn.ZoneEngine.Tests`, `JXReborn.Integration.Tests`) per Constitution v1.4.0 and [quickstart.md](quickstart.md).

**Organization**: Tasks are grouped by user story (`US1`–`US5`) so each increment compiles cleanly (`-warnaserror`), adheres strictly to 1 type per `.cs` file and `sealed` concrete classes, and is independently testable.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no unresolved intra-phase dependencies)
- **[Story]**: Maps task to user story (`[US1]`..`[US5]`) in Phases 3–7
- Every implementation and test task specifies its exact repository-relative `.cs` file path

---

## Phase 1: Setup (Shared Primitives & Constants)

**Purpose**: Establish zero-dependency PK state enums and unit-explicit frame/percentage constants required by all downstream assemblies.

- [X] T001 [P] Create `PkEnmityState` byte enum (`Closed = 0`, `Countdown = 1`, `Active = 2`) in `src/JXReborn.Primitives/Combat/PkEnmityState.cs`
- [X] T002 [P] Add PK frame and threshold constants (`EnmityWarmupFrames = 200`, `EnmityActiveDurationFrames = 10800`, `MaxPkValue = 10`, `MinFightExpDebtPercent = -80`, `MinEnmityExpDebtPercent = -50`) in `src/JXReborn.Primitives/Combat/CombatConsts.cs`

---

## Phase 2: Foundational (Asset Pipeline, Core Session State, Sinks & Wire Packets)

**Purpose**: Core declarative `PkRules` asset compilation, transient zone session slots, transport-agnostic outbound sink abstractions, and symmetric Legacy 6.0 wire packet framing that MUST be complete before user story implementation begins.

**⚠️ CRITICAL**: No user story work can begin until this phase is complete.

### Declarative `PkRules` Asset Pipeline (`JXReborn.GameAssets` & `JXReborn.DataExporter`)

- [X] T003 [P] Create `[MemoryPackable]` `PkPenaltyRuleTemplate` record (`PkValue`, `ExpLossPerMil`, `MaxExpLoss`, `MoneyDropPercent`, `InventoryDropChecks`, `InventoryDropRatePercent`, `EquipDropRatePercent`, `EquipAbradePercent`) in `src/JXReborn.GameAssets/PlayerTemplates/PkPenaltyRuleTemplate.cs`
- [X] T004 Create `[MemoryPackable]` `PkRulesTemplate` record (including `HighLevelMaxExpLossByPkValue` Lv130+ cap array and canonical defaults) in `src/JXReborn.GameAssets/PlayerTemplates/PkRulesTemplate.cs`
- [X] T005 Update `IPlayerTemplateCatalog` with `PkRules` property and `GetPkPenaltyRule(int pkValue)` lookup in `src/JXReborn.GameAssets/PlayerTemplates/IPlayerTemplateCatalog.cs`
- [X] T006 Update `GameAssetsSnapshotBundle` to embed `PkRulesTemplate? PkRules` in `src/JXReborn.GameAssets/Snapshots/GameAssetsSnapshotBundle.cs`
- [X] T007 Update `GameAssetsSnapshotLoader` to parse `pkRules` from `data/players/progression.players.json` into `PkRulesTemplate` in `src/JXReborn.GameAssets/Snapshots/GameAssetsSnapshotLoader.cs`
- [X] T008 Update `PlayerTemplateCatalog` to materialize and clamp `0..10` `PkRulesTemplate` entries from snapshot or canonical Linux 6.0 defaults in `src/JXReborn.GameAssets/PlayerTemplates/PlayerTemplateCatalog.cs`
- [X] T009 Update `MemoryPackSnapshotCompiler` to serialize `PkRulesTemplate` into `artifacts/jxreborn-data.pack` and verify round-trip SHA-256 parity in `tools/JXReborn.DataExporter/Compilation/MemoryPackSnapshotCompiler.cs`

### Core Zone Session State & Transport-Agnostic Outbound Sinks (`JXReborn.ZoneEngine`)

- [X] T010 [P] Create `sealed class PlayerPkSessionState : IPlayerFeatureSlot` storing transient `EnmityState`, opponent `CharacterId`/`WorldNpcId`, `EnmityRemainingFrames`, `IsLauncher`, `IsExercising`, and `ExerciseTarget*` in `src/JXReborn.ZoneEngine/Combat/PlayerPkSessionState.cs`
- [X] T011 Update `PlayerZoneState` to separate `BaseCamp` from mutable `CurrentCamp` (`Camp` alias returning `CurrentCamp`), expose `PartyId`, and add `GetOrCreatePkSessionState()` helper in `src/JXReborn.ZoneEngine/PlayerSessions/PlayerZoneState.cs`
- [X] T012 [P] Extend `ICombatOutboundSink` with `SendPkValueSync`, `SendPkEnmityStateSync`, and `SendPkExerciseStateSync` in `src/JXReborn.ZoneEngine/PlayerSessions/ICombatOutboundSink.cs`
- [X] T013 [P] Extend `IWorldOutboundSink` with `SendNpcChangeCurCampBroadcast(uint worldNpcId, EntityCamp currentCamp)` in `src/JXReborn.ZoneEngine/PlayerSessions/IWorldOutboundSink.cs`
- [X] T014 [P] Implement no-op stubs for new PK combat sink methods in `src/JXReborn.ZoneEngine/PlayerSessions/NoOpCombatOutboundSink.cs`
- [X] T015 [P] Implement no-op stub for `SendNpcChangeCurCampBroadcast` in `src/JXReborn.ZoneEngine/PlayerSessions/NoOpWorldOutboundSink.cs`
- [X] T016 Align `IPlayerOutboundSink` aggregated interface surface (if needed) and forward `SendPkValueSync`, `SendPkEnmityStateSync`, `SendPkExerciseStateSync`, and `SendNpcChangeCurCampBroadcast` in `src/JXReborn.ZoneEngine/PlayerSessions/IPlayerOutboundSink.cs` and `src/JXReborn.ZoneEngine/PlayerSessions/SelectivePlayerOutboundSink.cs`

### Legacy 6.0 Wire Packet Structs, Framing Table & Tier-1 Protocol Tests (`JXReborn.Adapter.Legacy`)

- [X] T017 [P] Register `c2s_playerapplysetpk` (`0x54 => 2`) and `c2s_pkapplyenmity` (`0x77 => 5`) in `src/JXReborn.Adapter.Legacy/Framing/ProtocolSizeTable.cs` and update the `0x92` (`8 + N` bytes) doc comment in `src/JXReborn.Adapter.Legacy/Packets/ProtocolOpcodes.cs`
- [X] T018 [P] Create `PlayerApplySetPkC2SPacket` (`0x54`, `2B`) in `src/JXReborn.Adapter.Legacy/Packets/PlayerApplySetPkC2SPacket.cs`
- [X] T019 [P] Create `PkApplyEnmityC2SPacket` (`0x77`, `5B`, little-endian `TargetWorldNpcId`) in `src/JXReborn.Adapter.Legacy/Packets/PkApplyEnmityC2SPacket.cs`
- [X] T020 [P] Create `NpcChangeCurCampS2CPacket` (`0x58`, `6B`) in `src/JXReborn.Adapter.Legacy/Packets/NpcChangeCurCampS2CPacket.cs`
- [X] T021 [P] Create `PkSyncEnmityStateS2CPacket` (`0x91`, `8 + N` bytes with `PacketLength = TotalSize - 1` and `TCVN3` opponent name transcoding) in `src/JXReborn.Adapter.Legacy/Packets/PkSyncEnmityStateS2CPacket.cs`
- [X] T022 [P] Create `PkSyncExerciseStateS2CPacket` (`0x92`, `8 + N` bytes with `PacketLength = TotalSize - 1` and `TCVN3` opponent name transcoding) in `src/JXReborn.Adapter.Legacy/Packets/PkSyncExerciseStateS2CPacket.cs`
- [X] T023 [P] Create `PkSyncPkValueS2CPacket` (`0x93`, `5B`, signed `int32` `PkValue`) in `src/JXReborn.Adapter.Legacy/Packets/PkSyncPkValueS2CPacket.cs`
- [X] T024 Write symmetric `Read`/`Write` round-trip, `TCVN3` transcoding, and `ProtocolSizeTable` unit tests covering `0x54`, `0x76`, `0x77`, `0x58`, `0x90`, `0x91`, `0x92`, and `0x93` in `tests/JXReborn.Protocol.Tests/GameServer/PkWirePacketTests.cs`

**Checkpoint**: Foundation ready — `jxreborn-data.pack` compiles `PkRulesTemplate` and all 8 PK wire packets pass symmetric framing tests.

---

## Phase 3: User Story 1 - Normal PK Mode Transitions, Peace-Zone Cooldown Bypass & Camp Flag Toggling (Priority: P1) 🎯 MVP

**Goal**: Deliver authentic `Peace`/`Fight`/`Slaughter` mode switching (with `3,240`-frame combat-zone cooldown, immediate Peace-zone cooldown bypass, and `-80%` XP debt gate), initial world-entry `0x90`+`0x93` sync, and temporary `CurrentCamp` (`0x54` $\rightarrow$ `0x58`) toggling without mutating `BaseCamp`.

**Independent Test**: Verify via `ZoneSimulationHarness` and protocol sinks that a player in a combat zone waits `3,240` frames to return to `Peace`, immediately switches to `Peace` at frame `100` upon stepping into a `FightMode == 0` Peace Zone, is blocked from leaving `Peace` at `-80%` XP debt, receives `0x90` + `0x93` on world entry, and toggles `CurrentCamp = EntityCamp.Free (4)` $\leftrightarrow$ `BaseCamp` with `0x58` AOI broadcasts.

### Tests for User Story 1

- [X] T025 [P] [US1] Add deterministic tests for `3,240`-frame cooldown, Peace-Zone (`FightMode == 0`) immediate bypass, Novice/`-80%` XP debt rejection, and `SetTemporaryPkCampZoneCommand` (`BaseCamp` preservation + `0x58` broadcast) in `tests/JXReborn.ZoneEngine.Tests/Combat/PvpRelationMatrixAndPkStateTests.cs`

### Implementation for User Story 1

- [X] T026 [P] [US1] Create `SetTemporaryPkCampZoneCommand` (`CharacterId`, `ExpectedLeaseEpoch`, `EnableFreePkCamp`) in `src/JXReborn.ZoneEngine/Combat/SetTemporaryPkCampZoneCommand.cs`
- [X] T027 [P] [US1] Create `sealed class PlayerApplySetPkPacketHandler : IGameServerPacketHandler` (`0x54` $\rightarrow$ `SetTemporaryPkCampZoneCommand`) in `src/JXReborn.Adapter.Legacy/GameServer/PlayerApplySetPkPacketHandler.cs`
- [X] T028 [US1] Register `PlayerApplySetPkPacketHandler` in DI in `src/JXReborn.Adapter.Legacy/LegacyAdapterServiceCollectionExtensions.cs`
- [X] T029 [US1] Implement `IZoneCommandHandler<SetTemporaryPkCampZoneCommand>` on `CombatProgressionCommandHandler` and register `SetTemporaryPkCampZoneCommand` in `CoreZoneModule` in `src/JXReborn.ZoneEngine/Combat/CombatProgressionCommandHandler.cs` and `src/JXReborn.ZoneEngine/WorldZones/CoreZoneModule.cs`
- [X] T030 [US1] Update `MapZoneWorker.HandleChangePkMode` (allow immediate `Peace` switch when `FightMode == 0` bypassing `PkModeSwitchCooldownUntilFrame`, enforce `Experience > -80%` `MaxExp` gate when leaving `Peace`) and add `HandleSetTemporaryPkCamp` (`CurrentCamp = Free` vs `BaseCamp` + `3x3` AOI `SendNpcChangeCurCampBroadcast`) in `src/JXReborn.ZoneEngine/WorldZones/MapZoneWorker.cs`
- [X] T031 [US1] Update `src/JXReborn.Adapter.Legacy/GameServer/GameServerClientSession.WorldSink.cs` (emit `0x90` `PkSyncNormalFlagS2CPacket` and `0x93` `PkSyncPkValueS2CPacket` in `SendInitialWorldSyncBurst`; serialize `0x58` `NpcChangeCurCampS2CPacket` in `SendNpcChangeCurCampBroadcast`) and `src/JXReborn.Adapter.Legacy/GameServer/GameServerClientSession.CombatSink.cs` (serialize `0x93` in `SendPkValueSync`) in `src/JXReborn.Adapter.Legacy/GameServer/GameServerClientSession.WorldSink.cs` and `src/JXReborn.Adapter.Legacy/GameServer/GameServerClientSession.CombatSink.cs`

**Checkpoint**: User Story 1 is independently functional and verifiable via Tier-1 and Tier-2 tests.

---

## Phase 4: User Story 2 - Exhaustive PvP Hostility & Target Relation Matrix (Priority: P1)

**Goal**: Enforce the exact 9-step PvP relation precedence order (fixing the legacy bug where `Peace` blocked `Slaughter` and ensuring `Peace` victims can retaliate against `Slaughter` aggressors) across both immediate skill hits and delayed in-flight `MissileSimulationTickSystem` collisions.

**Independent Test**: Run parameterized matrix tests across all `Camp × PkMode × FightMode × Party × Enmity × Exercise` combinations for both direct damage and mid-flight projectiles.

### Tests for User Story 2

- [X] T032 [P] [US2] Add exhaustive parameterized truth-table tests (Peace Zone, Novice, Party `Ally`, Sparring `Enemy` vs third-party, Enmity `Countdown` vs `Active`, `Slaughter` vs `Peace` + self-defense retaliation, `Fight` camp rules) and mid-flight projectile posture-transition tests in `tests/JXReborn.ZoneEngine.Tests/Combat/PvpRelationMatrixAndPkStateTests.cs`

### Implementation for User Story 2

- [X] T033 [P] [US2] Create `readonly record struct PkRelationContext` (`CharacterId`, `PartyId`, `FightMode`, `EnmityState`, `EnmityTargetCharacterId`, `IsExercising`, `ExerciseTargetCharacterId`) in `src/JXReborn.ZoneEngine/Combat/PkRelationContext.cs`
- [X] T034 [US2] Implement the authoritative 11-step `EvaluateRelation(..., in PkRelationContext attackerPk, in PkRelationContext defenderPk)` overload (evaluating `Slaughter` before `Peace` and comparing `CurrentCamp`) in `src/JXReborn.ZoneEngine/Combat/CombatDamageCalculator.cs`
- [X] T035 [US2] Update `ActiveMissileSlot` to retain launcher `CharacterId` and snapshot `PkRelationContext` fallback for live player relation lookup in `src/JXReborn.ZoneEngine/Combat/ActiveMissileSlot.cs`
- [X] T036 [US2] Update `MissileSimulationTickSystem` and `MapZoneWorker.TryApplyPlayerDamageToPlayer` to build live `PkRelationContext` for attacker and defender on every direct hit, AoE check, and projectile collision in `src/JXReborn.ZoneEngine/Combat/MissileSimulationTickSystem.cs` and `src/JXReborn.ZoneEngine/WorldZones/MapZoneWorker.cs`

**Checkpoint**: User Stories 1 and 2 are complete; `Slaughter`, `Peace` retaliation, `Party`, `Camp`, `Enmity`, and `Exercise` evaluate identically on direct skills and projectiles.

---

## Phase 5: User Story 3 - Targeted Enmity Warmup, Active Duel & Automatic Termination (Priority: P1)

**Goal**: Implement `c2s_pkapplyenmity` (`0x77` $\rightarrow$ `0x91`), all 11 validation gates, the deterministic `200`-frame non-hostile warmup countdown (`PkEnmityState.Countdown`), the `10,800`-frame (`10`-minute) active duel (`PkEnmityState.Active`) with clean mutual `EnmityPKClose` on timeout, and immediate termination on death, Peace-Zone entry, map transfer, or disconnect.

**Independent Test**: Verify via `ZoneSimulationHarness` and loopback TCP bots that `0x77` enforces all 11 gates, remains non-hostile for `199` ticks, turns mutually `Enemy` on tick `200` (`0x91` `btState = 2`), cleanly closes Enmity on both players (`0x91` `btState = 0`) after `10,800` ticks, and immediately closes upon entering a Peace Zone or disconnecting.

### Tests for User Story 3

- [X] T037 [P] [US3] Add 18-Hz `ZoneSimulationHarness` tests for all 11 Enmity gates (`-50%` XP gate, Novice, Peace Zone, Sparring, already-in-Enmity), `200`-frame warmup non-hostility, `10,800`-frame clean dual-player Enmity timeout (`btState = 0`), and immediate `CloseAll` on Peace-Zone transition/detach in `tests/JXReborn.ZoneEngine.Tests/Combat/PvpRelationMatrixAndPkStateTests.cs`

### Implementation for User Story 3

- [X] T038 [P] [US3] Create `ApplyEnmityZoneCommand` (`LauncherCharacterId`, `ExpectedLeaseEpoch`, `TargetWorldNpcId`) in `src/JXReborn.ZoneEngine/Combat/ApplyEnmityZoneCommand.cs`
- [X] T039 [P] [US3] Create `sealed class PkApplyEnmityPacketHandler : IGameServerPacketHandler` (`0x77` $\rightarrow$ `ApplyEnmityZoneCommand`) in `src/JXReborn.Adapter.Legacy/GameServer/PkApplyEnmityPacketHandler.cs`
- [X] T040 [US3] Register `PkApplyEnmityPacketHandler` in DI in `src/JXReborn.Adapter.Legacy/LegacyAdapterServiceCollectionExtensions.cs`
- [X] T041 [US3] Create `sealed class PkStateZoneTickSystem : IZoneTickSystem` stepping `Countdown` (`200f` $\rightarrow$ `Active` `10,800f` + `SendPkEnmityStateSync(Active)`) and cleanly closing Enmity on both participants (`SendPkEnmityStateSync(Closed)`) at `0` frames or orphan partner missing in `src/JXReborn.ZoneEngine/Combat/PkStateZoneTickSystem.cs`
- [X] T042 [US3] Implement `IZoneCommandHandler<ApplyEnmityZoneCommand>` on `CombatProgressionCommandHandler` and register `ApplyEnmityZoneCommand` + `PkStateZoneTickSystem` in `CoreZoneModule` in `src/JXReborn.ZoneEngine/Combat/CombatProgressionCommandHandler.cs` and `src/JXReborn.ZoneEngine/WorldZones/CoreZoneModule.cs`
- [X] T043 [US3] Implement `MapZoneWorker.HandleApplyEnmity` (11 validation gates, mutual `Countdown` state, `SendPkEnmityStateSync(Countdown)`) and `CloseAllPkStates` hooks on region `FightMode == 0` transition, death, and `OnPlayerDetached` in `src/JXReborn.ZoneEngine/WorldZones/MapZoneWorker.cs`
- [X] T044 [US3] Implement `SendPkEnmityStateSync` (`0x91` `PkSyncEnmityStateS2CPacket`) in `src/JXReborn.Adapter.Legacy/GameServer/GameServerClientSession.CombatSink.cs`

**Checkpoint**: User Story 3 is complete; Enmity warmup, active duel, timeout bugfix, and automatic `CloseAll` triggers operate deterministically.

---

## Phase 6: User Story 4 - Penalty-Free 1v1 Sparring & Reference Arena Trigger (Priority: P2)

**Goal**: Deliver engine-first 1v1 Sparring (`Exercise` / `Tỷ Thí`) via `StartExerciseSparZoneCommand`, `IScriptDialogContext.StartExerciseSpar(...)`, and `ArenaSparringMasterDialogScript` (`0x92` sync, zero invented C2S opcodes, Enmity preemption, and zero death penalties on sparring defeat).

**Independent Test**: Trigger sparring between two same-camp `Peace` players via `StartExerciseSparZoneCommand` and `ArenaSparringMasterDialogScript`, deal lethal damage from one sparring partner to the other, and assert `0x92` `btState = 1` $\rightarrow$ `0`, `0` XP/money/item/durability loss, and `+0` `PkValue`.

### Tests for User Story 4

- [X] T045 [P] [US4] Add `ZoneSimulationHarness` tests for Sparring validation gates, active Enmity preemption (`0x91` `btState = 0` before `0x92` `btState = 1`), sparring partner exclusivity, and zero-penalty defeat (`enumDEATH_MODE_PLAYER_NO_PUNISH`) in `tests/JXReborn.ZoneEngine.Tests/Combat/PvpRelationMatrixAndPkStateTests.cs` and `tests/JXReborn.ZoneEngine.Tests/Combat/PvpDeathPenaltySimulationTests.cs`

### Implementation for User Story 4

- [X] T046 [P] [US4] Create `StartExerciseSparZoneCommand` (`InitiatorCharacterId`, `ExpectedLeaseEpoch`, `TargetCharacterId`) in `src/JXReborn.ZoneEngine/Combat/StartExerciseSparZoneCommand.cs`
- [X] T047 [P] [US4] Define/extend `IScriptDialogContext` with `bool StartExerciseSpar(Guid targetCharacterId)` engine-first helper contract in `src/JXReborn.ZoneEngine/Scripting/IScriptDialogContext.cs`
- [X] T048 [US4] Implement `IZoneCommandHandler<StartExerciseSparZoneCommand>` on `CombatProgressionCommandHandler` and register `StartExerciseSparZoneCommand` in `CoreZoneModule` in `src/JXReborn.ZoneEngine/Combat/CombatProgressionCommandHandler.cs` and `src/JXReborn.ZoneEngine/WorldZones/CoreZoneModule.cs`
- [X] T049 [US4] Implement `MapZoneWorker.HandleStartExerciseSpar` (validation gates, preemptive `CloseEnmityPair`, mutual `IsExercising = true`, `SendPkExerciseStateSync(true)`) and `enumDEATH_MODE_PLAYER_NO_PUNISH` sparring defeat handling in `src/JXReborn.ZoneEngine/WorldZones/MapZoneWorker.cs`
- [X] T050 [P] [US4] Implement `SendPkExerciseStateSync` (`0x92` `PkSyncExerciseStateS2CPacket`) in `src/JXReborn.Adapter.Legacy/GameServer/GameServerClientSession.CombatSink.cs`
- [X] T051 [US4] Implement `sealed class ArenaSparringMasterDialogScript` providing the reference 1v1 sparring challenge/accept NPC dialog flow using `IScriptDialogContext.StartExerciseSpar` in `src/JXReborn.Scripts/Pvp/ArenaSparringMasterDialogScript.cs`

**Checkpoint**: User Story 4 is complete; engine-first 1v1 Sparring works via both zone commands and the reference Arena NPC script with zero death penalties.

---

## Phase 7: User Story 5 - Slaughter PK Value Progression & Tiered PvP Death Penalties (Priority: P1)

**Goal**: Implement killer `PkValue` delta calculation (`0..10` cap: Sparring `0`, Enmity launcher `+2` / target `0`, `Slaughter` vs `Peace` `+1`, third-party `Slaughter` vs Sparring `+1` `ButcherPKExercise`, over-leveled `Fight` vs Lv<=50 `Peace` `+1`) and victim punishment (`0..10` XP deduction with Lv130+ `HighLevelMaxExpLossByPkValue` cap & `-80%` floor, inventory money loss with `moneyLost / 2` ground money spawn, bag/equipped item drop rolls, equipped durability percentage abrasion, and `-1` `PkValue` decay when XP `> -50%`).

**Independent Test**: Simulate PvP kills across `PkValue 0..10` (below and above Lv 130, and near `-50%`/`-80%` XP debt) in `PvpDeathPenaltySimulationTests` and full loopback TCP duels in `PvpEnmityAndSparringIntegrationTests`.

### Tests for User Story 5

- [X] T052 [P] [US5] Create `PvpDeathPenaltySimulationTests` covering all killer `PkValue` rules (`0..10` cap, Enmity `+2`/`0`, `ButcherPKExercise` `+1`), all `PkValue 0..10` penalty tiers, Lv130+ `HighLevelMaxExpLossByPkValue` caps, `-80%` XP floor, `moneyLost / 2` ground money drop, bag/equip drop rolls, equipped durability percentage abrasion, and `-50%` XP `-1` `PkValue` decay in `tests/JXReborn.ZoneEngine.Tests/Combat/PvpDeathPenaltySimulationTests.cs`
- [X] T053 [P] [US5] Create end-to-end loopback TCP integration suite verifying initial `0x90`/`0x93` sync, live encrypted Enmity duel + kill punishment + `0x64` ground money drop, and zero-penalty Arena Sparring duel in `tests/JXReborn.Integration.Tests/CombatAndPvp/PvpEnmityAndSparringIntegrationTests.cs`

### Implementation for User Story 5

- [X] T054 [US5] Create `static class PvpDeathPenaltyCalculator` implementing deterministic `CalculateKillerPkDelta` and `CalculateVictimPunishment` (XP loss with Lv130+ `HighLevelMaxExpLossByPkValue` cap and `-80%` floor, money loss & `moneyLost / 2` ground drop, deterministic bag/equip drop selection skipping Quest/locked items, equipped durability percentage abrasion math, and `-1` `PkValue` decay check) in `src/JXReborn.ZoneEngine/Combat/PvpDeathPenaltyCalculator.cs`
- [X] T055 [US5] Update `MapZoneWorker.HandlePlayerDeath` to track last PvP attacker `CharacterId`, distinguish Sparring (`NoPunish`) vs PvP (`PlayerPunish`), apply killer `PkValue` delta + `SendPkValueSync`, atomically deduct victim XP/money/items, abrade equipped durability (marking `IsBroken` at `0`), spawn `GroundObjectSlot` (`moneyLost / 2` + dropped items), close active PK states, and schedule dirty persistence snapshot in `src/JXReborn.ZoneEngine/WorldZones/MapZoneWorker.cs`

**Checkpoint**: All 5 User Stories (`US1`–`US5`) are complete and verified across protocol, 18-Hz simulation, and live loopback TCP integration suites.

---

## Phase 8: Polish & Cross-Cutting Verification

**Purpose**: High-performance structured observability, zero-allocation tick verification, and full repository regression validation.

- [X] T056 [P] Add source-generated `[LoggerMessage]` telemetry entries for Enmity open/close, Sparring open/close, `PkValue` transitions, and PvP death item/money drops in `src/JXReborn.ZoneEngine/Logging/ZoneEngineLogMessages.cs`
- [X] T057 [P] Add 100-concurrent-duel 18-Hz tick benchmark assertion (`< 1 ms` p99 step budget, zero per-tick managed allocations in `PkStateZoneTickSystem`) in `tests/JXReborn.ZoneEngine.Tests/Combat/PvpRelationMatrixAndPkStateTests.cs`
- [X] T058 Execute [quickstart.md](quickstart.md) verification gate (`dotnet build JXReborn.slnx -warnaserror`, `JXReborn.DataExporter`, and `dotnet test JXReborn.slnx --no-restore`) and verify `1 type per file` and `sealed` class compliance across all new files

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1 (Setup)**: No dependencies — starts immediately.
- **Phase 2 (Foundational)**: Depends on Phase 1 — **BLOCKS** all user stories (`US1`–`US5`).
- **Phase 3 (`US1` - Normal PK Modes & Camp Flag, P1)**: Depends on Phase 2. Establishes Peace-zone cooldown bypass, `-80%` XP gate, initial `0x90`/`0x93` sync, and `CurrentCamp` toggling.
- **Phase 4 (`US2` - PvP Hostility Matrix, P1)**: Depends on Phase 2 (and uses `CurrentCamp` from `T011`/`US1`). Can execute in parallel with `US1` packet/command wiring once `T011` is complete.
- **Phase 5 (`US3` - Targeted Enmity, P1)**: Depends on Phase 2 and `PkRelationContext` (`T033`–`T034` from `US2`) for `Active` Enmity combat resolution.
- **Phase 6 (`US4` - 1v1 Sparring, P2)**: Depends on Phase 2 and `US2`/`US3` (`CloseEnmityPair` preemption in `MapZoneWorker`).
- **Phase 7 (`US5` - Slaughter PK Value & Death Penalties, P1)**: Depends on Phase 2 and integrates cleanly with `US3` (Enmity `+2`/`0`) and `US4` (Sparring `NoPunish` & `ButcherPKExercise` `+1`).
- **Phase 8 (Polish)**: Depends on Phases 3–7.

### Parallel Opportunities

- **Phase 1**: `T001` and `T002` touch separate files and run in parallel.
- **Phase 2**:
  - `T003`, `T010`, `T012`–`T015`, and all packet definitions `T017`–`T023` touch independent files and can be authored concurrently.
- **User Story Phases (Phases 3–7)**:
  - Within each story, `[P]` test tasks, `*ZoneCommand.cs` records, and `*PacketHandler.cs` adapter classes can be written in parallel before converging on `MapZoneWorker.cs`.

---

## Parallel Example: Phase 2 Wire Packets & Sinks

```bash
# Run independent Phase 2 tasks concurrently:
Task T010: "Create PlayerPkSessionState in src/JXReborn.ZoneEngine/Combat/PlayerPkSessionState.cs"
Task T018: "Create PlayerApplySetPkC2SPacket in src/JXReborn.Adapter.Legacy/Packets/PlayerApplySetPkC2SPacket.cs"
Task T019: "Create PkApplyEnmityC2SPacket in src/JXReborn.Adapter.Legacy/Packets/PkApplyEnmityC2SPacket.cs"
Task T020: "Create NpcChangeCurCampS2CPacket in src/JXReborn.Adapter.Legacy/Packets/NpcChangeCurCampS2CPacket.cs"
Task T021: "Create PkSyncEnmityStateS2CPacket in src/JXReborn.Adapter.Legacy/Packets/PkSyncEnmityStateS2CPacket.cs"
Task T022: "Create PkSyncExerciseStateS2CPacket in src/JXReborn.Adapter.Legacy/Packets/PkSyncExerciseStateS2CPacket.cs"
Task T023: "Create PkSyncPkValueS2CPacket in src/JXReborn.Adapter.Legacy/Packets/PkSyncPkValueS2CPacket.cs"
```

---

## Implementation Strategy

### MVP First (User Stories 1 & 2)

1. Complete **Phase 1 (Setup)** and **Phase 2 (Foundational)**.
2. Complete **Phase 3 (`US1`)** and **Phase 4 (`US2`)**.
3. **STOP & VALIDATE**: Verify normal PK mode transitions, Peace-zone cooldown bypass, camp flag toggling, `Slaughter` vs `Peace` self-defense, and projectile relation accuracy.

### Incremental Full Delivery

1. Add **Phase 5 (`US3`)** $\rightarrow$ validate `200f` warmup, `10,800f` Enmity expiry bugfix, and automatic `CloseAll`.
2. Add **Phase 6 (`US4`)** $\rightarrow$ validate engine-first 1v1 Sparring and `ArenaSparringMasterDialogScript`.
3. Add **Phase 7 (`US5`)** + **Phase 8 (Polish)** $\rightarrow$ validate killer `PkValue` deltas, `PkPunish` `0..10` tiers, Lv130+ `HighLevelMaxExpLossByPkValue` caps, `moneyLost / 2` ground drops, and full loopback TCP integration suite.
