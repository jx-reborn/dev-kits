# Tasks: Guilds/Tong System (Spec 024)

**Feature Branch**: `024-module-guilds-tong`  
**Status**: Completed & Verified  

---

## Task List

- [X] **T01 — Primitives & Constants** `src/JXReborn.Primitives/Guilds/`
  - [X] Define `TongRankKind.cs` (Member=0, Manager=1, Director=2, Master=3)
  - [X] Define `CampKind.cs` (Justice=1, Evil=2, Balance=3, Free=4)
  - [X] Define `GuildConsts.cs` (MinMasterLevel=50, MinLeadLevel=20, CreationFee=1_000_000, NhacVuongKiemGenre=4, NhacVuongKiemDetail=195, UpkeepIntervalSeconds=750, MaxDirector=7, MaxManager=56)

- [X] **T02 — Domain & Contracts** `src/JXReborn.Contracts/Guilds/` & `src/JXReborn.Domain/Guilds/`
  - [X] Define `GuildDto.cs`, `GuildMemberDto.cs`, `IGuildAppService.cs`
  - [X] Define aggregate entities: `Guild.cs`, `GuildMember.cs`
  - [X] Define repository interface: `IGuildRepository.cs`

- [X] **T03 — Infrastructure & Application Service** `src/JXReborn.Infrastructure/` & `src/JXReborn.Application/`
  - [X] Implement EF Core repository `EfCoreGuildRepository.cs`
  - [X] Update `JXRebornDbContext.cs` with `DbSet<Guild> Guilds` and `DbSet<GuildMember> GuildMembers`
  - [X] Implement `GuildAppService.cs`
  - [X] Register in DI extensions

- [X] **T04 — Zone Engine Guild Module & Systems** `src/JXReborn.ZoneEngine/Guilds/`
  - [X] Implement `PlayerGuildSessionSlot.cs : IPlayerFeatureSlot`
  - [X] Implement `GuildModel.cs`, `GuildMemberModel.cs`, `GuildManager.cs`
  - [X] Implement `GuildTreasuryUpkeepTickSystem.cs` (750s upkeep math)
  - [X] Implement `GuildZoneModule.cs : IZoneModule`
  - [X] Implement `GuildCommandHandler.cs`
  - [X] Implement outbound sinks: `IGuildOutboundSink.cs`, `NoOpGuildOutboundSink.cs`
  - [X] Enforce Channel 6 chat isolation in `ChatCommandHandler.cs`

- [X] **T05 — Wire Packets & Handlers** `src/JXReborn.Adapter.Legacy/`
  - [X] Implement packets: `TongExtendC2SPacket.cs` (`0x9A`), `TongCreateS2CPacket.cs` (`0xA0`), `TongExMsgS2CPacket.cs` (`0xC3`)
  - [X] Implement packet handlers: `TongExtendPacketHandler.cs`, `TongExMsgPacketHandler.cs`
  - [X] Implement session sink: `GameServerClientSession.GuildSink.cs`
  - [X] Register in `LegacyAdapterServiceCollectionExtensions.cs`

- [X] **T06 — Automated Testing & Verification**
  - [X] Unit tests: `tests/JXReborn.ZoneEngine.Tests/Guilds/GuildCreationTests.cs`
  - [X] Unit tests: `tests/JXReborn.ZoneEngine.Tests/Guilds/GuildTreasuryUpkeepTests.cs`
  - [X] Protocol tests: `tests/JXReborn.Protocol.Tests/GameServer/GuildWirePacketTests.cs`
  - [X] Parity test fixture: `qa/JXReborn.ClientDriver.Tests/DifferentialParity/DifferentialParityEndgameMissionTests.cs`
