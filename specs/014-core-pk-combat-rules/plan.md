# Implementation Plan: Core PK & Combat Rules

**Branch**: `014-core-pk-combat-rules` | **Date**: 2026-11-11 | **Spec**: [spec.md](spec.md)  
**Input**: Feature specification from `/specs/014-core-pk-combat-rules/spec.md` and ground-truth research in `/specs/014-core-pk-combat-rules/research.md`

## Summary

Deliver the authoritative **Core PK & Combat Rules (`014-core-pk-combat-rules`)** subsystem for `JXReborn` in strict compliance with **Constitution v1.4.0** and **JX Reborn Data Standard v1**:
1. **Normal PK Postures, Peace-Zone Cooldown Bypass & Camp Flag Toggling**: Enforce `3,240`-frame (`180s`) Peace-return cooldowns in combat zones (`FightMode != 0`), immediate cooldown-free Peace restoration inside Peace Zones (`FightMode == 0`), `-80%` experience-debt lockouts (`NotFightExpPercent`), initial world-entry `0x90`/`0x93` synchronization, and `0x54` $\rightarrow$ `0x58` temporary `CurrentCamp = EntityCamp.Free` toggling without overwriting `BaseCamp`.
2. **Authoritative Linux 6.0 PvP Hostility Matrix**: Fix `CombatDamageCalculator.EvaluateRelation` and `MissileSimulationTickSystem.StepTick` using `PkRelationContext` so Peace-Zone protection, Novice protection, Party immunity, active 1v1 Sparring, active Enmity (`PkEnmityState.Active` only), `Slaughter` aggression + `Peace` self-defense retaliation, `Peace` protection, and `Fight` camp comparison (`CurrentCamp`) evaluate in exact ground-truth order on both immediate hits and delayed projectiles.
3. **Targeted Enmity (`Cừu Sát`, `0x77` $\rightarrow$ `0x91`)**: Enforce all 11 validation gates, step the `200`-frame non-hostile warmup countdown into `10,800` frames (`10` minutes) of mutual `Enemy` combat via `PkStateZoneTickSystem`, fix the legacy 10-minute expiry bug (where expiry previously called sparring cleanup instead of enmity cleanup) by cleanly closing Enmity on both players, and trigger immediate `CloseAll()` on death, Peace-Zone entry, map transfer, disconnect, or Sparring start.
4. **Engine-First 1v1 Sparring (`Exercise` / `Tỷ Thí`, `0x92`)**: Expose `StartExerciseSparZoneCommand`, `IScriptDialogContext.StartExerciseSpar(...)`, and `ArenaSparringMasterDialogScript` (`INpcDialogScript`) with zero invented wire opcodes, zero death penalties on sparring defeat, and `+1` `PkValue` (`ButcherPKExercise`) on third-party `Slaughter` interference.
5. **Declarative `PkRules` Asset Pipeline & Tiered `PkPunish`**: Load `pkRules` (`0..10` penalty table) from `data/players/progression.players.json` into `GameAssetsSnapshotBundle` / `IPlayerTemplateCatalog`, compute killer `PkValue` increments (`0..10` cap), and execute victim `PkPunish` (XP loss with Lv130+ `HighLevelMaxExpLossByPkValue` caps and `-80%` floor, inventory money loss with `moneyLost / 2` ground money object spawn, bag/equipped item drop rolls, equipped durability percentage abrasion, and `-1` `PkValue` decay above `-50%` XP).

---

## Technical Context

