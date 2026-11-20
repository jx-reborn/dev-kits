# Tasks: Gameplay Scripting Host & Interactive Dialog System (`012`)

**Input**: Design documents from `/specs/012-core-csharp-scripting-host/`  
**Prerequisites**: [plan.md](plan.md), [spec.md](spec.md), [research.md](research.md), [data-model.md](data-model.md), [contracts/](contracts/), [quickstart.md](quickstart.md)  
**Tests**: Included across all 3 Constitution verification tiers (`JXReborn.Protocol.Tests`, `JXReborn.ZoneEngine.Tests`, and `JXReborn.Integration.Tests`).

**Organization**: Tasks are grouped by foundational dependencies and user stories (`US1`–`US5`) so each story can be implemented and verified independently under strict `1 type per file`, `sealed` concrete classes, and `TreatWarningsAsErrors=true`.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no intra-phase ordering conflict)
- **[Story]**: Maps task to `US1`–`US5` in [spec.md](spec.md)
- Exact repository-relative paths are provided in every task description.

---

## Phase 1: Setup (Project Scaffolding & Solution Wiring)

**Purpose**: Scaffold the `JXReborn.Scripts` class library and wire inward project references without altering existing runtime behavior.

- [X] T001 Create `src/JXReborn.Scripts/JXReborn.Scripts.csproj` targeting `net10.0` with project references to `JXReborn.Primitives`, `JXReborn.GameAssets`, and `JXReborn.ZoneEngine`
- [X] T002 Register `src/JXReborn.Scripts/JXReborn.Scripts.csproj` inside `JXReborn.slnx` under `/src/` and add its project reference to `src/JXReborn.Host/JXReborn.Host.csproj` and `tests/JXReborn.ZoneEngine.Tests/JXReborn.ZoneEngine.Tests.csproj`
- [X] T003 [P] Create `src/JXReborn.Primitives/Scripting/ScriptingConsts.cs` defining `MaxDialogOptions = 50`, `MaxScriptActionContentBytes = 900`, and `DefaultDialogRadiusCells = 4`
- [X] T004 [P] Create source-generated structured logging helpers in `src/JXReborn.Scripts/Logging/ScriptsLogMessages.cs`

---

## Phase 2: Foundational (Static Asset Templates & Non-Collectible Zone Contracts)

**Purpose**: Establish non-collectible scripting interfaces in `JXReborn.ZoneEngine`, static asset schema/template extensions in `JXReborn.GameAssets`, and the core `ZoneScriptHost` registry before wiring user-story handlers.

**⚠️ CRITICAL**: Complete this phase before beginning Phases 3–7.

### Static Assets & Primitives Alignment

- [X] T005 [P] Align `ItemGenreKind` (`QuestKey = 4`, `MagicScript = 6` with legacy importer compatibility) in `src/JXReborn.Primitives/Items/ItemGenreKind.cs`
- [X] T006 [P] Add `CustomScript = 3` to `PortalTrapActionKind` in `src/JXReborn.Primitives/WorldZones/PortalTrapActionKind.cs`
- [X] T007 [P] Create `[MemoryPackable]` `ItemScriptUsageTemplate` record in `src/JXReborn.GameAssets/Items/ItemScriptUsageTemplate.cs` and add optional `ItemScriptUsageTemplate? ScriptUsage = null` to `src/JXReborn.GameAssets/Items/ItemTemplate.cs`
- [X] T008 [P] Add optional `string? ScriptCode = null` and `byte DialogRadiusCells = 4` to `src/JXReborn.GameAssets/NpcTemplates/StaticNpcSpawnTemplate.cs` and `src/JXReborn.ZoneEngine/NpcEntities/NpcZoneState.cs`
- [X] T009 [P] Add optional `string? ScriptCode = null` to `src/JXReborn.GameAssets/WorldMaps/PortalTrapBindingTemplate.cs`
- [X] T010 Update `schemas/item-catalog.schema.json`, `schemas/map.schema.json`, `src/JXReborn.GameAssets/Snapshots/GameAssetsSnapshotBundle.cs`, `src/JXReborn.GameAssets/Snapshots/GameAssetsSnapshotLoader.cs`, and `tools/JXReborn.DataExporter/Compilation/MemoryPackSnapshotCompiler.cs` to validate, compile, and hydrate `ScriptUsage` and `ScriptCode` bindings

