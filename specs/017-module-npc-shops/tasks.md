# Tasks: Multi-Currency NPC Shops & Equipment Repair

**Feature Branch**: `017-module-npc-shops`  
**Status**: Completed & Verified  

---

## Task List

- [X] **T01 — Primitive Shop Definitions** `src/JXReborn.Primitives/Shops/`
  - [X] Define currency type enum: `ShopCurrencyType.cs`
  - [X] Define shop constants: `ShopConsts.cs`

- [X] **T02 — Wire Packet Serialization** `src/JXReborn.Adapter.Legacy/Packets/`
  - [X] Define buy item packet struct (`0x5D`): `PlayerBuyItemC2SPacket.cs`
  - [X] Define sell item packet struct (`0x5C`): `PlayerSellItemC2SPacket.cs`
  - [X] Define repair equipment packet struct (`0x7A`): `RepairEquipmentItemC2SPacket.cs`
  - [X] Define open sale box packet struct (`0x84` S2C): `OpenSaleBoxS2CPacket.cs`
  - [X] Define item repair packet struct (`0x9B` S2C): `ItemRepairS2CPacket.cs`

- [X] **T03 — Plug-in Module & Feature Slot** `src/JXReborn.ZoneEngine/Shops/`
  - [X] Implement `PlayerShopSessionSlot : IPlayerFeatureSlot`
  - [X] Implement `NpcShopZoneModule : IZoneModule`
  - [X] Register `BuyShopGoodZoneCommand`, `SellItemBackZoneCommand`, `RepairEquipmentItemZoneCommand`

- [X] **T04 — Command Handler Implementation** `src/JXReborn.ZoneEngine/Shops/`
  - [X] Implement `NpcShopCommandHandler`
  - [X] Support multi-currency balance validation & deduction
  - [X] Support item placement in inventory bag
  - [X] Support sell-back divisor scaling (`Price / BUY_SELL_SCALE`)
  - [X] Support normal & golden equipment repair via `EquipmentRepairCalculator`

- [X] **T05 — Packet Handlers & DI Registration** `src/JXReborn.Adapter.Legacy/GameServer/`
  - [X] Implement `PlayerBuyItemPacketHandler.cs`
  - [X] Implement `PlayerSellItemPacketHandler.cs`
  - [X] Implement `RepairEquipmentItemPacketHandler.cs`
  - [X] Register in `LegacyAdapterServiceCollectionExtensions.cs`

- [X] **T06 — Test Verification**
  - [X] Protocol golden vector tests: `tests/JXReborn.Protocol.Tests/GameServer/NpcShopGoldenVectorPacketTests.cs`
  - [X] ZoneEngine unit tests: `tests/JXReborn.ZoneEngine.Tests/Shops/NpcShopZoneModuleTests.cs` & `EquipmentRepairCostBoundaryTests.cs`
  - [X] Integration tests: `tests/JXReborn.Integration.Tests/ItemsAndEconomy/NpcShopAndEconomyIntegrationTests.cs`
  - [X] Parity test fixture: `qa/JXReborn.ClientDriver.Tests/DifferentialParity/DifferentialParityNpcShopTests.cs`
