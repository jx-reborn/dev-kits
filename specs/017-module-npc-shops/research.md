# Research & Ground-Truth Analysis: Spec 017 Multi-Currency NPC Shops & Equipment Repair

## 1. Legacy 6.0 Wire Protocol & Shop Rules

### A. Shop Purchase (`0x5D`)
- **C2S Wire Packet**: `0x5D` (`c2s_playerbuyitem`), size = 12 bytes.
  - Byte 0: `0x5D` (Opcode).
  - Byte 1: `BuyIdx` (0-based index of item in shop catalog).
  - Byte 2: `Place` (Destination container place, typically `pos_equiproom` = 3).
  - Byte 3: `X` (Target grid X coordinate in bag).
  - Bytes 4..7: `Y / Flags` (Target grid Y coordinate in bag).
  - Bytes 8..11: `CheckToken` (Anti-cheat timestamp token).
- **Validation**:
  - Verifies player has an open shop session (`m_pCurShop`).
  - Verifies currency balance >= good price * scale / 100.
  - Verifies destination container cell is free and has adequate rectangle footprint.

### B. Shop Sell Back (`0x5C`)
- **C2S Wire Packet**: `0x5C` (`c2s_playersellitem`), size = 13 bytes.
  - Byte 0: `0x5C` (Opcode).
  - Bytes 1..4: `RuntimeItemId` (Item in player's bag).
  - Bytes 5..8: `SubType / Flags` (0).
  - Bytes 9..12: `CheckToken` (Anti-cheat token).
- **Formula**:
  - Calls `price / BUY_SELL_SCALE` where `BUY_SELL_SCALE = 4`.
  - Min return is 1 silver.
  - Checks item is unbound (`BindState == Unbound`) and not a quest item (`Genre != ItemGenreKind.Task`).

### C. Equipment Repair (`0x7A` -> `0x9B`)
- **C2S Wire Packet**: `0x7A` (`c2s_repairitem`), size = 9 bytes.
  - Byte 0: `0x7A` (Opcode).
  - Bytes 1..4: `RuntimeItemId` (Equipment to repair).
  - Bytes 5..8: `RepairType` (`0` = Normal, `2` = Cheap Golden, `3` = Costly Golden).
- **S2C Wire Packet**: `0x9B` (`s2c_itemrepair`), size = 13 bytes.
  - Byte 0: `0x9B` (Opcode).
  - Bytes 1..4: `RuntimeItemId`.
  - Bytes 5..8: `MaxDurability`.
  - Bytes 9..12: `CurrentDurability`.

### D. Shop UI Open (`0x84`)
- **S2C Wire Packet**: `0x84` (`s2c_opensalebox`), size = 13 bytes.
  - Byte 0: `0x84` (Opcode).
  - Bytes 1..4: `ShopId`.
  - Bytes 5..8: `ScalePercent` (default 100).
  - Bytes 9..12: `CurrencyType` (0 = Gold, 1 = FuYuan, 2 = Repute, 3 = Honor, 4 = BattleScore, 5 = Coin).

---

## 2. Existing Codebase Assets

- `data/shops/npc-shops.shops.json`: 38,855 lines declaring static shops and item catalogs.
- `IShopCatalog`: Resolves `ShopTemplate` and `ShopGoodTemplate` with zero allocations.
- `EquipmentRepairCalculator`: Authoritative legacy 6.0 calculation engine for repair pricing and golden equipment durability retention.
