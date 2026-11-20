# Implementation Plan: Core Monster AI, Death, Progression & Revival

**Branch**: `010-core-monster-ai-and-progression` | **Date**: 2026-11-11 | **Spec**: [spec.md](spec.md)

## Summary

Deliver the **010-core-monster-ai-and-progression** vertical slice of `JXReborn` in strict compliance with **Constitution v1.4.0** and **JX Reborn Data Standard v1**:
1. **18-Hz Monster AI State Machine (`MonsterAiZoneTickSystem`)**: Stepping all `NpcZoneState` instances on Phase 200 through `IdlePatrol`, `Pursuit`, `Attack`, `LeashReturn`, and `DeadRespawnWait`, parameterized by `AIMode` (Passive mode 4: `IsAutoAggressive = false`, retaliates only; Stationary modes 7/8: `CanPatrol = false`, fixed at spawn with no patrol steps; Auto-aggressive modes 1, 2, 3, 5, 6, 9, 10: 3x3 surrounding region vision scans), with aggro evaluation, path stepping, attack triggering, and scheduled respawn timers.
2. **Entity Death & Killer Attribution**: Broadcast `0x57` (`s2c_npcdeath`) to the 3x3 region AOI on health depletion, award experience to the killer, and clear monster presence from active targeting.
3. **Experience Accrual & Level-Up Progression**: Apply `level_exp.txt` thresholds from `IPlayerTemplateCatalog`, advance character levels, award 5 unallocated attribute points and 1 unallocated skill point per level, expand max vitals via `level_add.txt` series growth rules, and sync via `s2c_playerlevelup` (`0x5B`) and `s2c_playerexp` (`0x5C`).
4. **Attribute & Skill Allocation**: Handle `AllocateBaseAttributeZoneCommand` (`0x57` $\rightarrow$ `0x5D`) and `AllocateSkillPointZoneCommand` (`0x58` $\rightarrow$ `0x5E`) in `CombatProgressionCommandHandler`, updating `Character` persistence attributes.
5. **Player Death & Sanctuary Revival**: Handle player death posture, clear monster aggro, process `RevivePlayerZoneCommand` (`0x74` $\rightarrow$ `0x89`), reset `FightMode = 0`, restore vitals, and teleport to saved town revival coordinates.

---

## Technical Context

- **Platform & Language**: .NET 10 (`net10.0`), C# 14 with `Nullable=enable`, `TreatWarningsAsErrors=true`, `AnalysisLevel=latest`, `AllowUnsafeBlocks=true`.
- **Primary Projects**:
  - `src/JXReborn.Primitives`: `MonsterAiStateKind`, `BaseAttributeKind`, `EntityRelationKind`.
  - `src/JXReborn.GameAssets`: `INpcTemplateCatalog`, `NpcTemplate`, `IPlayerTemplateCatalog`, `CharacterBaseTemplate`, `PlayerSeriesGrowthRuleTemplate`.
  - `src/JXReborn.ZoneEngine`: `NpcZoneState`, `MonsterAiZoneTickSystem`, `MonsterAiTickSystem`, `CombatProgressionCommandHandler`, `AllocateBaseAttributeZoneCommand`, `AllocateSkillPointZoneCommand`, `RevivePlayerZoneCommand`, `ICombatOutboundSink`.
  - `src/JXReborn.Adapter.Legacy`: Wire packets `PlayerAddBaseAttributeC2SPacket` (`0x57`), `PlayerApplyAddSkillPointC2SPacket` (`0x58`), `PlayerReviveC2SPacket` (`0x74`), `NpcDeathS2CPacket` (`0x57`), `NpcReviveS2CPacket` (`0x89`), `PlayerAttributeS2CPacket` (`0x5D`), `PlayerSkillLevelS2CPacket` (`0x5E`), `PlayerExpS2CPacket` (`0x5C`), `PlayerLevelUpS2CPacket` (`0x5B`), and packet handlers.
  - `src/JXReborn.Domain` & `src/JXReborn.Contracts`: `Character`, `CharacterSkillValue`, `UpdateCharacterWorldSnapshotDto`.
- **Verification Suites**:
  - `tests/JXReborn.Protocol.Tests/GameServer/ProgressionAndRevivalPacketTests.cs`
  - `tests/JXReborn.ZoneEngine.Tests/Combat/MonsterAiAndProgressionTests.cs`
  - `tests/JXReborn.Integration.Tests/CombatAndPvp/MonsterAiAndProgressionIntegrationTests.cs`

