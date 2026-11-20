# Tasks: Great Golden World Bosses (Spec 026)

**Feature Branch**: `026-module-world-bosses`  
**Status**: Completed & Verified  

---

## Task List

- [X] **T01 — Primitives & Domain State** `src/JXReborn.Primitives/Bosses/`
  - [X] Define `WorldBossId.cs` (11 Golden Boss IDs: Cổ Bách=566, Huyền Giác=740, Đường Phi Yến=1366, Lam Y Y=582, Hà Linh Phiêu=568, Yên Hiểu Trái=744, Mạnh Thương Lương=583, Gia Luật Tị Ly=563, Đạo Thanh Chân Nhân=562, Tuyền Cơ Tử=747, Từ Đại Nhạc=1367)
  - [X] Define `BossDropOwnershipKind.cs` (None, Individual, Party)
  - [X] Define `WorldBossConsts.cs` (DropOwnershipProtectSeconds=30, ShareExpRadiusMps=768)

- [X] **T02 — Game Assets & Catalog** `src/JXReborn.GameAssets/Bosses/`
  - [X] Define `BossTemplate.cs` record
  - [X] Define `IBossTemplateCatalog.cs` interface
  - [X] Implement `BossTemplateCatalog.cs` with authoritative templates for all 11 bosses
  - [X] Register catalog in `GameAssetsServiceCollectionExtensions.cs`

- [X] **T03 — Zone Engine Module & Systems** `src/JXReborn.ZoneEngine/Bosses/`
  - [X] Implement `WorldBossAttributionSession.cs` (damage attribution rules, party aggregation, top damager evaluation)
  - [X] Implement `WorldBossManager.cs` (per-zone active session tracking)
  - [X] Implement `WorldBossDropEvaluator.cs` (Archimedean spiral loot spawning, 30s ownership protection)
  - [X] Implement tick systems: `WorldBossScheduledSpawnerTickSystem.cs`, `WorldBossDamageAttributionTickSystem.cs`
  - [X] Implement zone commands and handler: `SpawnWorldBossZoneCommand.cs`, `QueryWorldBossStatusZoneCommand.cs`, `AttackWorldBossZoneCommand.cs`, `WorldBossCommandHandler.cs`
  - [X] Implement plug-in module `WorldBossZoneModule.cs : IZoneModule`
  - [X] Register in `ZoneEngineServiceCollectionExtensions.cs`

- [X] **T04 — Wire Protocol Adapter** `src/JXReborn.Adapter.Legacy/`
  - [X] Implement `AutoAttackNpcPacketHandler.cs` routing for SubCommand 26/0x1A
  - [X] Implement `ScriptProtocolPacketHandler.cs` for opcode 0xAB

- [X] **T05 — Automated Testing & Verification**
  - [X] Unit tests: `tests/JXReborn.ZoneEngine.Tests/WorldBosses/WorldBossZoneModuleTests.cs`
  - [X] Integration tests: `tests/JXReborn.Integration.Tests/WorldBosses/WorldBossDamageAttributionIntegrationTests.cs`
  - [X] Architecture compliance: `SolutionArchitectureComplianceTests` (4/4 passed)
