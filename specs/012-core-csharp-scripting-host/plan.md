# Implementation Plan: Gameplay Scripting Host & Interactive Dialog System

**Branch**: `012-core-csharp-scripting-host` | **Date**: 2026-11-11 | **Spec**: [spec.md](spec.md)  
**Input**: Feature specification from `/specs/012-core-csharp-scripting-host/spec.md`

---

## 1. Summary

Deliver the authoritative **C# 14 Fluent Gameplay Scripting Host & Interactive Dialog System (`012-core-csharp-scripting-host`)** for `JXReborn` (.NET 10, C# 14) in strict compliance with **Constitution v1.5.0** and **`JX Reborn Data Standard v1`**:

1. **Non-Collectible Zone Scripting Spine (`JXReborn.ZoneEngine/Scripting/`)**: Define compile-time verified, zero-pinning scripting contracts (`IZoneScriptHost`, `INpcDialogScript`, `IItemUsageScript`, `ITrapScript`, `IMissionController`, `IScriptDialogContext`, `IScriptItemContext`, `IScriptTrapContext`, `IMissionContext`, `DialogOption`, `ItemUsageResult`), domain-segregated outbound sink (`IDialogOutboundSink`), per-player dialog feature slot (`PlayerDialogFeatureSlot : IPlayerFeatureSlot`), command handler (`ScriptingCommandHandler`), periodic mission tick system (`MissionZoneTickSystem`), and modular registration (`ScriptingZoneModule : IZoneModule`).
2. **Collectible Hot-Reloadable Script Engine (`JXReborn.Scripts`)**: Introduce the standalone `JXReborn.Scripts` class library implementing `ZoneScriptHost` backed by lock-free `Volatile.Read` / `Interlocked.Exchange` `ScriptRegistrySnapshot` swaps, `CollectibleScriptAssemblyLoader` (`AssemblyLoadContext(isCollectible: true)` loaded exclusively from `MemoryStream` / `byte[]` with zero disk file locks), `ScriptHotReloadWatcher`, `FluentDialogBuilder`, context implementations, and the reference `TownTeleporterDialogScript` (*Xa Phu*).
3. **Static Asset & Schema Extensions (`JXReborn.Primitives` & `JXReborn.GameAssets`)**: Align `ItemGenreKind` (`QuestKey = 4`, `MagicScript = 6`), add `ItemScriptUsageTemplate` onto `ItemTemplate`, add `ScriptCode` + `DialogRadiusCells` onto `StaticNpcSpawnTemplate` and `NpcZoneState`, add `PortalTrapActionKind.CustomScript = 3` and `ScriptCode` onto `PortalTrapBindingTemplate`, and update `MemoryPackSnapshotCompiler` and `GameAssetsSnapshotLoader`.
4. **Byte-Exact Legacy 6.0 Wire Protocol (`JXReborn.Adapter.Legacy`)**: Register `c2s_playerselui` (`0x5F`, **17 bytes**) in `ProtocolSizeTable.cs`, implement symmetric packets and handlers for `DialogNpcC2SPacket` (`0x6E`, 9B with `0x506C` check key), `PlayerSelectUiC2SPacket` (`0x5F`, 17B), and `ScriptActionS2CPacket` (`0x63`, `16 + len`B supporting Linux 6.0 null-terminated TCVN3 `Say`/`Describe` and `"| |"`-delimited `Talk`), forward `TargetId` from `PlayerEatItemPacketHandler` (`0x59`, 16B) onto `UseInventoryItemZoneCommand`, and implement `GameServerClientSession.DialogSink.cs`.

---

## 2. Technical Context