---

## Constitution Check (v1.4.0)

| Principle | Status | Evaluation |
| :--- | :---: | :--- |
| **I. Hybrid Distributed Control Plane & Sharded 18-Hz Zone Loop** | **PASS** | Monster AI state machines execute entirely on single-threaded 18-Hz zone shards. Progression state is synced to Control Plane via lease-fenced snapshots. |
| **II. Multi-Protocol Transport Abstraction** | **PASS** | Zone simulation communicates via typed commands (`AllocateBaseAttributeZoneCommand`, `RevivePlayerZoneCommand`) and `ICombatOutboundSink`. Legacy packets remain in `Adapter.Legacy`. |
| **III. Zero-Hallucination & Ground-Truth Fidelity** | **PASS** | Exact behavioral and formula alignment with `level_exp.txt`, `level_add.txt`, and legacy 6.0 progression contracts. |
| **IV. Zero-Dupe Economy & Lease-Fenced Integrity** | **PASS** | Attribute and skill allocations validate monotonic `LeaseEpoch` and available point balances prior to mutation. |
| **V. UTF-8 Domain Core & Boundary-Only Transcoding** | **PASS** | All entity identifiers and string fields are UTF-8; legacy wire packets transcode at the legacy transport boundary. |
| **VI. Clean Modular Boundaries, ABP DDD & C# 14** | **PASS** | 1 type per `.cs` file, sealed concrete classes, pluralized folders (`Combat/`, `Characters/`), unit suffixes (`*MpsX`, `*MpsY`, `*Frames`). |

---

## Project Structure

```text
specs/010-core-monster-ai-and-progression/
├── spec.md
├── checklists/
│   └── requirements.md
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   └── monster-ai-and-progression-protocol.md
└── tasks.md
```

### Actual Codebase Mapping

```text
src/
├── JXReborn.Primitives/
│   └── Combat/
│       ├── MonsterAiStateKind.cs
│       └── BaseAttributeKind.cs
├── JXReborn.GameAssets/
│   ├── NpcTemplates/
│   │   ├── INpcTemplateCatalog.cs
│   │   └── NpcTemplate.cs
│   └── PlayerTemplates/
│       ├── CharacterBaseTemplate.cs
│       ├── IPlayerTemplateCatalog.cs
│       ├── PlayerSeriesGrowthRuleTemplate.cs
│       └── PlayerTemplateCatalog.cs
├── JXReborn.Domain/
│   └── Characters/
│       ├── Character.cs
│       └── CharacterSkillValue.cs
├── JXReborn.ZoneEngine/
│   ├── NpcEntities/
│   │   └── NpcZoneState.cs
│   ├── Combat/
│   │   ├── MonsterAiZoneTickSystem.cs
│   │   ├── MonsterAiTickSystem.cs
│   │   ├── CombatProgressionCommandHandler.cs
│   │   ├── AllocateBaseAttributeZoneCommand.cs
│   │   ├── AllocateSkillPointZoneCommand.cs
│   │   └── RevivePlayerZoneCommand.cs
│   └── PlayerSessions/
│       ├── ICombatOutboundSink.cs
│       └── NoOpCombatOutboundSink.cs
└── JXReborn.Adapter.Legacy/
    ├── Packets/
    │   ├── PlayerAddBaseAttributeC2SPacket.cs
    │   ├── PlayerApplyAddSkillPointC2SPacket.cs
    │   ├── PlayerReviveC2SPacket.cs
    │   ├── NpcDeathS2CPacket.cs
    │   ├── NpcReviveS2CPacket.cs
    │   ├── PlayerAttributeS2CPacket.cs
    │   ├── PlayerSkillLevelS2CPacket.cs
    │   ├── PlayerExpS2CPacket.cs
    │   └── PlayerLevelUpS2CPacket.cs
    └── GameServer/
        ├── GameServerClientSession.CombatSink.cs
        ├── PlayerAddBaseAttributePacketHandler.cs
        ├── PlayerApplyAddSkillPointPacketHandler.cs
        └── PlayerRevivePacketHandler.cs

tests/
├── JXReborn.Protocol.Tests/
│   └── ProgressionAndRevivalPacketTests.cs
├── JXReborn.ZoneEngine.Tests/
│   └── MonsterAiAndProgressionTests.cs
└── JXReborn.Integration.Tests/
    └── MonsterAiAndProgressionIntegrationTests.cs
```
