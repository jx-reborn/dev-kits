# Implementation Plan: 8,000 Dã Tẩu Quest Engine (Spec 025)

**Branch**: `025-module-quest-datau` | **Date**: 2026-11-15 | **Spec**: [spec.md](spec.md)

## Summary

Deliver the **025-module-quest-datau** specification for `JXReborn` as an isolated plug-in zone module:
1. **Plug-in Architecture (`DaTauQuestZoneModule : IZoneModule`)**: Registers quest command handlers, feature slot `PlayerDaTauQuestSlot`, touching 0 lines of `MapZoneWorker.cs`.
2. **Quest Slot & Session State (`PlayerDaTauQuestSlot : IPlayerFeatureSlot`)**: Maintains active quest parameters, 8000-task progression state, daily counters, and pending reward choices.
3. **Engines & Calculators**:
   - `DaTauQuestCatalog`: Ingests and provides zero-copy lookups for `trea_pos.txt`, `award_link.txt`, `award_basic.txt`, `tasklink_findgoods.txt`, etc.
   - `DaTauQuestGenerator`: Generates quests matching player level and category weight distributions.
   - `DaTauRewardCalculator`: Computes 3-way `Prise` options and evaluates milestone rewards.
4. **Commands**:
   - `AcceptDaTauQuestZoneCommand`, `SubmitDaTauQuestZoneCommand`, `SelectDaTauRewardZoneCommand`, `CancelDaTauQuestZoneCommand`, `FastCompleteDaTauQuestZoneCommand`, `RecordMonsterKillZoneCommand`.
5. **Wire Protocol Handlers**:
   - Hooks into `PlayerSelectUiPacketHandler` for opcode `0x5F` (`c2s_playerselui`).
   - Sinks for `TaskOrderS2CPacket` (65B) and `ScriptActionS2CPacket` (`0x63` SubType `0x0D` for 3-way `Prise`).

## Technical Context

- Platform: .NET 9 / 10 (`net10.0`), C# 14.
- Target Assemblies: `JXReborn.Primitives`, `JXReborn.Contracts`, `JXReborn.GameAssets`, `JXReborn.Domain`, `JXReborn.ZoneEngine`, `JXReborn.Adapter.Legacy`.
- Tests: `JXReborn.GameAssets.Tests`, `JXReborn.ZoneEngine.Tests`, `JXReborn.ClientDriver.Tests`.
