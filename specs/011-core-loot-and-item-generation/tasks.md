# Tasks: Core Loot, Item Generation & Ground Objects

**Feature Branch**: `011-core-loot-and-item-generation`  
**Status**: Completed & Verified  

---

## Task List

- [X] **T01 — Primitive & Data Catalog Foundations**
  - [X] Define item primitives: `src/JXReborn.Primitives/Items/ItemGenreKind.cs`, `src/JXReborn.Primitives/Items/ItemQualityKind.cs`, `src/JXReborn.Primitives/Items/EquipmentPartKind.cs`, `src/JXReborn.Primitives/Items/ItemContainerPlace.cs`, `src/JXReborn.Primitives/Items/GroundObjectKind.cs`, `src/JXReborn.Primitives/Items/ItemConsts.cs`
  - [X] Implement static loot catalogs: `src/JXReborn.GameAssets/LootTables/LootTableCatalog.cs`, `src/JXReborn.GameAssets/LootTables/LootTableTemplate.cs`, `src/JXReborn.GameAssets/LootTables/WeightedLootPoolEntryTemplate.cs`
  - [X] Implement item template catalogs: `src/JXReborn.GameAssets/Items/ItemTemplateCatalog.cs`, `src/JXReborn.GameAssets/Items/ItemTemplate.cs`, `src/JXReborn.GameAssets/Items/MagicAffixTemplate.cs`, `src/JXReborn.GameAssets/Items/GroundObjectTemplate.cs`

- [X] **T02 — Procedural 6-Affix Blue Item Generator** `src/JXReborn.GameAssets/Items/`
  - [X] Port version 4 generator logic (`m_wVersion = 4`): `ItemGenerator.cs`
  - [X] Implement base durability, defense, and damage rolling: `GeneratedItemData.cs`
  - [X] Implement 6-slot magic attribute generation (3 explicit prefix/suffix, 3 hidden Five-Elements slots): `EquipPartWeightsTemplate.cs`, `SeriesWeightsTemplate.cs`
  - [X] Item generator unit test verification: `tests/JXReborn.ZoneEngine.Tests/Items/BlueItemGeneratorAndLootTests.cs`

- [X] **T03 — Wire Packet Serialization & Deserialization** `src/JXReborn.Adapter.Legacy/Packets/`
  - [X] Define ground object add packet struct (`0x64`): `ObjAddS2CPacket.cs`
  - [X] Define ground object remove packet struct (`0x67`): `ObjRemoveS2CPacket.cs`
  - [X] Define ground object query packet struct (`0x4A`): `RequestObjC2SPacket.cs`
  - [X] Define object mouse click packet struct (`0x72`): `ObjMouseClickC2SPacket.cs`
  - [X] Define player pickup packet struct (`0x5A`): `PlayerPickupItemC2SPacket.cs`
  - [X] Define throw-away item packet structs (`0x5E`, `0x83`): `PlayerThrowAwayItemC2SPacket.cs`, `ThrowAwayAllMedicineC2SPacket.cs`
  - [X] Wire packet test verification: `tests/JXReborn.Protocol.Tests/GameServer/GroundObjectPacketTests.cs`

- [X] **T04 — Phase 400 Ground Object Tick System** `src/JXReborn.ZoneEngine/Items/`
  - [X] Implement in-memory ground object slot: `GroundObjectSlot.cs`
  - [X] Implement Phase 400 18-Hz ground object tick system: `GroundObjectZoneTickSystem.cs`, `GroundObjectTickSystem.cs`
  - [X] Implement killer ownership protection timer (180 frames) and despawn expiration (1,080 frames)

- [X] **T05 — Zone Loot Commands & Atomic Pickup Handling** `src/JXReborn.ZoneEngine/Items/`
  - [X] Define pickup and query commands: `PickupGroundObjectZoneCommand.cs`, `RequestGroundObjectSyncZoneCommand.cs`, `DropInventoryItemZoneCommand.cs`
  - [X] Implement atomic pickup handling with zero duplication: `InventoryLootCommandHandler.cs`
  - [X] Implement packet handlers: `src/JXReborn.Adapter.Legacy/GameServer/PlayerPickupItemPacketHandler.cs`, `src/JXReborn.Adapter.Legacy/GameServer/RequestObjPacketHandler.cs`, `src/JXReborn.Adapter.Legacy/GameServer/ObjMouseClickPacketHandler.cs`, `src/JXReborn.Adapter.Legacy/GameServer/PlayerThrowAwayItemPacketHandler.cs`, `src/JXReborn.Adapter.Legacy/GameServer/ThrowAwayAllMedicinePacketHandler.cs`
  - [X] Implement legacy inventory outbound sink: `src/JXReborn.Adapter.Legacy/GameServer/GameServerClientSession.InventorySink.cs`

- [X] **T06 — Integration & Concurrency Verification**
  - [X] End-to-end loot drop and pickup test suite: `tests/JXReborn.Integration.Tests/ItemsAndEconomy/InventoryAndLootIntegrationTests.cs`
  - [X] High-density concurrent pickup race condition stress tests: `tests/JXReborn.Integration.Tests/CombatAndPvp/HighDensityCombatAndLootStressTests.cs`
