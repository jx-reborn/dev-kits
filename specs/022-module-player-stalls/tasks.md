# Tasks: Personal City Stalls & Atomic Market Buyout

**Feature Branch**: `022-module-player-stalls`  
**Status**: Completed & Verified  

---

## Task List

- [X] **T01 — Primitive Stall Definitions** `src/JXReborn.Primitives/Stalls/`
  - [X] Define stall constants: `StallConsts.cs`
  - [X] Define stall sub-protocol enum: `StallSubProtocolKind.cs`
  - [X] Define stall buy failure enum: `StallBuyFailureReason.cs`
  - [X] Define player menu state enum: `PlayerMenuStateKind.cs`

- [X] **T02 — Game Assets & Catalogs** `src/JXReborn.GameAssets/Stalls/`
  - [X] Define `IStallTaxCatalog.cs`, `StallTaxCatalog.cs`, and `StallTaxEntry.cs`

- [X] **T03 — Wire Packet Serialization** `src/JXReborn.Adapter.Legacy/Packets/`
  - [X] Define `StallExtendC2SPacket.cs` (`0x7C`)
  - [X] Define `StallExtendS2CPacket.cs` (`0x9D`)
  - [X] Define `SyncMenuStateS2CPacket.cs` (`0x76`)

- [X] **T04 — Plug-in Module & Feature Slot** `src/JXReborn.ZoneEngine/Stalls/`
  - [X] Implement `PlayerStallSessionSlot : IPlayerFeatureSlot`
  - [X] Implement `PlayerStallZoneModule : IZoneModule`
  - [X] Implement `StallCommandHandler` (MarkPrice, SetBanner, BuyItem, Exit, RequestAdv, RequestCatalog, RequestTaxRate)
  - [X] Implement outbound sinks: `IStallOutboundSink.cs`, `NoOpStallOutboundSink.cs`

- [X] **T05 — Packet Handlers & Adapter Sinks** `src/JXReborn.Adapter.Legacy/`
  - [X] Implement `StallExtendPacketHandler.cs`
  - [X] Implement session sink: `GameServerClientSession.StallSink.cs`
  - [X] Register in `LegacyAdapterServiceCollectionExtensions.cs`

- [X] **T06 — Test Verification**
  - [X] Protocol golden vector tests: `tests/JXReborn.Protocol.Tests/GameServer/PlayerStallGoldenVectorPacketTests.cs`
  - [X] ZoneEngine unit tests: `tests/JXReborn.ZoneEngine.Tests/Stalls/PlayerStallZoneModuleTests.cs` & `PlayerStallBuyoutAndTaxBoundaryTests.cs`
  - [X] Integration tests: `tests/JXReborn.Integration.Tests/ItemsAndEconomy/PlayerStallMarketIntegrationTests.cs`
  - [X] Parity test fixture: `qa/JXReborn.ClientDriver.Tests/DifferentialParity/DifferentialParityPlayerStallTests.cs`
