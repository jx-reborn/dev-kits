# Tasks: Song-Jin Battleground (Spec 027)

**Feature Branch**: `027-module-battleground-songjin`  
**Status**: Completed & Verified  

---

## Task List

- [X] **T01 — Primitives & Domain State** `src/JXReborn.Primitives/Missions/`
  - [X] Define `SongJinPhase.cs` (Registration=0, Battle=1, Settlement=2, Cooldown=3, Idle=4)
  - [X] Define `SongJinCamp.cs` (None=0, Song=1, Jin=2)
  - [X] Define `SongJinMilitaryRank.cs` (BinhSi=1, HieuUy=2, ThongLanh=3, PhoTuong=4, DaiTuong=5)
  - [X] Define `MissionConsts.cs` (SongJinSignUpFee=20000, SongJinMaxCampDifference=5, SongJinRankPkBonusPercent 5x5 matrix, SongJinTaskBattleScore=747)

- [X] **T02 — Contracts & DTOs** `src/JXReborn.Contracts/Missions/`
  - [X] Define `SongJinParticipantScoreDto.cs`
  - [X] Define `SongJinScoreboardDto.cs`

- [X] **T03 — Zone Engine Module & Controller** `src/JXReborn.ZoneEngine/Missions/SongJin/`
  - [X] Implement `SongJinParticipantState.cs` (military rank recalculation, kill streaks, shutdown bonuses)
  - [X] Implement `PlayerSongJinSessionSlot.cs : IPlayerFeatureSlot`
  - [X] Implement `SongJinMissionController.cs : IMissionController` (5 phases, camp balancing, scoring, scoreboard)
  - [X] Implement commands and handler: `RegisterSongJinZoneCommand.cs`, `ExchangeBattleScoreZoneCommand.cs`, `QuerySongJinScoreboardZoneCommand.cs`, `RecordSongJinCombatKillZoneCommand.cs`, `SongJinCommandHandler.cs`
  - [X] Implement plug-in module `SongJinZoneModule.cs : IZoneModule`
  - [X] Register in `ZoneEngineServiceCollectionExtensions.cs`

- [X] **T04 — Wire Packets & Protocol Handlers** `src/JXReborn.Adapter.Legacy/`
  - [X] Define opcodes `s2c_battlefieldstatus` (0xA0) and `s2c_battlefieldscoreboard` (0xA1)
  - [X] Implement `SongJinStatusSyncS2CPacket.cs`
  - [X] Implement `SongJinScoreboardS2CPacket.cs`
  - [X] Implement `ScriptProtocolPacketHandler.cs` routing for Song-Jin operation 27

- [X] **T05 — Automated Testing & Verification**
  - [X] Controller unit tests: `tests/JXReborn.ZoneEngine.Tests/Missions/SongJinMissionControllerTests.cs`
  - [X] Protocol encoding tests: `tests/JXReborn.Protocol.Tests/Missions/SongJinPacketEncodingTests.cs`
  - [X] Integration tests: `tests/JXReborn.Integration.Tests/Missions/SongJinBattlegroundIntegrationTests.cs`
  - [X] Architecture compliance: `SolutionArchitectureComplianceTests` (4/4 passed)
