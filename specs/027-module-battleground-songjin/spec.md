# Specification: Song-Jin Battleground (Spec 027)

**Feature Code**: `027-module-battleground-songjin`  
**Status**: Implemented & Verified  
**Golden Reference**: Spec 016 (`IZoneModule`, `IMissionController`)  

---

## 1. Executive Summary

This feature delivers the Song-Jin Battleground (Chiến Trường Tống Kim) engine for JXReborn Phase 2. The battleground features a 5-phase lifecycle (Registration, Battle, Settlement, Cooldown, Idle), strict dynamic camp balancing (|Song - Jin| <= 5), an authoritative PK scoring system driven by a 5x5 military rank multiplier matrix and kill streak bonuses, 20-second scoreboard announcements, and BattleScore settlement awarded to player Task 747.

The implementation functions strictly as an isolated plug-in module (`SongJinZoneModule : IZoneModule`) registering `SongJinMissionController` and zone command routes without altering `MapZoneWorker.cs`.

---

## 2. Functional Requirements

### FR-001: 5-State Battleground Lifecycle
- Managed by `SongJinMissionController : IMissionController`.
- Phases:
  1. `Registration` (10 minutes): Players register for Song or Jin camp at the recruiter NPC. Periodic countdown announcements every 2 minutes.
  2. `Battle` (60 minutes): Active combat phase in battleground maps. 20-second score updates broadcast.
  3. `Settlement` (30 seconds): Determines winning camp (or tie), awards endgame points (Win: 1200, Lose: 600, Tie: 900), updates participant scores.
  4. `Cooldown` (30 seconds): Grace period before returning participants to peaceful areas.
  5. `Idle`: Post-battle reset.

### FR-002: Dynamic Camp Balancing
- Camp registration enforces `|SongPlayerCount - JinPlayerCount| <= 5`.
- If one camp has 5 more participants than the other, registration into the larger camp is rejected with an explanatory system message until the smaller camp receives more registrants.

### FR-003: Authoritative PK Combat Scoring & Ranks
- Base kill points: 50 points.
- Scaled by the 5x5 military rank matrix (`SongJinRankPkBonusPercent`):
  - Killing a higher-ranked opponent yields a substantial bonus (up to 2.0x).
  - Killing a lower-ranked opponent yields diminished points (down to 0.2x).
- Kill streak bonuses: +10 bonus points on streaks of 3, 5, 10, etc.
- Ending an opponent's high kill streak awards shutdown points (+50).
- Killing enemy rank 1..6 NPCs awards 5..30 points.
- Killing the opposing camp's Marshal awards +1000 points and triggers a server announcement.

### FR-004: Scoreboard Synchronization
- `GetScoreboard()` returns `SongJinScoreboardDto` with current phase, remaining seconds, camp scores, player counts, and top 10 participants.
- Synchronized via opcode `0xA1` (`s2c_battlefieldscoreboard`) and `0xA0` (`s2c_battlefieldstatus`).

### FR-005: Task 747 Settlement & Quartermaster Exchange
- Accumulated score is banked into player task variable 747 (`SongJinTaskBattleScore`).
- Players can spend BattleScore at the Quartermaster NPC via `ExchangeBattleScoreZoneCommand`.

---

## 3. Success Criteria

1. **Zero Core Modifications**: Zero changes to `src/JXReborn.ZoneEngine/WorldZones/MapZoneWorker.cs` (`git diff = 0`).
2. **Camp Balancing**: Max delta of 5 strictly enforced at registration.
3. **PK Matrix Parity**: Rank multiplier matrix and kill streak bonuses match canonical behavior.
4. **Settlement Banking**: BattleScore awarded accurately to Task 747.
5. **Architecture Compliance**: Clean Release build with `-warnaserror` (0w/0e) and 4/4 passed `SolutionArchitectureComplianceTests`.
