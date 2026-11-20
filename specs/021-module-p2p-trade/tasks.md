# Tasks: Two-Phase Zero-Dupe P2P Player Trade

**Feature Branch**: `021-module-p2p-trade`  
**Status**: Completed & Verified  

---

## Task List

- [X] **T01 — Primitive Trade Definitions** `src/JXReborn.Primitives/Trades/`
  - [X] Define trade constants: `TradeConsts.cs`
  - [X] Define decision enum: `TradeDecisionKind.cs`
  - [X] Define session state enum: `TradeSessionStateKind.cs`

- [X] **T02 — Domain & Contracts** `src/JXReborn.Contracts/Trades/` & `src/JXReborn.Domain/Trades/`
  - [X] Define application service contract: `ITradeAppService.cs`
  - [X] Define DTO inputs and results: `ExecuteTradeSwapInput.cs`, `ExecuteTradeSwapResultDto.cs`, `TradeAuditLogDto.cs`
  - [X] Define aggregate audit entity: `TradeAuditLog.cs`
  - [X] Define repository interface: `ITradeAuditLogRepository.cs`
  - [X] Add `TransferOwnership` to `CharacterItem.cs`

- [X] **T03 — Infrastructure & Application Service** `src/JXReborn.Infrastructure/` & `src/JXReborn.Application/`
  - [X] Implement EF Core audit log repository: `EfCoreTradeAuditLogRepository.cs`
  - [X] Update `JXRebornDbContext.cs` with `DbSet<TradeAuditLog> TradeAuditLogs`
  - [X] Implement `TradeAppService.cs` with transactional atomic swap and `LeaseEpoch` validation
  - [X] Register in DI extensions

- [X] **T04 — Wire Packet Serialization** `src/JXReborn.Adapter.Legacy/Packets/`
  - [X] Define `TradeApplyStartC2SPacket.cs` (`0x6B`) and `TradeApplyStartS2CPacket.cs` (`0x71`)
  - [X] Define `TradeReplyStartC2SPacket.cs` (`0x75`) and `TradeChangeStateS2CPacket.cs` (`0x75`)
  - [X] Define `TradeMoveMoneyC2SPacket.cs` (`0x6C`) and `TradeMoneySyncS2CPacket.cs` (`0x72`)
  - [X] Define `TradeDecisionC2SPacket.cs` (`0x6D`), `TradeDecisionS2CPacket.cs` (`0x78`), and `TradePressOkSyncS2CPacket.cs` (`0x77`)
  - [X] Define `ItemExchangeFinishS2CPacket.cs` (`0x8E`)

- [X] **T05 — Plug-in Module & Feature Slot** `src/JXReborn.ZoneEngine/Trades/`
  - [X] Implement `PlayerTradeSessionSlot : IPlayerFeatureSlot`
  - [X] Implement `P2PTradeZoneModule : IZoneModule`
  - [X] Implement `TradeCommandHandler` (Apply, Reply, MoveMoney, Decision)
  - [X] Implement outbound sinks: `ITradeOutboundSink.cs`, `NoOpTradeOutboundSink.cs`

- [X] **T06 — Packet Handlers & Adapter Sinks** `src/JXReborn.Adapter.Legacy/`
  - [X] Implement packet handlers: `TradeApplyStartPacketHandler`, `TradeReplyStartPacketHandler`, `TradeMoveMoneyPacketHandler`, `TradeDecisionPacketHandler`
  - [X] Implement session sink: `GameServerClientSession.TradeSink.cs`
  - [X] Register in `LegacyAdapterServiceCollectionExtensions.cs`

- [X] **T07 — Test Verification**
  - [X] Protocol golden vector tests: `tests/JXReborn.Protocol.Tests/GameServer/PlayerTradeGoldenVectorPacketTests.cs`
  - [X] ZoneEngine unit tests: `tests/JXReborn.ZoneEngine.Tests/Trades/PlayerTradeZoneModuleTests.cs` & `PlayerTradeDisconnectRollbackTests.cs`
  - [X] Integration tests: `tests/JXReborn.Integration.Tests/ItemsAndEconomy/P2PTradeAtomicSwapIntegrationTests.cs`
  - [X] Parity test fixture: `qa/JXReborn.ClientDriver.Tests/DifferentialParity/DifferentialParityPlayerTradeTests.cs`
