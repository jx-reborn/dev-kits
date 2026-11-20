# Implementation Plan: Core Combat and 64-Direction Missiles

**Branch**: `009-core-combat-and-missiles` | **Date**: 2026-11-11 | **Spec**: [spec.md](spec.md)

## Summary

Deliver the **009-core-combat-and-missiles** vertical slice of `JXReborn` in strict compliance with **Constitution v1.4.0** and **JX Reborn Data Standard v1**:
1. **Martial Skill Execution & Wire Dispatch**: Support client skill commands (`0x4D` `c2s_npcskill`, wrapped auto-attack `0x87` `c2s_autoattacknpc`, aura switch `0x6F` `c2s_changeauraskill`) routed to `CastSkillZoneCommand`, `ChangeAuraSkillZoneCommand`, and `ChangePkModeZoneCommand` handled by `CombatProgressionCommandHandler`.
2. **64-Direction Fixed-Point Missile Simulation**: Stepping pooled `ActiveMissileSlot` instances on Phase 300 (`MissileZoneTickSystem` / `MissileSimulationTickSystem`) using `SixtyFourDirectionTrigTable` (1024-scaled integer sine/cosine vectors) with hit-radius queries against `SpatialRegionGrid`.
3. **Five-Elements Damage Math & Mitigations**: Deterministic multi-channel damage evaluation in `CombatDamageCalculator` using `FiveElementsSeriesMatrix`, physical/elemental defense reductions, attack rating vs defense rating hit probability clamping, and status effect generation.
4. **Status Ailments & Outbound Sinks**: Manage Cold slow, Poison DoT ticks, Lightning stuns, and hurt recovery in `EntityStatusEffectState` stepped by Phase 100 (`StatusEffectZoneTickSystem`), broadcasting hurt reactions (`0x56` `s2c_broadcasenetcommand` with `do_hurt`), skill casts (`0x5A` / `0x85`), and vitals via `ICombatOutboundSink`.

---

## Technical Context

- **Platform & Language**: .NET 10 (`net10.0`), C# 14 with `Nullable=enable`, `TreatWarningsAsErrors=true`, `AnalysisLevel=latest`, `AllowUnsafeBlocks=true`.
- **Primary Projects**:
  - `src/JXReborn.Primitives`: Math tables (`SixtyFourDirectionTrigTable`), Five-Elements matrices (`FiveElementsSeriesMatrix`), constants (`CombatConsts`), and enumerations (`SkillStyleKind`, `MissileMoveKind`, `DamageChannelKind`, `PlayerPkMode`).
  - `src/JXReborn.GameAssets`: `ISkillTemplateCatalog`, `IMissileTemplateCatalog`, `SkillTemplate`, `MissileTemplate`, `SkillCostScalingTemplate`, `SkillEffectScalingTemplate`.
  - `src/JXReborn.ZoneEngine`: `ActiveMissileSlot`, `SkillDamagePayload`, `EntityStatusEffectState`, `CombatDamageCalculator`, `MissileZoneTickSystem`, `StatusEffectZoneTickSystem`, `CombatProgressionCommandHandler`, `CastSkillZoneCommand`, `ChangeAuraSkillZoneCommand`, `ChangePkModeZoneCommand`, `ICombatOutboundSink`.
  - `src/JXReborn.Adapter.Legacy`: Wire packets `NpcSkillC2SPacket` (`0x4D`), `AutoAttackNpcC2SPacket` (`0x87`), `ChangeAuraSkillC2SPacket` (`0x6F`), `PkApplyChangeNormalFlagC2SPacket` (`0x76`), `SkillCastS2CPacket` (`0x5A`), `CastSkillDirectlyS2CPacket` (`0x85`), `BroadcastNetCommandS2CPacket` (`0x56`), and packet handlers.
- **Verification Suites**:
  - `tests/JXReborn.Protocol.Tests/GameServer/CombatAndSkillPacketTests.cs`
  - `tests/JXReborn.Domain.Tests/MathAndCoordinates/DirectionAndFiveElementsMathTests.cs`
  - `tests/JXReborn.ZoneEngine.Tests/Combat/CombatAndMissileSimulationTests.cs`
  - `tests/JXReborn.Integration.Tests/CombatAndPvp/CombatSkillIntegrationTests.cs`

---

## Constitution Check (v1.4.0)