- **Language / Runtime**: C# 14 on .NET 10 (`net10.0`), XML solution `JXReborn.slnx`, `Nullable=enable`, `TreatWarningsAsErrors=true`, `AnalysisLevel=latest`, `AllowUnsafeBlocks=true`.
- **Primary Dependencies**:
  - `JXReborn.Primitives` (zero-dependency enums & frame/percentage constants)
  - `JXReborn.GameAssets` & `JXReborn.DataExporter` (`MemoryPack` `.pack` snapshot + `FrozenDictionary` / `ReadOnlyMemory` catalog for `PkRulesTemplate`)
  - `JXReborn.ZoneEngine` (single-threaded 18-Hz `MapZoneWorker`, `IZoneCommandHandler`, `IZoneTickSystem`, `IPlayerFeatureSlot`, `ICombatOutboundSink`, `IWorldOutboundSink`, `IScriptDialogContext`)
  - `JXReborn.Scripts` (C# 14 `ArenaSparringMasterDialogScript`)
  - `JXReborn.Adapter.Legacy` (binary framing `ProtocolSizeTable`, `*C2SPacket` / `*S2CPacket`, `TCVN3` boundary transcoding)
- **Storage**: `PkState` (`PlayerPkMode`) and `PkValue` (`sbyte`, `0..10`) are already persisted via `Character` / `UpdateCharacterWorldSnapshotDto` write-behind snapshots; transient duel states (`Enmity`, `Exercise`, `CurrentCamp`) live strictly in 18-Hz zone memory (`PlayerPkSessionState : IPlayerFeatureSlot` & `PlayerZoneState`).
- **Testing**: xUnit deterministic unit, 18-Hz `ZoneSimulationHarness`, and full loopback TCP integration suites (`JXReborn.Protocol.Tests`, `JXReborn.ZoneEngine.Tests`, `JXReborn.Integration.Tests`).
- **Target Platform**: Cross-platform .NET 10 server host (Windows & Linux).
- **Performance Goals**: Zero heap allocations per 18-Hz tick in `PkStateZoneTickSystem`, `CombatDamageCalculator.EvaluateRelation`, and `MissileSimulationTickSystem`; `< 1 ms` p99 tick overhead under `100` concurrent duels per map shard.
- **Constraints**: Strictly 1 type per `.cs` file, `sealed` concrete classes, unit-explicit suffixes (`*Frames`, `*PerMil`, `*Percent`), UTF-8 internal strings with `TCVN3` conversion strictly inside `JXReborn.Adapter.Legacy`.

---

## Constitution Check (`v1.4.0`)

| Principle | Status | Verification |
| :--- | :---: | :--- |
| **I. Hybrid Distributed Control Plane & Sharded 18-Hz Zone Loop** | **PASS** | All PK posture checks, Enmity countdowns (`PkStateZoneTickSystem`), projectile relation checks, and PvP death drops execute deterministically inside the single-threaded 18-Hz `MapZoneWorker` with zero DB/network blocking; persisted `PkState`/`PkValue` flow through existing async write-behind snapshots. |
| **II. Multi-Protocol Transport Abstraction** | **PASS** | `JXReborn.ZoneEngine` operates exclusively on strongly-typed `*ZoneCommand` records and `ICombatOutboundSink` / `IWorldOutboundSink` abstractions; legacy opcodes (`0x54`, `0x58`, `0x76`, `0x77`, `0x90`–`0x93`) are confined to `JXReborn.Adapter.Legacy`. |
| **III. Zero-Hallucination Evidence & Ground-Truth Fidelity** | **PASS** | Every opcode, byte offset, `m_wLength = TotalSize - 1` rule, 11 Enmity gates, `200`/`10800` frame timers, 9-step relation precedence, `HighLevelMaxExpLossByPkValue` caps, and `pkpunish.txt` / `pkrate.ini` values are verified in [research.md](research.md). Zero invented opcodes for Sparring. |
| **IV. Zero-Dupe Economy & Lease-Fenced Integrity** | **PASS** | All C2S PK commands validate active `LeaseEpoch`; dropped money (`moneyLost / 2`) and dropped bag/equipped items are atomically deducted from `PlayerZoneState` before spawning `GroundObjectSlot` instances on the single-threaded zone loop. |
| **V. UTF-8 Domain Core & Dual-Tier Game Data Standard** | **PASS** | Opponent names in `PlayerZoneState` and sink methods are standard UTF-8, transcoded to `TCVN3` solely in `PkSyncEnmityStateS2CPacket` / `PkSyncExerciseStateS2CPacket`. `pkRules` loads from `data/players/progression.players.json` (Tier 1) and compiles into `artifacts/jxreborn-data.pack` via `MemoryPack` (Tier 2). |
| **VI. Clean Modular Boundaries, ABP DDD & C# 14 Fluent Scripting** | **PASS** | Dependencies flow strictly inward; every new type resides in its own `.cs` file; concrete handlers, tick systems, and `ArenaSparringMasterDialogScript` are `sealed`; NPC sparring uses C# 14 `IScriptDialogContext` with zero embedded Lua. |

---

## Project Structure

### Documentation (this feature)

```text
specs/014-core-pk-combat-rules/
├── spec.md
├── checklists/
│   └── requirements.md
├── research.md
├── plan.md
├── data-model.md
├── quickstart.md
├── contracts/
│   ├── pk-and-pvp-wire-protocol.md
│   └── pvp-relation-and-punishment-contracts.md
└── tasks.md
```

### Source Code (exact repository mapping — 1 type per file, `sealed` concrete classes)

```text
src/
├── JXReborn.Primitives/
│   └── Combat/
│       ├── PkEnmityState.cs                         # NEW: enum PkEnmityState : byte (Closed = 0, Countdown = 1, Active = 2)
│       └── CombatConsts.cs                          # UPDATE: EnmityWarmupFrames = 200, EnmityActiveDurationFrames = 10800, MaxPkValue = 10, MinFightExpDebtPercent = -80, MinEnmityExpDebtPercent = -50
├── JXReborn.GameAssets/
│   ├── PlayerTemplates/
│   │   ├── PkPenaltyRuleTemplate.cs                 # NEW: [MemoryPackable] sealed partial record PkPenaltyRuleTemplate
│   │   ├── PkRulesTemplate.cs                       # NEW: [MemoryPackable] sealed partial record PkRulesTemplate (+ Lv130+ HighLevelMaxExpLossByPkValue)
│   │   ├── IPlayerTemplateCatalog.cs                # UPDATE: PkRulesTemplate PkRules { get; }, GetPkPenaltyRule(int pkValue)
│   │   └── PlayerTemplateCatalog.cs                 # UPDATE: materialize PkRulesTemplate from snapshot bundle or canonical defaults
│   └── Snapshots/
│       ├── GameAssetsSnapshotBundle.cs              # UPDATE: include PkRulesTemplate? PkRules
│       └── GameAssetsSnapshotLoader.cs              # UPDATE: parse pkRules from data/players/progression.players.json
├── JXReborn.ZoneEngine/
│   ├── Combat/
│   │   ├── ApplyEnmityZoneCommand.cs                # NEW: sealed record ApplyEnmityZoneCommand : IZoneCommand
│   │   ├── SetTemporaryPkCampZoneCommand.cs         # NEW: sealed record SetTemporaryPkCampZoneCommand : IZoneCommand
│   │   ├── StartExerciseSparZoneCommand.cs          # NEW: sealed record StartExerciseSparZoneCommand : IZoneCommand
│   │   ├── PlayerPkSessionState.cs                  # NEW: sealed class PlayerPkSessionState : IPlayerFeatureSlot
│   │   ├── PkRelationContext.cs                     # NEW: readonly record struct PkRelationContext
│   │   ├── PvpDeathPenaltyCalculator.cs             # NEW: static class PvpDeathPenaltyCalculator (DeathCalcPKValue + PkPunish math)
│   │   ├── PkStateZoneTickSystem.cs                 # NEW: sealed class PkStateZoneTickSystem : IZoneTickSystem
│   │   ├── CombatDamageCalculator.cs                # UPDATE: authoritative 6.0 EvaluateRelation overload accepting PkRelationContext
│   │   ├── CombatProgressionCommandHandler.cs       # UPDATE: implement IZoneCommandHandler for ApplyEnmity, SetTemporaryPkCamp, StartExerciseSpar
│   │   ├── ActiveMissileSlot.cs                     # UPDATE: store launcher CharacterId / initial PkRelationContext fallback
│   │   └── MissileSimulationTickSystem.cs           # UPDATE: resolve live attacker & defender PkRelationContext on player collision
│   ├── Logging/
│   │   └── ZoneEngineLogMessages.cs                 # UPDATE: source-generated [LoggerMessage] entries for Enmity, Sparring, and PkPunish
│   ├── PlayerSessions/
│   │   ├── PlayerZoneState.cs                       # UPDATE: BaseCamp + CurrentCamp (Camp alias -> CurrentCamp), PartyId, GetOrCreatePkSessionState()
│   │   ├── ICombatOutboundSink.cs                   # UPDATE: SendPkValueSync, SendPkEnmityStateSync, SendPkExerciseStateSync
│   │   ├── NoOpCombatOutboundSink.cs                # UPDATE: no-op implementations for new PK combat sink methods
│   │   ├── IWorldOutboundSink.cs                    # UPDATE: SendNpcChangeCurCampBroadcast
│   │   ├── NoOpWorldOutboundSink.cs                 # UPDATE: no-op implementation for SendNpcChangeCurCampBroadcast
│   │   ├── IPlayerOutboundSink.cs                   # UPDATE: expose aggregated sink surface if required
│   │   └── SelectivePlayerOutboundSink.cs           # UPDATE: forward SendPkValueSync, SendPkEnmityStateSync, SendPkExerciseStateSync, SendNpcChangeCurCampBroadcast
│   ├── Scripting/
│   │   └── IScriptDialogContext.cs                  # NEW/UPDATE: bool StartExerciseSpar(Guid targetCharacterId) helper contract
│   └── WorldZones/
│       ├── CoreZoneModule.cs                        # UPDATE: register new PK zone commands & PkStateZoneTickSystem
│       └── MapZoneWorker.cs                         # UPDATE: Peace-zone cooldown bypass & -80% XP gate, camp toggle, Enmity/Sparring handlers, CloseAll on FightMode=0/death/detach, HandlePlayerDeath killer attribution & PkPunish
├── JXReborn.Scripts/
│   └── Pvp/
│       └── ArenaSparringMasterDialogScript.cs       # NEW: sealed class ArenaSparringMasterDialogScript (reference 1v1 sparring NPC challenge flow)
└── JXReborn.Adapter.Legacy/
    ├── LegacyAdapterServiceCollectionExtensions.cs # UPDATE: register PlayerApplySetPkPacketHandler & PkApplyEnmityPacketHandler
    ├── Framing/
    │   └── ProtocolSizeTable.cs                     # UPDATE: c2s_playerapplysetpk (0x54 => 2), c2s_pkapplyenmity (0x77 => 5)
    ├── Packets/
    │   ├── ProtocolOpcodes.cs                       # UPDATE: fix comment on s2c_pksyncexercisestate (0x92 => 8 + N bytes)
    │   ├── PlayerApplySetPkC2SPacket.cs             # NEW: 0x54 (2B)
    │   ├── PkApplyEnmityC2SPacket.cs                # NEW: 0x77 (5B)
    │   ├── NpcChangeCurCampS2CPacket.cs             # NEW: 0x58 (6B)
    │   ├── PkSyncEnmityStateS2CPacket.cs            # NEW: 0x91 (8 + N bytes, m_wLength = TotalSize - 1)
    │   ├── PkSyncExerciseStateS2CPacket.cs          # NEW: 0x92 (8 + N bytes, m_wLength = TotalSize - 1)
    │   └── PkSyncPkValueS2CPacket.cs                # NEW: 0x93 (5B)
    └── GameServer/
        ├── PlayerApplySetPkPacketHandler.cs         # NEW: sealed class PlayerApplySetPkPacketHandler
        ├── PkApplyEnmityPacketHandler.cs            # NEW: sealed class PkApplyEnmityPacketHandler
        ├── GameServerClientSession.CombatSink.cs    # UPDATE: serialize 0x91, 0x92, 0x93
        └── GameServerClientSession.WorldSink.cs     # UPDATE: emit 0x90 & 0x93 in SendInitialWorldSyncBurst; serialize 0x58 in SendNpcChangeCurCampBroadcast

tools/
└── JXReborn.DataExporter/
    └── Compilation/
        └── MemoryPackSnapshotCompiler.cs            # UPDATE: compile PkRulesTemplate into artifacts/jxreborn-data.pack and verify SHA-256 round-trip

tests/
├── JXReborn.Protocol.Tests/
│   └── PkWirePacketTests.cs                         # NEW: symmetric round-trip & ProtocolSizeTable tests for 0x54, 0x76, 0x77, 0x58, 0x90, 0x91, 0x92, 0x93
├── JXReborn.ZoneEngine.Tests/
│   └── Combat/
│       ├── PvpRelationMatrixAndPkStateTests.cs      # NEW: exhaustive truth-table, Peace-zone cooldown bypass, camp toggle, 200f warmup, 10800f expiry, CloseAll, missile relation tests
│       └── PvpDeathPenaltySimulationTests.cs        # NEW: DeathCalcPKValue (0..10 cap, ButcherPKExercise) & PkPunish (0..10 tiers, Lv130+ HighLevelMaxExpLossByPkValue, -80%/-50% XP gates, money/2 ground drop, item/equip drop, AbradePercent)
└── JXReborn.Integration.Tests/
    └── PvpEnmityAndSparringIntegrationTests.cs      # NEW: end-to-end loopback TCP duel, initial 0x90/0x93 burst, Enmity duel + kill punishment, and Arena Sparring zero-penalty match
```

**Structure Decision**: Extends the existing modular monolith / distributed shard project layout (`Primitives` $\rightarrow$ `GameAssets` $\rightarrow$ `ZoneEngine` $\rightarrow$ `Scripts` / `Adapter.Legacy`) without introducing extra assemblies. Transient duel and sparring state attaches cleanly to `PlayerZoneState` via `PlayerPkSessionState : IPlayerFeatureSlot` so `OnPlayerDetached` automatically executes `CloseAll` across logout, socket drop, lease preemption, and cross-map handoffs.

---

## Complexity Tracking

No Constitution violations exist. Cross-spec coordination with `012` (`IScriptDialogContext` / `INpcDialogScript`) and `013` (equipped durability abrasion) is isolated behind narrow, single-responsibility contracts (`IScriptDialogContext.StartExerciseSpar` and `PvpDeathPenaltyCalculator.AbradeEquippedItemsByPercent`).