### Non-Collectible Scripting Contracts & Outbound Sink (`JXReborn.ZoneEngine`)

- [X] T011 [P] Create `ItemUsageResult` enum (`KeepItem = 0`, `ConsumeOne = 1`) in `src/JXReborn.ZoneEngine/Scripting/ItemUsageResult.cs` and `PendingDialogKind` enum in `src/JXReborn.ZoneEngine/Scripting/PendingDialogKind.cs`
- [X] T012 [P] Create `DialogOption` readonly record struct in `src/JXReborn.ZoneEngine/Scripting/DialogOption.cs`
- [X] T013 [P] Create `IScriptDialogContext` interface in `src/JXReborn.ZoneEngine/Scripting/IScriptDialogContext.cs`
- [X] T014 [P] Create `IScriptItemContext` in `src/JXReborn.ZoneEngine/Scripting/IScriptItemContext.cs`, `IScriptTrapContext` in `src/JXReborn.ZoneEngine/Scripting/IScriptTrapContext.cs`, and `IMissionContext` in `src/JXReborn.ZoneEngine/Scripting/IMissionContext.cs`
- [X] T015 [P] Create the four entry-point script interfaces: `INpcDialogScript` (`src/JXReborn.ZoneEngine/Scripting/INpcDialogScript.cs`), `IItemUsageScript` (`src/JXReborn.ZoneEngine/Scripting/IItemUsageScript.cs`), `ITrapScript` (`src/JXReborn.ZoneEngine/Scripting/ITrapScript.cs`), and `IMissionController` (`src/JXReborn.ZoneEngine/Scripting/IMissionController.cs`)
- [X] T016 Create `IZoneScriptHost` interface in `src/JXReborn.ZoneEngine/Scripting/IZoneScriptHost.cs` and expose `IZoneScriptHost? ScriptHost { get; init; }` on `src/JXReborn.ZoneEngine/WorldZones/ZoneAssetServices.cs`
- [X] T017 Create `IDialogOutboundSink` in `src/JXReborn.ZoneEngine/PlayerSessions/IDialogOutboundSink.cs` and `NoOpDialogOutboundSink` in `src/JXReborn.ZoneEngine/PlayerSessions/NoOpDialogOutboundSink.cs`, inherit `IDialogOutboundSink` on `src/JXReborn.ZoneEngine/PlayerSessions/IPlayerOutboundSink.cs`, and add `DialogSink` to `src/JXReborn.ZoneEngine/PlayerSessions/PlayerZoneState.cs`
- [X] T018 Create `ScriptRegistrySnapshot` (`src/JXReborn.Scripts/Hosting/ScriptRegistrySnapshot.cs`), `ZoneScriptHost` (`src/JXReborn.Scripts/Hosting/ZoneScriptHost.cs`), `ScriptHostOptions` (`src/JXReborn.Scripts/Hosting/ScriptHostOptions.cs`), and `ScriptsServiceCollectionExtensions` (`src/JXReborn.Scripts/Hosting/ScriptsServiceCollectionExtensions.cs`)

**Checkpoint**: Non-collectible interfaces, static asset bindings, outbound dialog sink, and `ZoneScriptHost` compile cleanly with zero warnings.

---

## Phase 3: User Story 1 - Interactive NPC Dialogs & Multi-Step Option Menus (Priority: P1) 🎯 MVP

**Goal**: Enable players within `256` Mps of a scripted NPC to send `0x6E`, receive `0x63` (`Say` / `Describe`) option menus, navigate arbitrarily deep nested sub-menus via `0x5F` (`17` bytes) without callback overwrite, and travel across towns via *Xa Phu* (`TownTeleporterDialogScript`).

**Independent Test**: Run `ScriptingDialogPacketTests` and `ScriptingZoneHostTests` verifying 9B `0x6E`, 17B `0x5F`, `0x63` TCVN3 `Say`/`Describe` encoding, Euclidean proximity rejection/acceptance, cancel handling (`SelectIndex = -1`), and 3-step *Xa Phu* confirmation + gold deduction + teleport.

### Wire Protocol & Adapter (`JXReborn.Adapter.Legacy`)