- **Language & Runtime**: C# 14 on .NET 10 (`net10.0`), XML solution (`JXReborn.slnx`), centralized `Directory.Build.props` (`Nullable=enable`, `TreatWarningsAsErrors=true`, `AnalysisLevel=latest`).
- **Primary Dependencies**:
  - `Microsoft.Extensions.DependencyInjection.Abstractions` / `Hosting.Abstractions` / `Logging.Abstractions` / `Options` (`10.0.0`)
  - `MemoryPack` (static asset snapshot serialization in `JXReborn.GameAssets` & `JXReborn.DataExporter`)
  - `System.Runtime.Loader` (`AssemblyLoadContext` built into .NET 10 BCL — zero external scripting VM packages)
- **Storage & State**:
  - Persistent quest variables (`TaskValues`: `ushort 0..65535 -> int32`) stored on `PlayerZoneState` and persisted write-behind to `Character.TaskValues` via `ICharacterAppService`.
  - Session-volatile quest variables (`TempTaskValues`: `ushort 0..65535 -> int32`) and pending dialog option closures stored in `PlayerDialogFeatureSlot` and deterministically cleared prior to callback invocation and on `OnPlayerDetached`.
- **Testing**:
  - `xunit.v3` across `JXReborn.Protocol.Tests` (`ScriptingDialogPacketTests`), `JXReborn.ZoneEngine.Tests` (`ScriptingZoneHostTests`, `ScriptHotReloadUnloadabilityTests`), and `JXReborn.Integration.Tests` (`NpcDialogAndItemScriptIntegrationTests`).
- **Performance & Operational Targets**:
  - Lock-free `O(1)` `FrozenDictionary` script resolution on the 18-Hz zone thread (`< 100 ns` lookup, zero allocations on lookup).
  - Pre-callback state reset guarantees arbitrarily deep nested `Say`/`Describe`/`Talk` menus (`SC-001`).
  - Collectible `AssemblyLoadContext` deterministic unload verified via `WeakReference` + `GC.Collect()` (`SC-005`).
  - 100% per-session `try/catch` fault isolation across NPC dialogs, item uses, trap triggers, and mission ticks (`SC-006`).

---

## 3. Constitution Check (v1.5.0)

### Core Principles (I–VI)

| Principle | Status | Verification Evidence |
| :--- | :---: | :--- |
| **I. Hybrid Distributed Control Plane & Sharded 18-Hz Zone Architecture** | **PASS** | Dialogs, item scripts, custom traps, and mission ticks execute strictly inside the single-threaded 18-Hz `MapZoneWorker` shard; cross-map teleports delegate cleanly to `IZoneClusterRouter`; `TaskValues` persist asynchronously via write-behind snapshots. |
| **II. Multi-Protocol Transport Abstraction & Evolutionary Client Compatibility** | **PASS** | `JXReborn.ZoneEngine` and `JXReborn.Scripts` operate strictly on UTF-8 strings, `DialogNpcZoneCommand`, `SelectDialogUiZoneCommand`, and `IDialogOutboundSink`; legacy opcodes (`0x6E`, `0x63`, `0x5F`, `0x59`, `0x84`, `0x88`) are confined to `JXReborn.Adapter.Legacy`. |
| **III. Zero-Hallucination Evidence & Ground-Truth Fidelity (NON-NEGOTIABLE)** | **PASS** | Every byte offset, 17-byte `0x5F` 6.0 layout, `0x506C` check key, `256` Mps Euclidean distance gate, and `0x63` null-terminated vs. `"\| \|"` framing is verified against legacy 6.0 protocol vectors and `jx-swarm` in [research.md](research.md). |
| **IV. Zero-Dupe Economy & Lease-Fenced Session Integrity** | **PASS** | Every dialog and item command verifies `player.LeaseEpoch == cmd.LeaseEpoch`; `ItemUsageResult` defaults to `KeepItem` on precondition failure or caught script exception; currency/item helpers delegate through core `PlayerInventoryGrid` invariants. |
| **V. UTF-8 Domain Core, Dual-Tier Game Data Standard & Boundary-Only Transcoding** | **PASS** | All C# script prompts, options, pages, and `ScriptCode` bindings use UTF-8; `TCVN3` transcoding executes exclusively inside `ScriptActionS2CPacket` at the legacy adapter boundary; static bindings compile into `jxreborn-data.pack` (`MemoryPack`). |
| **VI. Clean Modular Boundaries, ABP DDD Role Separation & C# 14 Fluent Scripting API** | **PASS** | Implements the Constitution-mandated `JXReborn.Scripts` C# 14 Fluent Scripting API with collectible `AssemblyLoadContext` hot-reload, zero untyped Lua VMs, 1 type per `.cs` file, `sealed` concrete classes, and pluralized folders. |

