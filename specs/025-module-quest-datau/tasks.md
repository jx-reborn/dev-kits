# Tasks: 8,000 Dã Tẩu Quest Engine (Spec 025)

**Feature Branch**: `025-module-quest-datau`  
**Status**: Completed & Verified  

---

## Task List

- [X] **T01 — Primitives & Domain State** `src/JXReborn.Primitives/Quests/` & `src/JXReborn.Domain/Quests/`
  - [X] Define `DaTauQuestType.cs` (BuyGoods=1, FindGoods=2, ShowGoods=3, FindMaps=4, UpGround=5, WorldMaps=6, KillMonsters=7)
  - [X] Define `DaTauNumericType.cs` (Exp=2, Repute=3, FuYuan=4, PkValue=5, BattleScore=6)
  - [X] Define `DaTauQuestState.cs` (TotalCompletedCount, CurrentTaskTimes, CurrentTaskLinks, CurrentTaskLoops, CancelChancesRemaining, DailyStreak, DailyCompletedCount, DailyFastCompletedCount, ActiveQuest, PendingReward)

- [X] **T02 — Game Assets & Catalogs** `src/JXReborn.GameAssets/Quests/DaTau/`
  - [X] Define `TreaPosEntry.cs` and `IDaTauQuestCatalog.cs`
  - [X] Implement `DaTauQuestCatalog.cs` parsing `trea_pos.txt` (245 rows), `award_link.txt` (10..8000 milestones), `award_basic.txt`, and tasklink tables into immutable collections
  - [X] Register in DI extensions

- [X] **T03 — Zone Engine Quest Systems** `src/JXReborn.ZoneEngine/Quests/`
  - [X] Implement `DaTauQuestGenerator.cs` (level-weighted generation across categories)
  - [X] Implement `DaTauRewardCalculator.cs` (3-way Prise options and milestone evaluations)
  - [X] Implement commands: `AcceptDaTauQuestZoneCommand.cs`, `SubmitDaTauQuestZoneCommand.cs`, `SelectDaTauRewardZoneCommand.cs`, `CancelDaTauQuestZoneCommand.cs`, `FastCompleteDaTauQuestZoneCommand.cs`, `RecordMonsterKillZoneCommand.cs`
  - [X] Implement `PlayerDaTauQuestSlot.cs : IPlayerFeatureSlot`
  - [X] Implement `DaTauQuestCommandHandler.cs`
  - [X] Implement `DaTauQuestZoneModule.cs : IZoneModule`
  - [X] Implement outbound sinks: `IDaTauQuestOutboundSink.cs`, `NoOpDaTauQuestOutboundSink.cs`
  - [X] Register in `ZoneEngineServiceCollectionExtensions.cs`

- [X] **T04 — Wire Packets & Handlers** `src/JXReborn.Adapter.Legacy/`
  - [X] Define opcodes `c2s_needcount` (0x80), `c2s_offlinerequest` (0x8C), `c2s_playerselui` (0x5F)
  - [X] Implement `TaskOrderS2CPacket.cs` (65 bytes)
  - [X] Implement `ScriptActionS2CPacket.cs` (3-way Prise presentation, 0x63 subtype 0x0D)
  - [X] Integrate reward selection routing in `PlayerSelectUiPacketHandler.cs`
  - [X] Implement session sink: `GameServerClientSession.DaTauSink.cs`

- [X] **T05 — Automated Testing & Verification**
  - [X] Catalog tests: `tests/JXReborn.GameAssets.Tests/Quests/DaTauCatalogTests.cs` (verifying trea_pos.txt coordinates and award_link.txt milestones)
  - [X] Unit tests: `tests/JXReborn.ZoneEngine.Tests/Quests/DaTauQuestGeneratorTests.cs`
  - [X] Unit tests: `tests/JXReborn.ZoneEngine.Tests/Quests/DaTauStreakAndCancellationTests.cs`
  - [X] Unit tests: `tests/JXReborn.ZoneEngine.Tests/Quests/DaTauDeliveryValidationTests.cs`
  - [X] Parity tests: `qa/JXReborn.ClientDriver.Tests/DifferentialParity/DifferentialParityEndgameMissionTests.cs`