- [X] T019 [US1] Add `ProtocolOpcodes.c2s_playerselui => payload.Length == 17` to `src/JXReborn.Adapter.Legacy/Framing/ProtocolSizeTable.cs`
- [X] T020 [P] [US1] Implement `DialogNpcC2SPacket` (`0x6E`, `9` bytes `<BII`) in `src/JXReborn.Adapter.Legacy/Packets/DialogNpcC2SPacket.cs`
- [X] T021 [P] [US1] Implement `PlayerSelectUiC2SPacket` (`0x5F`, `17` bytes `<Biiii`) in `src/JXReborn.Adapter.Legacy/Packets/PlayerSelectUiC2SPacket.cs`
- [X] T022 [P] [US1] Implement `ScriptActionS2CPacket` (`0x63`, `16 + len` bytes) supporting null-terminated TCVN3 `Say` (`OperateType = 0`, `UiId = 0`, `Param = -1`) and `Describe` (`OperateType = 2`, `UiId = 0`, `Param = -1`) in `src/JXReborn.Adapter.Legacy/Packets/ScriptActionS2CPacket.cs`
- [X] T023 [US1] Implement `IDialogOutboundSink` wire emission (`0x63`, plus thin `0x84` `SendOpenSaleBox` and `0x88` `SendOpenStoreBox` frames) in `src/JXReborn.Adapter.Legacy/GameServer/GameServerClientSession.DialogSink.cs`

### Zone Commands, Feature Slot, Fluent Context & Reference *Xa Phu* Script

- [X] T024 [P] [US1] Create `DialogNpcZoneCommand` in `src/JXReborn.ZoneEngine/Scripting/DialogNpcZoneCommand.cs` and `SelectDialogUiZoneCommand` in `src/JXReborn.ZoneEngine/Scripting/SelectDialogUiZoneCommand.cs`
- [X] T025 [US1] Implement `PlayerDialogFeatureSlot` (`IPlayerFeatureSlot`) managing server-only `_optionCallbacks` (`<= 50`), pre-callback state reset, and `OnPlayerDetached` cleanup in `src/JXReborn.ZoneEngine/Scripting/PlayerDialogFeatureSlot.cs`
- [X] T026 [P] [US1] Implement `DialogNpcPacketHandler` (`0x6E` with `0x506C` `CheckValue` check) in `src/JXReborn.Adapter.Legacy/GameServer/DialogNpcPacketHandler.cs` and `PlayerSelectUiPacketHandler` (`0x5F`) in `src/JXReborn.Adapter.Legacy/GameServer/PlayerSelectUiPacketHandler.cs`
- [X] T027 [US1] Implement `ZoneScriptDialogContext` (`Say`, `Describe`, `SendSystemMessage`, currency/bag/teleport helpers, `OpenShop`, `OpenStoreBox`) in `src/JXReborn.Scripts/Dialogs/ZoneScriptDialogContext.cs` and `FluentDialogBuilder` in `src/JXReborn.Scripts/Dialogs/FluentDialogBuilder.cs`
- [X] T028 [US1] Implement `ScriptingCommandHandler` enforcing lease epoch, non-dead/non-trading/AOI checks, Euclidean `distanceMps <= (DialogRadiusCells * 32) * 2`, and pre-callback state clearing in `src/JXReborn.ZoneEngine/Scripting/ScriptingCommandHandler.cs`
- [X] T029 [US1] Implement `ScriptingZoneModule` (`IZoneModule`) registering `DialogNpcZoneCommand`, `SelectDialogUiZoneCommand`, and `PlayerDialogFeatureSlot` in `src/JXReborn.ZoneEngine/Scripting/ScriptingZoneModule.cs`
- [X] T030 [US1] Implement the production reference *Xa Phu* script `TownTeleporterDialogScript` (`ScriptCode = "npc-town-teleporter"`) with nested confirmation sub-menus, 500-gold fare check, and cross-town relocation in `src/JXReborn.Scripts/Dialogs/TownTeleporterDialogScript.cs`
- [X] T031 [US1] Author symmetric packet round-trip and hex-vector tests for `0x6E`, `0x5F`, `0x63` (`Say`/`Describe`), and `ProtocolSizeTable` (17B `0x5F`) in `tests/JXReborn.Protocol.Tests/GameServer/ScriptingDialogPacketTests.cs` and nested sub-menu + proximity/cancel tests in `tests/JXReborn.ZoneEngine.Tests/Scripting/ScriptingZoneHostTests.cs`

**Checkpoint**: User Story 1 (`US1`) is independently functional and tested end-to-end in `Protocol.Tests` and `ZoneEngine.Tests`.

---

## Phase 4: User Story 2 - Multi-Page Narrative Monologues & Completion Callbacks (Priority: P1)

