# Implementation Plan: Great Golden World Bosses (Spec 026)

**Branch**: `026-module-world-bosses` | **Date**: 2026-11-15 | **Spec**: [spec.md](spec.md)

## Summary

Deliver the **026-module-world-bosses** specification for `JXReborn` as an isolated plug-in zone module:
1. **Plug-in Architecture (`WorldBossZoneModule : IZoneModule`)**: Registers tick systems `WorldBossScheduledSpawnerTickSystem` and `WorldBossDamageAttributionTickSystem`, plus command routes for `SpawnWorldBossZoneCommand`, `QueryWorldBossStatusZoneCommand`, `AttackWorldBossZoneCommand`. 0 changes to `MapZoneWorker.cs`.
2. **Boss Attribution Sessions (`WorldBossAttributionSession`)**: Tracks per-hit character damage, computes party aggregation within 3x3 AOI, determines winning captain or solo entity per legacy boss-kill attribution rules.
3. **Loot Drops & Protection (`WorldBossDropEvaluator`)**: Spawns items and money in an Archimedean spiral with 30s ownership protection.
4. **Wire Protocol Adapter (`AutoAttackNpcPacketHandler`, `ScriptProtocolPacketHandler`)**: Handles live client packets for boss interactions.

## Technical Context

- Platform: .NET 9 / 10 (`net10.0`), C# 14.
- Target Assemblies: `JXReborn.Primitives`, `JXReborn.GameAssets`, `JXReborn.ZoneEngine`, `JXReborn.Adapter.Legacy`.
- Tests: `JXReborn.ZoneEngine.Tests`, `JXReborn.Protocol.Tests`, `JXReborn.Integration.Tests`.
