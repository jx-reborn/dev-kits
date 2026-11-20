---
description: "Actionable, dependency-ordered task list for 008-core-inventory-and-equipment: Spatial Inventory & Equipment Grid"
---

# Tasks: Spatial Inventory & Equipment Grid

**Input**: Design documents from `/specs/008-core-inventory-and-equipment/` (`spec.md`, `plan.md`, `research.md`, `data-model.md`, `contracts/`, `quickstart.md`)  
**Prerequisites**: `plan.md` (required), `spec.md` (required for user stories), `research.md`, `data-model.md`, `contracts/`  
**Status**: All tasks completed (`[X]`) and verified against actual codebase.

---

## Phase 1: Shared Primitives, Domain Entities & Catalogs

- [X] T001 [P] [US1] Define `ItemContainerPlace`, `ItemGenreKind`, `EquipmentPartKind`, and `ItemConsts` (EquipRoomWidth=6, EquipRoomHeight=10, MaxEquipParts=15) in `src/JXReborn.Primitives/Items/`.
- [X] T002 [P] [US2] Extend `CharacterItem` domain aggregate with `IsBroken` and `EnchaseLevel` in `src/JXReborn.Domain/Characters/CharacterItem.cs` and update `CharacterItemDto` in `src/JXReborn.Contracts/Characters/CharacterItemDto.cs`.
- [X] T003 [P] [US1] Implement `IItemTemplateCatalog` and `ItemTemplateCatalog` in `src/JXReborn.GameAssets/Items/`.

---

## Phase 2: 2D Spatial Container Matrix & Five-Elements Affixes (`JXReborn.ZoneEngine`)

- [X] T004 [P] [US1, US2] Implement `PlayerItemSlot` in `src/JXReborn.ZoneEngine/Items/PlayerItemSlot.cs` and `PlayerItemSyncSnapshot` in `src/JXReborn.ZoneEngine/PlayerSessions/PlayerItemSyncSnapshot.cs`.
- [X] T005 [US1, US2, US5] Implement `PlayerInventoryGrid` managing 6x10 bag occupancy, 2D rectangular footprint bounds, 15 equipment slots, Five-Elements generation ring affix activation, stack splitting, and cursor disconnect safety (`SafelyReturnHandItem`) in `src/JXReborn.ZoneEngine/Items/PlayerInventoryGrid.cs`.
- [X] T006 [P] [US1, US3, US4, US5] Implement typed zone commands `MoveInventoryItemZoneCommand`, `DivideInventoryItemZoneCommand`, `DropInventoryItemZoneCommand`, and `UseInventoryItemZoneCommand` in `src/JXReborn.ZoneEngine/Items/`.
- [X] T007 [US1, US3, US4, US5] Implement `InventoryLootCommandHandler` routing container moves, stack splits, consumable usage, and item dropping in `src/JXReborn.ZoneEngine/Items/InventoryLootCommandHandler.cs`.
- [X] T008 [P] [US1, US2, US3, US5] Unit test 2D rectangular collision, Five-Elements hidden affix ring, stack splitting, and cursor disconnect recovery in `tests/JXReborn.ZoneEngine.Tests/Items/PlayerInventoryAndEquipmentTests.cs` and `tests/JXReborn.Domain.Tests/Characters/CharacterItemTests.cs`.

---

## Phase 3: Wire Protocol Packets & Outbound Sinks (`JXReborn.Adapter.Legacy`)

- [X] T009 [P] [US1, US3, US4, US5] Implement wire packet layouts for `PlayerMoveItemC2SPacket` (`0x5B`), `PlayerMoveItemS2CPacket` (`0x61`), `ItemAutoMoveS2CPacket` (`0x8E`), `PlayerDivideItemC2SPacket` (`0x85`), `PlayerEatItemC2SPacket` (`0x59`), `SyncItemS2CPacket` (`0x5F`, 80B), `RemoveItemS2CPacket` (`0x60`, 5B), `SyncMoneyS2CPacket` (`0x68`, 13B), `PlayerThrowAwayItemC2SPacket` (`0x5E`), and `ThrowAwayAllMedicineC2SPacket` (`0x83`) in `src/JXReborn.Adapter.Legacy/Packets/`.
- [X] T010 [P] [US1, US3, US4] Unit test wire packet serialization, 64-byte `SViewItemInfo` layout, and round-trips in `tests/JXReborn.Protocol.Tests/GameServer/InventoryAndEquipmentPacketTests.cs`.
- [X] T011 [P] [US1, US3, US4, US5] Implement packet handlers `PlayerMoveItemPacketHandler`, `PlayerDivideItemPacketHandler`, `PlayerEatItemPacketHandler`, `PlayerThrowAwayItemPacketHandler`, and `ThrowAwayAllMedicinePacketHandler` in `src/JXReborn.Adapter.Legacy/GameServer/`.
- [X] T012 [US1, US3] Implement `SendItemSync`, `SendItemRemove`, `SendMoneySync`, `SendPlayerMoveItemConfirm`, and `SendItemAutoMoveUnlock` in `src/JXReborn.Adapter.Legacy/GameServer/GameServerClientSession.InventorySink.cs`.

---

## Phase 4: Integration Verification

- [X] T013 [US1, US2, US3, US4] Verify end-to-end live TCP inventory synchronization, item move/swap, medicine consumption, and currency updates in `tests/JXReborn.Integration.Tests/ItemsAndEconomy/InventoryAndLootIntegrationTests.cs`.