### Verification Gates (1–5)

| Gate | Status | Enforcement |
| :--- | :---: | :--- |
| **1. Ground-Truth Specification Gate** | **PASS** | Documented with exact ELF addresses, C++ line numbers, and hex vectors in [research.md](research.md) and [dialog-and-script-wire-protocol.md](contracts/dialog-and-script-wire-protocol.md). |
| **2. Symmetric Protocol, Schema & Formula Unit Gate** | **PASS** | Enforced by `ScriptingDialogPacketTests.cs` (`0x6E`, `0x5F`, `0x63` `Say`/`Describe`/`Talk`, `0x59` `TargetId`, `ProtocolSizeTable` 17B check) and JSON Schema validation. |
| **3. Autonomous Swarm & Headless Integration Gate** | **PASS** | Enforced by `NpcDialogAndItemScriptIntegrationTests.cs` driving full TCP handshake -> walk within `256` Mps -> `0x6E` -> `0x63` -> `0x5F` nested *Xa Phu* teleport & `0x59` item script. |
| **4. High-Density Stress & Fault Isolation Gate** | **PASS** | Enforced by `ScriptingZoneHostTests.cs` and `ScriptHotReloadUnloadabilityTests.cs` verifying throwing scripts isolate to Player A while Player B continues uninterrupted and retired ALCs unload cleanly. |
| **5. Structural & Zero-Warning Build Gate** | **PASS** | Enforced by `dotnet build JXReborn.slnx -warnaserror` with strict 1-type-per-file and `sealed` concrete class compliance. |

---

## 4. Project Structure

### 4.1 Specification Package (`specs/012-core-csharp-scripting-host/`)

```text
specs/012-core-csharp-scripting-host/
├── spec.md                                            # Technology-agnostic specification (16/16 verified)
├── plan.md                                            # Technical context, Constitution check & file tree
├── research.md                                        # Ground-truth C++ / Linux 6.0 / jx-swarm analysis
├── data-model.md                                      # Entities, dialog state machine & task variables
├── quickstart.md                                      # Build & 3-tier verification guide
├── tasks.md                                           # Dependency-ordered implementation tasks
├── checklists/
│   └── requirements.md                                # 16/16 SpecKit quality checklist
└── contracts/
    ├── dialog-and-script-wire-protocol.md             # Wire byte tables & hex test vectors (0x6E, 0x5F, 0x63, 0x59)
    └── csharp-fluent-scripting-api.md                 # C# 14 scripting interfaces, contexts & Xa Phu showcase
```

### 4.2 Source Code & Test File Tree (`1 Type Per File`, `sealed` Concrete Classes)

