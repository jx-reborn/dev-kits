# Tasks: Repository Box, Secondary Password & Revival Binding

**Feature Branch**: `018-module-repository-box`  
**Status**: Completed & Verified  

---

## Task List

- [X] **T01 — Primitive Storage Definitions** `src/JXReborn.Primitives/Storage/`
  - [X] Define storage box constants: `StorageBoxConsts.cs`
  - [X] Define storage box operations enum: `StorageBoxOperationKind.cs`

- [X] **T02 — Wire Packet Serialization** `src/JXReborn.Adapter.Legacy/Packets/`
  - [X] Define store money packet struct (`0x73`): `StoreMoneyC2SPacket.cs`
  - [X] Define box operate packet struct (`0x84` C2S): `BoxOperateC2SPacket.cs`
  - [X] Define open store box packet struct (`0x88` S2C): `OpenStoreBoxS2CPacket.cs`

- [X] **T03 — Inventory Grid Extension** `src/JXReborn.ZoneEngine/Items/`
  - [X] Extend `PlayerInventoryGrid` with `_repositoryRoomGrid[6, 10]`
  - [X] Add support for `ItemContainerPlace.RepositoryRoom` in `TryPlaceItemDirect`, `FindItemAt`, `DetachFromContainer`

- [X] **T04 — Plug-in Module & Feature Slot** `src/JXReborn.ZoneEngine/Storage/`
  - [X] Implement `PlayerRepositorySessionSlot : IPlayerFeatureSlot`
  - [X] Implement `RepositoryBoxZoneModule : IZoneModule`
  - [X] Register `DepositWithdrawMoneyZoneCommand`, `OperateStorageBoxZoneCommand`, `BindRevivalPointZoneCommand`

- [X] **T05 — Command Handler Implementation** `src/JXReborn.ZoneEngine/Storage/`
  - [X] Implement `RepositoryBoxCommandHandler`
  - [X] Atomic silver deposit/withdraw with negative/overflow guards
  - [X] Secondary PIN state machine (set, unlock, lock, change, cancel) via `LegacyPasswordHasher`
  - [X] Revival point binding logic

- [X] **T06 — Packet Handlers & DI Registration** `src/JXReborn.Adapter.Legacy/GameServer/`
  - [X] Implement `StoreMoneyPacketHandler.cs`
  - [X] Implement `BoxOperatePacketHandler.cs`
  - [X] Register in `LegacyAdapterServiceCollectionExtensions.cs`

- [X] **T07 — Test Verification**
  - [X] Protocol golden vector tests: `tests/JXReborn.Protocol.Tests/GameServer/RepositoryBoxGoldenVectorPacketTests.cs`
  - [X] ZoneEngine unit tests: `tests/JXReborn.ZoneEngine.Tests/Storage/RepositoryBoxZoneModuleTests.cs` & `RepositoryMoneyAndPinBoundaryTests.cs`
  - [X] Integration tests: `tests/JXReborn.Integration.Tests/ItemsAndEconomy/RepositoryBoxAndRevivalIntegrationTests.cs`
  - [X] Parity test fixture: `qa/JXReborn.ClientDriver.Tests/DifferentialParity/DifferentialParityRepositoryBoxTests.cs`
