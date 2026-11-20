# Tasks: Core Monster AI, Death, Progression & Revival

**Feature Branch**: `010-core-monster-ai-and-progression`  
**Status**: Completed & Verified  

---

## Task List

- [X] **T01 — Primitive & Asset Model Definitions**
  - [X] Define AI state and attribute enums: `src/JXReborn.Primitives/Combat/MonsterAiStateKind.cs`, `src/JXReborn.Primitives/Combat/BaseAttributeKind.cs`
  - [X] Implement static NPC and player progression templates: `src/JXReborn.GameAssets/NpcTemplates/NpcTemplate.cs`, `src/JXReborn.GameAssets/PlayerTemplates/CharacterBaseTemplate.cs`, `src/JXReborn.GameAssets/PlayerTemplates/PlayerSeriesGrowthRuleTemplate.cs`, and `src/JXReborn.GameAssets/PlayerTemplates/PlayerTemplateCatalog.cs`
  - [X] Wire catalog abstractions: `src/JXReborn.GameAssets/NpcTemplates/INpcTemplateCatalog.cs`, `src/JXReborn.GameAssets/PlayerTemplates/IPlayerTemplateCatalog.cs`

- [X] **T02 — Wire Packet Serialization & Handlers** `src/JXReborn.Adapter.Legacy/Packets/`
  - [X] Define attribute allocation request/response structs (`0x57` / `0x5D`): `PlayerAddBaseAttributeC2SPacket.cs`, `PlayerAttributeS2CPacket.cs`
  - [X] Define skill allocation request/response structs (`0x58` / `0x5E`): `PlayerApplyAddSkillPointC2SPacket.cs`, `PlayerSkillLevelS2CPacket.cs`
  - [X] Define player revive request/broadcast structs (`0x74` / `0x89`): `PlayerReviveC2SPacket.cs`, `NpcReviveS2CPacket.cs`
  - [X] Define entity death broadcast struct (`0x57`): `NpcDeathS2CPacket.cs`
  - [X] Define experience & level-up sync structs (`0x5C` / `0x5B`): `PlayerExpS2CPacket.cs`, `PlayerLevelUpS2CPacket.cs`
  - [X] Wire packet test verification: `tests/JXReborn.Protocol.Tests/GameServer/ProgressionAndRevivalPacketTests.cs`

- [X] **T03 — 18-Hz Monster AI Tick System & State Machine** `src/JXReborn.ZoneEngine/Combat/`
  - [X] Implement in-memory monster simulation state: `src/JXReborn.ZoneEngine/NpcEntities/NpcZoneState.cs`
  - [X] Implement Phase 200 18-Hz monster state machine: `MonsterAiZoneTickSystem.cs`, `MonsterAiTickSystem.cs`
  - [X] Implement aggro evaluation, pursuit navigation, leash return, and scheduled respawn timers
  - [X] Monster AI unit test verification: `tests/JXReborn.ZoneEngine.Tests/Combat/MonsterAiAndProgressionTests.cs`

- [X] **T04 — Progression, Attribute Allocation & Revival Handlers** `src/JXReborn.ZoneEngine/Combat/`
  - [X] Define progression commands: `AllocateBaseAttributeZoneCommand.cs`, `AllocateSkillPointZoneCommand.cs`, `RevivePlayerZoneCommand.cs`
  - [X] Implement progression logic in command handler: `CombatProgressionCommandHandler.cs`
  - [X] Implement derived stat recalculations on attribute allocation (Strength, Dexterity, Vitality, Energy)
  - [X] Implement level threshold checking and unallocated point awards (5 attr, 1 skill per level)
  - [X] Implement town sanctuary revival and posture reset (`FightMode = 0`)

- [X] **T05 — Outbound Sink Integration & End-to-End Verification**
  - [X] Implement progression and death sink methods in legacy adapter: `src/JXReborn.Adapter.Legacy/GameServer/GameServerClientSession.CombatSink.cs`
  - [X] Implement packet handlers: `src/JXReborn.Adapter.Legacy/GameServer/PlayerAddBaseAttributePacketHandler.cs`, `src/JXReborn.Adapter.Legacy/GameServer/PlayerApplyAddSkillPointPacketHandler.cs`, `src/JXReborn.Adapter.Legacy/GameServer/PlayerRevivePacketHandler.cs`
  - [X] Integration test suite verification: `tests/JXReborn.Integration.Tests/CombatAndPvp/MonsterAiAndProgressionIntegrationTests.cs`
