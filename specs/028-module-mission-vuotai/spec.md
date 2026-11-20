# Specification: Vượt Ải Dungeon (Spec 028)

**Feature Code**: `028-module-mission-vuotai`  
**Status**: Implemented & Verified  
**Golden Reference**: Spec 016 (`IZoneModule`, `IMissionController`)  

---

## 1. Executive Summary

This feature provides the Vượt Ải Dungeon (Nhiệm Vụ Thách Thức Thời Gian) engine for JXReborn Phase 2. The dungeon enforces strict party formation rules (2 to 8 players, all level 50+, leader pays 10,000 silver, atomic consumption of 1 Sát Thủ Giản token per member), drives 28 sequential waves of escalating difficulty within an 18-minute global countdown, synchronizes a 1-second Wave Timer Board, and spawns milestone clear chests (Bảo Rương Vượt Ải) with experience and item rewards.

The implementation functions strictly as an isolated plug-in module (`VuotAiZoneModule : IZoneModule`) registering `VuotAiMissionController` and zone command routes without altering `MapZoneWorker.cs`.

---

## 2. Functional Requirements

### FR-001: Party Leader Entry & Token Validation
- Entry requires the player to be a Party Captain with a party of 2 to 8 members.
- Leader must possess at least 10,000 silver in inventory.
- Every member present must be level 50+ and possess 1 Sát Thủ Giản token (`VuotAiTokenGenre=6, VuotAiTokenDetail=1, VuotAiTokenParticular=400`).
- Entry executes atomic consumption of 10,000 silver from the leader and 1 Sát Thủ Giản token from each member.

### FR-002: 28 Sequential Waves
- 28 canonical waves with increasing monster counts and boss entities.
- Each wave spawns wave-specific monsters.
- Advancing to the next wave occurs automatically when all monsters in the current wave are defeated.

### FR-003: 18-Minute Timer & Wave Timer Board
- Dungeon runs on an authoritative 18-minute countdown timer (`TotalLimitSeconds = 1080`).
- Wave Timer Board synchronizes every second (`INTERVAL_BOARD = 1`):
  `<#> Nhiệm vụ 'Thách thức Thời gian' đã bắt đầu rồi {m} phút, bây giờ là {w} đám quái, còn dư {c} Con quái.`
- If timer expires before completing Wave 28, the party is wiped/ejected (`VuotAiPhase.Wipe`).

### FR-004: Milestone Clear Chests
- Milestone waves (Wave 7, 14, 21, 28) award milestone clear chests (Bảo Rương Vượt Ải).
- Claiming chests grants experience, silver, and rare items.

---

## 3. Success Criteria

1. **Zero Core Modifications**: Zero changes to `src/JXReborn.ZoneEngine/WorldZones/MapZoneWorker.cs` (`git diff = 0`).
2. **Atomic Verification**: Token consumption and leader fee deductions are atomic across all members.
3. **Canonical Progression**: All 28 waves and milestone chest waves function deterministically.
4. **Architecture Compliance**: Clean Release build with `-warnaserror` (0w/0e) and 4/4 passed `SolutionArchitectureComplianceTests`.
