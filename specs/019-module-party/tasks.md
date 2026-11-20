# Tasks: Party Management (Spec 019)

**Feature Branch**: `019-module-party`  
**Status**: Completed & Verified  

---

## Task List

- [X] **T01 — Primitives & Constants** `src/JXReborn.Primitives/Parties/`
  - [X] Define `PartyActionKind.cs`
  - [X] Define `PartyConsts.cs` (MaxMembers=8, ShareExpDist=768, ExpBonusPerMember=15, MinExpShare=5)

- [X] **T02 — Zone Engine Models & Slot** `src/JXReborn.ZoneEngine/Parties/`
  - [X] Implement `PlayerPartySessionSlot.cs : IPlayerFeatureSlot`
  - [X] Implement `PartyModel.cs`
  - [X] Implement `PartyManager.cs` with leadership limits and EXP distribution
  - [X] Implement `PartyAuraTickSystem.cs` radiating elemental resistance buffs

- [X] **T03 — Zone Commands & Zone Module** `src/JXReborn.ZoneEngine/Parties/`
  - [X] Implement `PartyZoneModule.cs : IZoneModule`
  - [X] Implement `PartyCommandHandler.cs`
  - [X] Implement outbound sinks: `IPartyOutboundSink.cs`, `NoOpPartyOutboundSink.cs`
  - [X] Hook monster death EXP sharing in `GroundObjectTickSystem.cs`

- [X] **T04 — Legacy Wire Packets & Handlers** `src/JXReborn.Adapter.Legacy/`
  - [X] Implement `TeamProtocolC2SPacket.cs` (`0x53`)
  - [X] Implement `TeamProtocolS2CPacket.cs` (`0x69`) with length-minus-one framing
  - [X] Implement `TeamProtocolPacketHandler.cs`
  - [X] Implement `GameServerClientSession.PartySink.cs`
  - [X] Register in `LegacyAdapterServiceCollectionExtensions.cs`

- [X] **T05 — Automated Testing & Verification**
  - [X] Unit tests: `tests/JXReborn.ZoneEngine.Tests/Parties/PartyLifecycleTests.cs`
  - [X] Unit tests: `tests/JXReborn.ZoneEngine.Tests/Parties/PartyExpSharingTests.cs`
  - [X] Protocol golden vector tests: `tests/JXReborn.Protocol.Tests/GameServer/PartyWirePacketTests.cs`
  - [X] Parity test fixture: `qa/JXReborn.ClientDriver.Tests/DifferentialParity/DifferentialParityPartyTests.cs`