**Goal**: Enable multi-page sequential story monologues (`Talk`) separated by Linux 6.0 `"| |"` (`0x7C 0x20 0x7C`) delimiters with optional server-side completion continuations (`m_nParam = 1` vs `0`).

**Independent Test**: Trigger both fire-and-forget (`onCompleted: null`) and continuation-backed (`onCompleted != null`) multi-page `Talk` sequences in `ScriptingDialogPacketTests` and `ScriptingZoneHostTests`, verifying `"| |"` wire framing, single-fire continuation execution on `0x5F` (`SelectIndex = 0`), and detach cleanup.

- [X] T032 [US2] Extend `ScriptActionS2CPacket` in `src/JXReborn.Adapter.Legacy/Packets/ScriptActionS2CPacket.cs` and `src/JXReborn.Adapter.Legacy/GameServer/GameServerClientSession.DialogSink.cs` to serialize/parse `Talk` mode (`OperateType = 0`, `UiId = 2`, `Param = expectCompletionCallback ? 1 : 0`, `"| |"` page delimiter)
- [X] T033 [US2] Wire `WaitingTalkCompletion` state registration and pre-callback clearing in `src/JXReborn.ZoneEngine/Scripting/PlayerDialogFeatureSlot.cs`, `src/JXReborn.ZoneEngine/Scripting/ScriptingCommandHandler.cs`, `src/JXReborn.Scripts/Dialogs/ZoneScriptDialogContext.cs`, and `src/JXReborn.Scripts/Dialogs/FluentDialogBuilder.cs`
- [X] T034 [US2] Add `Talk` hex-vector tests (`Vector 3C`) in `tests/JXReborn.Protocol.Tests/GameServer/ScriptingDialogPacketTests.cs` and multi-page monologue continuation/cancel/detach tests in `tests/JXReborn.ZoneEngine.Tests/Scripting/ScriptingZoneHostTests.cs`

**Checkpoint**: User Stories 1 and 2 (`Say`, `Describe`, `Talk`, nested callbacks) pass all unit and simulation tests.

---

## Phase 5: User Story 3 - Scripted Item Activation & Persistent Quest State Variables (Priority: P1)

**Goal**: Support `ItemGenreKind.MagicScript` (`6`) usable items via `0x59` (`TargetId` forwarded), explicit `ItemUsageResult` (`ConsumeOne` vs. `KeepItem`), `CastSkillId` fallback, persistent `TaskValues` (`0..65535`), and session-volatile `TempTaskValues` (`0..65535`).

**Independent Test**: Activate a scripted item with insufficient bag space (`KeepItem`), free space and re-activate (`ConsumeOne` + persistent `TaskValue` set), verify `TempTaskValue` wipes on detach while `TaskValue` survives snapshot export, and verify `CastSkillId` fallback when `ScriptCode` is empty.

- [X] T035 [P] [US3] Add optional `uint TargetNpcId = 0` to `src/JXReborn.ZoneEngine/Items/UseInventoryItemZoneCommand.cs` and forward `packet.TargetId` in `src/JXReborn.Adapter.Legacy/GameServer/PlayerEatItemPacketHandler.cs`
- [X] T036 [P] [US3] Implement `GetTask`/`SetTask` (backed by `PlayerZoneState`) and `GetTempTask`/`SetTempTask` (backed by `PlayerDialogFeatureSlot` and cleared in `OnPlayerDetached`) in `src/JXReborn.Scripts/Dialogs/ZoneScriptDialogContext.cs`
- [X] T037 [US3] Implement `ZoneScriptItemContext` (`IScriptItemContext`) in `src/JXReborn.Scripts/Items/ZoneScriptItemContext.cs`
- [X] T038 [US3] Update `HandleUseInventoryItemAsync` in `src/JXReborn.ZoneEngine/WorldZones/MapZoneWorker.cs` to dispatch `ItemGenreKind.MagicScript` items to `IZoneScriptHost.TryGetItemUsageScript`, honor `ItemUsageResult.ConsumeOne` vs `KeepItem`, and execute `ScriptUsage.CastSkillId` fallback when no `ScriptCode` is bound
- [X] T039 [US3] Add `0x59` `TargetId` packet verification in `tests/JXReborn.Protocol.Tests/GameServer/ScriptingDialogPacketTests.cs` and `MagicScript` `ConsumeOne`/`KeepItem`, `CastSkillId` fallback, and `TaskValues` vs `TempTaskValues` lifecycle tests in `tests/JXReborn.ZoneEngine.Tests/Scripting/ScriptingZoneHostTests.cs`

