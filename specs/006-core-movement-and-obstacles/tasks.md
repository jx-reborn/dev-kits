---
description: "Actionable, dependency-ordered task list for 006-core-movement-and-obstacles: Movement, Posture & Obstacle Validation"
---

# Tasks: Movement, Posture & Obstacle Validation

**Input**: Design documents from `/specs/006-core-movement-and-obstacles/` (`spec.md`, `plan.md`, `research.md`, `data-model.md`, `contracts/`, `quickstart.md`)  
**Prerequisites**: `plan.md` (required), `spec.md` (required for user stories), `research.md`, `data-model.md`, `contracts/`  
**Status**: All tasks completed (`[X]`) and verified against actual codebase.

---

## Phase 1: Shared Primitives & Obstacle Assets

- [X] T001 [P] [US2] Implement `EntityDoingPosture` enum (`Stand = 1`, `Walk = 2`, `Run = 3`, `Sit = 4`, `Jump = 5`) and `MovementAntiCheatConsts` (`WalkCheckKey = 0x24A1`, `RunCheckKey = 0x1D78`, and `IsValidCheckValue`) in `src/JXReborn.Primitives/WorldZones/EntityDoingPosture.cs` and `src/JXReborn.Primitives/WorldZones/MovementAntiCheatConsts.cs`.
- [X] T002 [P] [US1] Implement `RegionObstacleGrid` fast bitmask for 16x32 cell obstacle lookups in `src/JXReborn.GameAssets/WorldMaps/RegionObstacleGrid.cs`.

---

## Phase 2: Movement Commands & 18-Hz Simulation Tick System (`JXReborn.ZoneEngine`)

- [X] T003 [P] [US1, US3] Implement typed zone commands `MovePlayerZoneCommand`, `JumpPlayerZoneCommand`, and `TogglePlayerSitZoneCommand` in `src/JXReborn.ZoneEngine/PlayerSessions/MovePlayerZoneCommand.cs`, `src/JXReborn.ZoneEngine/PlayerSessions/JumpPlayerZoneCommand.cs`, and `src/JXReborn.ZoneEngine/PlayerSessions/TogglePlayerSitZoneCommand.cs`.
- [X] T004 [US1, US3] Implement `MovementCommandHandler` validating anti-cheat check values, breaking sit posture on movement, updating player state, and broadcasting `0x50`/`0x51`/`0x53`/`0x54` in `src/JXReborn.ZoneEngine/WorldZones/MovementCommandHandler.cs`.
- [X] T005 [US1, US3] Implement `MovementAndPostureZoneTickSystem` (Phase 500) stepping walk (10 Mps) and run (20 Mps) coordinates frame-by-frame, clamping at `Obstacle_Full` cells, updating spatial regions, and executing sitting vital regeneration in `src/JXReborn.ZoneEngine/WorldZones/MovementAndPostureZoneTickSystem.cs`.
- [X] T006 [P] [US1] Unit test 18-Hz deterministic frame-accurate speed progression, obstacle boundary clamping, and sitting vital recovery in `tests/JXReborn.ZoneEngine.Tests/Movement/MovementSimulationTests.cs`.

---

## Phase 3: Wire Protocol Packets & Outbound Sinks (`JXReborn.Adapter.Legacy`)

- [X] T007 [P] [US1, US3] Implement wire packet structs for `NpcWalkC2SPacket` (`0x4B`), `NpcRunC2SPacket` (`0x4C`), `NpcWalkS2CPacket` (`0x50`), `NpcRunS2CPacket` (`0x51`, asymmetric layout), `NpcJumpC2SPacket` (`0x4E`), `NpcJumpS2CPacket` (`0x54`), `NpcSitC2SPacket` (`0x71`), and `NpcSitS2CPacket` (`0x53`) in `src/JXReborn.Adapter.Legacy/Packets/`.
- [X] T008 [P] [US1, US2, US3] Unit test packet round-trips, asymmetric field layouts, and anti-cheat checksum evaluation in `tests/JXReborn.Protocol.Tests/GameServer/MovementAndPosturePacketTests.cs`.
- [X] T009 [P] [US1, US3] Implement packet handlers `NpcWalkPacketHandler`, `NpcRunPacketHandler`, `NpcJumpPacketHandler`, and `NpcSitPacketHandler` in `src/JXReborn.Adapter.Legacy/GameServer/`.
- [X] T010 [US1, US3] Implement `SendWalkBroadcast`, `SendRunBroadcast`, `SendJumpBroadcast`, and `SendSitBroadcast` in `src/JXReborn.Adapter.Legacy/GameServer/GameServerClientSession.WorldSink.cs`.

---

## Phase 4: Integration Verification

- [X] T011 [US1, US2, US3] Verify live TCP movement progression, anti-cheat invalid checksum rejection, and posture synchronization over live network sockets in `tests/JXReborn.Integration.Tests/WorldAndMovement/MovementIntegrationTests.cs`.