```text
JXReborn.slnx                                          # [MODIFY] Add src/JXReborn.Scripts/JXReborn.Scripts.csproj

schemas/
├── item-catalog.schema.json                           # [MODIFY] Add optional scriptUsage object on item entries
└── map-document.schema.json                           # [MODIFY] Add CustomScript trap action & scriptCode on NPC spawns / traps

src/
├── JXReborn.Primitives/
│   ├── Items/
│   │   └── ItemGenreKind.cs                           # [MODIFY] Align QuestKey = 4 / MagicScript = 6 (+ legacy compatibility)
│   ├── Scripting/
│   │   └── ScriptingConsts.cs                         # [NEW] MaxDialogOptions = 50, MaxScriptActionContentBytes = 900, DefaultDialogRadiusCells = 4
│   └── WorldZones/
│       └── PortalTrapActionKind.cs                    # [MODIFY] Add CustomScript = 3
│
├── JXReborn.GameAssets/
│   ├── Items/
│   │   ├── ItemScriptUsageTemplate.cs                 # [NEW] [MemoryPackable] ScriptCode, CastSkillId, CastSkillLevel, MinRequiredLevel, ConsumeOnSkillFallback
│   │   └── ItemTemplate.cs                            # [MODIFY] Add optional ItemScriptUsageTemplate? ScriptUsage = null
│   ├── NpcTemplates/
│   │   └── StaticNpcSpawnTemplate.cs                  # [MODIFY] Add string? ScriptCode = null, byte DialogRadiusCells = 4
│   ├── WorldMaps/
│   │   └── PortalTrapBindingTemplate.cs               # [MODIFY] Add string? ScriptCode = null for PortalTrapActionKind.CustomScript
│   └── Snapshots/
│       ├── GameAssetsSnapshotBundle.cs                # [VERIFY/MODIFY] Preserve binary compatibility with updated templates
│       └── GameAssetsSnapshotLoader.cs                # [MODIFY] Hydrate ScriptUsage, NPC ScriptCode/DialogRadiusCells, Trap ScriptCode
│
├── JXReborn.ZoneEngine/
│   ├── Items/
│   │   └── UseInventoryItemZoneCommand.cs             # [MODIFY] Add uint TargetNpcId = 0 parameter forwarded from 0x59
│   ├── NpcEntities/
│   │   └── NpcZoneState.cs                            # [MODIFY] Add string ScriptCode and byte DialogRadiusCells = 4
│   ├── PlayerSessions/
│   │   ├── IDialogOutboundSink.cs                     # [NEW] SendDialogSelectMenu, SendDialogTalkPages, SendOpenSaleBox, SendOpenStoreBox
│   │   ├── NoOpDialogOutboundSink.cs                  # [NEW] Singleton no-op IDialogOutboundSink implementation
│   │   ├── IPlayerOutboundSink.cs                     # [MODIFY] Inherit IDialogOutboundSink
│   │   └── PlayerZoneState.cs                         # [MODIFY] Add DialogSink property resolved from OutboundSink
│   ├── Scripting/
│   │   ├── DialogNpcZoneCommand.cs                    # [NEW] Enqueued from 0x6E (CharacterId, LeaseEpoch, TargetNpcId)
│   │   ├── DialogOption.cs                            # [NEW] Readonly record struct (TextUtf8, Callback)
│   │   ├── IItemUsageScript.cs                        # [NEW] ScriptCode + OnUseItemAsync(IScriptItemContext, CancellationToken)
│   │   ├── IMissionContext.cs                         # [NEW] Per-zone mission state, participants, broadcast & teleport helpers
│   │   ├── IMissionController.cs                      # [NEW] Start, TimerTick, PlayerJoined, PlayerLeft, Stop hooks
│   │   ├── INpcDialogScript.cs                        # [NEW] ScriptCode + OnDialogAsync(IScriptDialogContext, CancellationToken)
│   │   ├── IScriptDialogContext.cs                    # [NEW] UTF-8 Say, Describe, Talk, Task/TempTask & Engine-First player helpers
│   │   ├── IScriptItemContext.cs                      # [NEW] Extends IScriptDialogContext with RuntimeItemId, ItemCode, TargetNpcId
│   │   ├── IScriptTrapContext.cs                      # [NEW] Extends IScriptDialogContext with TrapId, TrapScriptCode, SetFightMode
│   │   ├── ITrapScript.cs                             # [NEW] ScriptCode + OnStepTrapAsync(IScriptTrapContext, CancellationToken)
│   │   ├── ItemUsageResult.cs                         # [NEW] Explicit enum { KeepItem = 0, ConsumeOne = 1 }
│   │   ├── IZoneScriptHost.cs                         # [NEW] Non-collectible lookup contract for NPC, Item, Trap & Mission scripts
│   │   ├── MissionZoneTickSystem.cs                   # [NEW] Sealed IZoneTickSystem stepping active zone missions at Phase 550
│   │   ├── PendingDialogKind.cs                       # [NEW] Enum { None = 0, WaitingOptionSelection = 1, WaitingTalkCompletion = 2 }
│   │   ├── PlayerDialogFeatureSlot.cs                 # [NEW] Sealed IPlayerFeatureSlot managing pending callbacks & TempTaskValues
│   │   ├── ScriptingCommandHandler.cs                 # [NEW] Sealed handler for DialogNpcZoneCommand & SelectDialogUiZoneCommand
│   │   ├── ScriptingZoneModule.cs                     # [NEW] Sealed IZoneModule registering commands, slot key & MissionZoneTickSystem
│   │   └── SelectDialogUiZoneCommand.cs               # [NEW] Enqueued from 0x5F (CharacterId, LeaseEpoch, SelectIndex, SelectType, Param1, Param2)
│   └── WorldZones/
│       ├── ZoneAssetServices.cs                       # [MODIFY] Add IZoneScriptHost? ScriptHost { get; init; }
│       └── MapZoneWorker.cs                           # [MODIFY] Dispatch MagicScript items in HandleUseInventoryItemAsync & CustomScript traps in EvaluatePlayerTrapAsync
│
├── JXReborn.Scripts/
│   ├── JXReborn.Scripts.csproj                        # [NEW] References JXReborn.Primitives, JXReborn.GameAssets, JXReborn.ZoneEngine
│   ├── Hosting/
│   │   ├── CollectibleScriptAssemblyLoader.cs         # [NEW] Sealed loader reading byte[] -> MemoryStream into collectible ALC
│   │   ├── ScriptHostOptions.cs                       # [NEW] Sealed options (HotReloadDirectory, DebounceMs, EnableHotReload)
│   │   ├── ScriptHotReloadWatcher.cs                  # [NEW] Sealed IHostedService watching compiled script .dll updates
│   │   ├── ScriptRegistrySnapshot.cs                  # [NEW] Sealed immutable FrozenDictionary snapshot + WeakReference ALC tracker
│   │   ├── ScriptsServiceCollectionExtensions.cs      # [NEW] AddJXRebornScripts() DI registration extension
│   │   └── ZoneScriptHost.cs                          # [NEW] Sealed IZoneScriptHost with Volatile.Read / Interlocked.Exchange swap
│   ├── Dialogs/
│   │   ├── FluentDialogBuilder.cs                     # [NEW] Sealed fluent builder for Say / Describe / Talk menus
│   │   ├── TownTeleporterDialogScript.cs              # [NEW] Sealed Xa Phu teleporter reference script + station.lua:451 Level < 5 Talk guard (FR-015)
│   │   ├── ZoneScriptDialogContext.cs                 # [NEW] Context implementation backed by PlayerZoneState & ZoneExecutionContext
│   │   └── ZoneScriptDialogContextBase.cs             # [NEW] Shared base context enabling DialogOption callbacks across NPC & Item scripts
│   ├── Items/
│   │   ├── TestGmTokenItemScript.cs                   # [NEW] Opt-in symmetric GM Token (6,1,4258) script gated by ZoneEngineOptions.EnableTestGmToken (FR-016)
│   │   └── ZoneScriptItemContext.cs                   # [NEW] Sealed IScriptItemContext implementation
│   ├── Traps/
│   │   └── ZoneScriptTrapContext.cs                   # [NEW] Sealed IScriptTrapContext implementation
│   ├── Missions/
│   │   ├── ActiveZoneMissionSlot.cs                   # [NEW] Sealed per-zone active mission state & participant roster
│   │   └── ZoneMissionContext.cs                      # [NEW] Sealed IMissionContext implementation
│   └── Logging/
│       └── ScriptsLogMessages.cs                      # [NEW] Source-generated [LoggerMessage] structured diagnostics
│
├── JXReborn.Adapter.Legacy/
│   ├── Framing/
│   │   └── ProtocolSizeTable.cs                       # [MODIFY] Add ProtocolOpcodes.c2s_playerselui => payload.Length == 17
│   ├── Packets/
│   │   ├── DialogNpcC2SPacket.cs                      # [NEW] 0x6E (9B <BII) symmetric parser/writer
│   │   ├── PlayerSelectUiC2SPacket.cs                 # [NEW] 0x5F (17B <Biiii) symmetric parser/writer
│   │   └── ScriptActionS2CPacket.cs                   # [NEW] 0x63 (16 + len B) Say / Describe / Talk TCVN3 serializer & parser
│   └── GameServer/
│       ├── DialogNpcPacketHandler.cs                  # [NEW] Sealed 0x6E handler verifying 0x506C CheckValue & dispatching DialogNpcZoneCommand
│       ├── GameServerClientSession.DialogSink.cs      # [NEW] Partial class implementing IDialogOutboundSink (0x63, 0x84, 0x88)
│       ├── PlayerEatItemPacketHandler.cs              # [MODIFY] Forward packet.TargetId onto UseInventoryItemZoneCommand
│       └── PlayerSelectUiPacketHandler.cs             # [NEW] Sealed 0x5F handler dispatching SelectDialogUiZoneCommand
│
└── JXReborn.Host/
    ├── JXReborn.Host.csproj                           # [MODIFY] Reference JXReborn.Scripts.csproj
    └── Program.cs                                     # [MODIFY] Register AddJXRebornScripts() and ScriptingZoneModule

tools/
└── JXReborn.DataExporter/
    └── Compilation/
        └── MemoryPackSnapshotCompiler.cs              # [MODIFY] Compile ItemScriptUsageTemplate, NPC ScriptCode & Trap ScriptCode

tests/
├── JXReborn.Protocol.Tests/
│   └── ScriptingDialogPacketTests.cs                  # [NEW] Symmetric round-trip & hex-vector tests for 0x6E, 0x5F, 0x63, 0x59 & ProtocolSizeTable
├── JXReborn.ZoneEngine.Tests/
│   └── Scripting/
│       ├── ScriptingZoneHostTests.cs                  # [NEW] Nested Say/Describe/Talk, distance gate, Task/TempTask, MagicScript item, CustomScript trap, Mission ticks & fault isolation
│       ├── ScriptHotReloadUnloadabilityTests.cs       # [NEW] MemoryStream reload swap, invalid DLL rollback & WeakReference + GC.Collect() ALC unload proof
│       └── TestGmTokenItemScriptTests.cs              # [NEW] Verifies EnableTestGmToken production lock & multi-page DialogOption callback routing
├── JXReborn.Integration.Tests/
│   └── FactionsAndScripting/
│       └── NpcDialogAndItemScriptIntegrationTests.cs  # [NEW] End-to-end TCP bot test: 0x6E -> 0x63 -> 0x5F nested Xa Phu teleport & 0x59 item script
└── JXReborn.ClientDriver.Tests/
    └── DifferentialParity/
        └── DifferentialParityMovementAndDialogTests.cs # [NEW] Journey J03 live parity against Zero-Sim jx-runtime-standalone Oracle
```

---

## 5. Complexity Tracking

> Zero Constitution violations. All components strictly obey the four-tier inward dependency graph (`Primitives` $\leftarrow$ `GameAssets` $\leftarrow$ `ZoneEngine` $\leftarrow$ `Scripts` / `Adapter.Legacy` $\leftarrow$ `Host`), one-type-per-file rule, `sealed` concrete classes, and zero-VM C# 14 Fluent Scripting mandate.