**Checkpoint**: User Stories 1, 2, and 3 pass all deterministic unit tests.

---

## Phase 6: User Story 4 - Dynamic Map Trap Triggers & Zone Mission Lifecycle Hooks (Priority: P2)

**Goal**: Dispatch custom map cell traps (`PortalTrapActionKind.CustomScript`) to `ITrapScript` without duplicate re-firing on the same trap cell group, and step active per-zone `IMissionController` instances deterministically on the 18-Hz tick loop.

**Independent Test**: Step a simulated player onto a `CustomScript` trap cell (verifying bounce-back/passage and single-fire `LastTriggeredTrapId` guard), and advance a map zone running an `IMissionController` through start, periodic timer ticks, participant join/detach, and stop.

- [X] T040 [P] [US4] Implement `ZoneScriptTrapContext` (`IScriptTrapContext`) in `src/JXReborn.Scripts/Traps/ZoneScriptTrapContext.cs`
- [X] T041 [US4] Extend `EvaluatePlayerTrapAsync` in `src/JXReborn.ZoneEngine/WorldZones/MapZoneWorker.cs` to handle `PortalTrapActionKind.CustomScript` by resolving `ITrapScript` from `IZoneScriptHost` and invoking `OnStepTrapAsync`
- [X] T042 [P] [US4] Implement `ActiveZoneMissionSlot` in `src/JXReborn.Scripts/Missions/ActiveZoneMissionSlot.cs` and `ZoneMissionContext` (`IMissionContext`) in `src/JXReborn.Scripts/Missions/ZoneMissionContext.cs`
- [X] T043 [US4] Implement `MissionZoneTickSystem` (`IZoneTickSystem` at `(ZoneTickPhase)550`) in `src/JXReborn.ZoneEngine/Scripting/MissionZoneTickSystem.cs` and register it inside `src/JXReborn.ZoneEngine/Scripting/ScriptingZoneModule.cs` (including participant cleanup on player detach)
- [X] T044 [US4] Add custom map trap and `MissionZoneTickSystem` lifecycle/interval tests in `tests/JXReborn.ZoneEngine.Tests/Scripting/ScriptingZoneHostTests.cs`

**Checkpoint**: All four gameplay script archetypes (`INpcDialogScript`, `IItemUsageScript`, `ITrapScript`, `IMissionController`) are operational.

---

## Phase 7: User Story 5 - Live Gameplay Script Hot-Reload & Per-Player Fault Containment (Priority: P2)

**Goal**: Support lock-free hot-reloading of compiled script `.dll`s via `MemoryStream` + collectible `AssemblyLoadContext(isCollectible: true)` with atomic `ScriptRegistrySnapshot` swaps, rollback on invalid bundles, zero memory leaks, and strict `try/catch` per-player fault isolation across all script entry points.

**Independent Test**: Run `ScriptHotReloadUnloadabilityTests` proving v1 -> v2 live swap, invalid assembly rejection preserving v1, and `WeakReference.IsAlive == false` after `GC.Collect()`, plus concurrent Player A exception containment while Player B continues uninterrupted.

- [X] T045 [US5] Implement `CollectibleScriptAssemblyLoader` reading `.dll`/`.pdb` bytes into `MemoryStream` inside `AssemblyLoadContext(name, isCollectible: true)` and validating script types before snapshot construction in `src/JXReborn.Scripts/Hosting/CollectibleScriptAssemblyLoader.cs`
- [X] T046 [US5] Implement `ReloadFromBytes` / `ReloadFromDirectory` atomic `Interlocked.Exchange` swap and previous-ALC `Unload()` scheduling in `src/JXReborn.Scripts/Hosting/ZoneScriptHost.cs`, plus file-system debounce watcher `ScriptHotReloadWatcher` in `src/JXReborn.Scripts/Hosting/ScriptHotReloadWatcher.cs`
- [X] T047 [US5] Audit and enforce structured `try/catch` fault isolation across `src/JXReborn.ZoneEngine/Scripting/ScriptingCommandHandler.cs`, `src/JXReborn.ZoneEngine/WorldZones/MapZoneWorker.cs` (`HandleUseInventoryItemAsync` defaulting to `ItemUsageResult.KeepItem` on fault & `EvaluatePlayerTrapAsync`), and `src/JXReborn.ZoneEngine/Scripting/MissionZoneTickSystem.cs`, logging via `ScriptsLogMessages` and resetting corrupted `PlayerDialogFeatureSlot` state
- [X] T048 [US5] Author collectible ALC unloadability (`WeakReference` + `[MethodImpl(MethodImplOptions.NoInlining)]` + `GC.Collect()`), invalid-bundle rollback, and multi-player fault containment tests in `tests/JXReborn.ZoneEngine.Tests/Scripting/ScriptHotReloadUnloadabilityTests.cs` and `tests/JXReborn.ZoneEngine.Tests/Scripting/ScriptingZoneHostTests.cs`

