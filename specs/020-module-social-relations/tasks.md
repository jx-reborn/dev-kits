# Tasks: Social Relations (Spec 020)

**Feature Branch**: `020-module-social-relations`  
**Status**: Completed & Verified  

---

## Task List

- [X] **T01 — Primitives & Constants** `src/JXReborn.Primitives/Social/`
  - [X] Define `SocialRelationKind.cs` (None=0, Friend=1, Enemy=2, Blacklist=3)
  - [X] Define `SocialConsts.cs` (MaxFriends=100, MaxEnemies=50, MaxBlacklist=50)

- [X] **T02 — Domain & Contracts** `src/JXReborn.Contracts/Social/` & `src/JXReborn.Domain/Social/`
  - [X] Define `CharacterSocialRelationDto.cs` and `ISocialAppService.cs`
  - [X] Define aggregate entity `CharacterSocialRelation.cs`
  - [X] Define repository interface `ICharacterSocialRelationRepository.cs`

- [X] **T03 — Infrastructure & Application Service** `src/JXReborn.Infrastructure/` & `src/JXReborn.Application/`
  - [X] Implement EF Core repository `EfCoreCharacterSocialRelationRepository.cs`
  - [X] Update `JXRebornDbContext.cs` with `DbSet<CharacterSocialRelation> CharacterSocialRelations`
  - [X] Implement `SocialAppService.cs`
  - [X] Register in DI extensions

- [X] **T04 — Zone Engine Social Module** `src/JXReborn.ZoneEngine/Social/`
  - [X] Implement `PlayerSocialSessionSlot.cs : IPlayerFeatureSlot`
  - [X] Implement `SocialRelationGraph.cs`
  - [X] Implement `SocialRelationsZoneModule.cs : IZoneModule`
  - [X] Implement `SocialCommandHandler.cs`
  - [X] Implement outbound sinks: `ISocialOutboundSink.cs`, `NoOpSocialOutboundSink.cs`
  - [X] Hook auto-record enemy in `PvpDeathPenaltyCalculator.cs`
  - [X] Hook whisper suppression in `ChatCommandHandler.cs`

- [X] **T05 — Wire Packets & Handlers** `src/JXReborn.Adapter.Legacy/`
  - [X] Implement packets: `0x61`..`0x68` (C2S) and `0x6A`..`0x72` (S2C)
  - [X] Implement packet handlers: `ChatApplyAddFriendPacketHandler`, `ChatAddFriendPacketHandler`, `ChatRefuseFriendPacketHandler`, `ChatDeleteFriendPacketHandler`, `ChatRedeleteFriendPacketHandler`, `ChatApplyResendAllFriendNamePacketHandler`, `ChatApplySendOneFriendNamePacketHandler`
  - [X] Implement session sink: `GameServerClientSession.SocialSink.cs`
  - [X] Register in `LegacyAdapterServiceCollectionExtensions.cs`

- [X] **T06 — Automated Testing & Verification**
  - [X] Unit tests: `tests/JXReborn.ZoneEngine.Tests/SocialRelations/SocialRelationsTests.cs`
  - [X] Protocol tests: `tests/JXReborn.Protocol.Tests/GameServer/SocialWirePacketTests.cs`
  - [X] Parity test fixture: `qa/JXReborn.ClientDriver.Tests/DifferentialParity/DifferentialParitySocialGraphTests.cs`
