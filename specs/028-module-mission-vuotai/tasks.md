# Tasks: Vượt Ải Dungeon (Spec 028)

**Feature Branch**: `028-module-mission-vuotai`  
**Status**: Completed & Verified  

---

## Task List

- [X] **T01 — Primitives & Domain State** `src/JXReborn.Primitives/Missions/`
  - [X] Define `VuotAiPhase.cs` (Registration=0, WaveProgression=1, Wipe=2, Victory=3, Teardown=4)
  - [X] Define `MissionConsts.cs` (VuotAiLeaderFee=10000, VuotAiMinPartySize=2, VuotAiMaxPartySize=8, VuotAiMinLevel=50, Sát Thủ Giản token genre=6/detail=1/particular=400, Bảo Rương Vượt Ải chest genre=6/detail=1/particular=2742, 28 canonical waves, 18-minute countdown)

- [X] **T02 — Contracts & DTOs** `src/JXReborn.Contracts/Missions/`
  - [X] Define `VuotAiProgressDto.cs`

- [X] **T03 — Zone Engine Module & Wave Systems** `src/JXReborn.ZoneEngine/Missions/VuotAi/`
  - [X] Define `VuotAiWaveDefinition.cs` (wave monster configurations and milestone chest flags)
  - [X] Implement `VuotAiWaveCatalog.cs` (immutable catalog of all 28 canonical waves)
  - [X] Implement `PlayerVuotAiSessionSlot.cs : IPlayerFeatureSlot`
  - [X] Implement `VuotAiMissionController.cs : IMissionController` (party entry, 28 sequential waves, 1s timer board, 18m timeout, milestone chests)
  - [X] Implement zone commands and handler: `EnterVuotAiZoneCommand.cs`, `ProgressVuotAiWaveZoneCommand.cs`, `ClaimVuotAiChestZoneCommand.cs`, `QueryVuotAiProgressZoneCommand.cs`, `VuotAiCommandHandler.cs`
  - [X] Implement plug-in module `VuotAiZoneModule.cs : IZoneModule`
  - [X] Register in `ZoneEngineServiceCollectionExtensions.cs`

- [X] **T04 — Wire Packets & Protocol Handlers** `src/JXReborn.Adapter.Legacy/`
  - [X] Define opcode `s2c_dungeonwaveclear` (0x8C)
  - [X] Implement `VuotAiWaveStatusS2CPacket.cs`
  - [X] Implement `ScriptProtocolPacketHandler.cs` routing for Vượt Ải operation 28

- [X] **T05 — Automated Testing & Verification**
  - [X] Controller unit tests: `tests/JXReborn.ZoneEngine.Tests/Missions/VuotAiMissionControllerTests.cs`
  - [X] Protocol encoding tests: `tests/JXReborn.Protocol.Tests/Missions/VuotAiPacketEncodingTests.cs`
  - [X] Integration tests: `tests/JXReborn.Integration.Tests/Missions/VuotAiDungeonIntegrationTests.cs`
  - [X] Architecture compliance: `SolutionArchitectureComplianceTests` (4/4 passed)