**Checkpoint**: Live script hot-reload, memory reclamation, and fault containment pass 100%.

---

## Phase 8: Polish & Cross-Cutting Verification

**Purpose**: Wire host startup registration, run full encrypted TCP bot end-to-end integration tests, and pass the Constitution zero-warning build gate.

- [X] T049 Wire `AddJXRebornScripts()`, `ScriptingZoneModule`, `DialogNpcPacketHandler`, and `PlayerSelectUiPacketHandler` into `src/JXReborn.Host/Program.cs` (and legacy adapter DI extensions)
- [X] T050 Author end-to-end encrypted TCP headless bot integration tests (`0x6E` -> `0x63` -> `0x5F` nested *Xa Phu* teleport, `0x59` `MagicScript` item activation, and persistent `TaskValues` survival across re-login) in `tests/JXReborn.Integration.Tests/FactionsAndScripting/NpcDialogAndItemScriptIntegrationTests.cs`
- [X] T051 Run `JXReborn.DataExporter` to verify `artifacts/jxreborn-data.pack` snapshot compilation with `ScriptUsage` and `ScriptCode` bindings, then execute `dotnet build JXReborn.slnx -c Release -warnaserror` and `dotnet test JXReborn.slnx` with zero warnings and 100% green tests

---

## Dependencies & Execution Order

### Phase Dependencies

1. **Phase 1 (Setup)** $\rightarrow$ No prerequisites; starts immediately.
2. **Phase 2 (Foundational)** $\rightarrow$ Depends on Phase 1; establishes non-collectible contracts and static asset bindings blocking all user stories.
3. **Phase 3 (`US1` — Interactive NPC Dialogs & Multi-Step Menus, P1)** $\rightarrow$ Depends on Phase 2; delivers the MVP dialog pipeline and *Xa Phu* reference script.
4. **Phase 4 (`US2` — Multi-Page `Talk` Monologues, P1)** $\rightarrow$ Builds cleanly on `ScriptActionS2CPacket` and `PlayerDialogFeatureSlot` from Phase 3.
5. **Phase 5 (`US3` — Scripted Items & Task Variables, P1)** $\rightarrow$ Depends on Phase 2 (`ZoneScriptDialogContext` from Phase 3 reused for item context). Can execute in parallel with Phase 4 once `ZoneScriptDialogContext` exists.
6. **Phase 6 (`US4` — Custom Map Traps & Zone Missions, P2)** $\rightarrow$ Depends on Phase 2 (`ZoneScriptDialogContext` reused for trap context). Can execute in parallel with Phase 5.
7. **Phase 7 (`US5` — Collectible Hot-Reload & Fault Isolation, P2)** $\rightarrow$ Depends on `ZoneScriptHost` (Phase 2) and script entry points (Phases 3–6).
8. **Phase 8 (Polish & End-to-End Integration)** $\rightarrow$ Depends on Phases 3–7.

### Parallel Opportunities

- **Within Phase 2**: `T005`–`T009` (static asset templates) and `T011`–`T015` (non-collectible interfaces) touch completely separate 1-type-per-file `.cs` files and can execute in parallel.
- **Within Phase 3 (`US1`)**: `T020`, `T021`, `T022` (wire packets) and `T024` (zone commands) can be implemented in parallel before wiring handlers (`T026`, `T028`).
- **Across Stories**: Once `ZoneScriptDialogContext` (`T027`) lands, `US2` (`Talk`), `US3` (`MagicScript` items), and `US4` (`CustomScript` traps + `MissionZoneTickSystem`) can proceed in parallel across separate sub-folders (`Dialogs/`, `Items/`, `Traps/`, `Missions/`).