| Principle | Status | Evaluation |
| :--- | :---: | :--- |
| **I. Hybrid Distributed Control Plane & Sharded 18-Hz Zone Loop** | **PASS** | Missiles and combat tick entirely within single-threaded 18-Hz `MapZoneWorker` shards. High missile activity in one map shard never impacts other zones. |
| **II. Multi-Protocol Transport Abstraction** | **PASS** | `ZoneEngine` operates solely on strongly-typed `CastSkillZoneCommand` and domain `ICombatOutboundSink`. Legacy opcodes and wire layouts stay in `Adapter.Legacy`. |
| **III. Zero-Hallucination & Ground-Truth Fidelity** | **PASS** | Verified against legacy 6.0 combat mechanics, Five-Elements damage formulas, 64-direction kinematics, and `jx-swarm` test vectors. |
| **IV. Zero-Dupe Economy & Lease-Fenced Integrity** | **PASS** | Combat commands validate active `LeaseEpoch` against current player session before processing mutations. |
| **V. UTF-8 Domain Core & Boundary-Only Transcoding** | **PASS** | Skill and missile codes/names use standard UTF-8. Boundary transcoding applies only at wire packet edge. |
| **VI. Clean Modular Boundaries, ABP DDD & C# 14** | **PASS** | 1 type per `.cs` file, sealed concrete classes, pluralized folders (`Combat/`), explicit unit suffixes (`*MpsX`, `*MpsY`, `*Frames`). |

---

## Project Structure

```text
specs/009-core-combat-and-missiles/
├── spec.md
├── checklists/
│   └── requirements.md
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   └── combat-and-skills-protocol.md
└── tasks.md
```

### Actual Codebase Mapping

```text
src/
├── JXReborn.Primitives/
│   └── Combat/
│       ├── SixtyFourDirectionTrigTable.cs
│       ├── FiveElementsSeriesMatrix.cs
│       ├── CombatConsts.cs
│       ├── DamageChannelKind.cs
│       ├── SkillStyleKind.cs
│       ├── MissileMoveKind.cs
│       ├── PlayerPkMode.cs
│       └── EntityRelationKind.cs
├── JXReborn.GameAssets/
│   └── Skills/
│       ├── ISkillTemplateCatalog.cs
│       ├── IMissileTemplateCatalog.cs
│       ├── SkillTemplate.cs
│       ├── MissileTemplate.cs
│       ├── SkillCostScalingTemplate.cs
│       ├── SkillEffectScalingTemplate.cs
│       └── SkillParameterCurveTemplate.cs
├── JXReborn.ZoneEngine/
│   ├── Combat/
│   │   ├── ActiveMissileSlot.cs
│   │   ├── SkillDamagePayload.cs
│   │   ├── EntityStatusEffectState.cs
│   │   ├── CombatDamageCalculator.cs
│   │   ├── MissileZoneTickSystem.cs
│   │   ├── MissileSimulationTickSystem.cs
│   │   ├── StatusEffectZoneTickSystem.cs
│   │   ├── CombatProgressionCommandHandler.cs
│   │   ├── CastSkillZoneCommand.cs
│   │   ├── ChangeAuraSkillZoneCommand.cs
│   │   └── ChangePkModeZoneCommand.cs
│   └── PlayerSessions/
│       ├── ICombatOutboundSink.cs
│       └── NoOpCombatOutboundSink.cs
└── JXReborn.Adapter.Legacy/
    ├── Packets/
    │   ├── NpcSkillC2SPacket.cs
    │   ├── AutoAttackNpcC2SPacket.cs
    │   ├── ChangeAuraSkillC2SPacket.cs
    │   ├── PkApplyChangeNormalFlagC2SPacket.cs
    │   ├── SkillCastS2CPacket.cs
    │   ├── CastSkillDirectlyS2CPacket.cs
    │   └── BroadcastNetCommandS2CPacket.cs
    └── GameServer/
        ├── GameServerClientSession.CombatSink.cs
        ├── NpcSkillPacketHandler.cs
        ├── AutoAttackNpcPacketHandler.cs
        ├── ChangeAuraSkillPacketHandler.cs
        └── PkApplyChangeNormalFlagPacketHandler.cs

tests/
├── JXReborn.Protocol.Tests/
│   ├── CombatAndSkillPacketTests.cs
│   └── DirectionAndFiveElementsMathTests.cs
├── JXReborn.ZoneEngine.Tests/
│   └── CombatAndMissileSimulationTests.cs
└── JXReborn.Integration.Tests/
    └── CombatSkillIntegrationTests.cs
```
