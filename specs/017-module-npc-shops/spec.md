# Feature Specification: Multi-Currency NPC Shops & Equipment Repair

**Feature Branch**: `017-module-npc-shops`  
**Created**: 2026-11-15  
**Status**: Specification Complete  
**Input**: NPC shop open and browse, multi-currency item purchase (Gold, FuYuan, Repute, Honor, BattleScore, Coin), inventory item sell back with divisor scaling, and normal/advanced equipment repair.

---

## 1. Overview & Context

In JX Reborn, NPC merchants provide the core economic foundation for adventurer progression. Players interact with diverse merchants across villages, cities, and faction headquarters:
- **Town Merchants**: Pharmacists, Weapon Smiths, Armorers, and Grocery Vendors selling everyday consumables and base equipment.
- **Specialty & Faction Merchants**: Faction stewards and honor quartermasters selling high-tier martial equipment, scrolls, and rewards in exchange for alternative currencies (FuYuan, Repute, Honor, BattleScore, Coin).
- **Recycling & Durability Maintenance**: Blacksmiths offer item buy-back for silver, and equipment repair services (restoring degraded durability through standard repair or costly advanced repair for golden items).

Architecturally, `017-module-npc-shops` is implemented as an isolated plug-in module (`NpcShopZoneModule : IZoneModule`) adhering to the Golden Reference Open/Closed pattern established by `016-module-chat-channels`, requiring zero modifications to the core zone loop.

---

## 2. Clarifications & Ground-Truth Context

- **Multi-Currency Support**:
  - `Gold / Silver (0)`: Standard game currency stored in `InventoryMoney`.
  - `FuYuan (1)`: Luck/Blessing points earned from events and quests.
  - `Repute (2)`: Prestige points earned from reputation tasks.
  - `Honor (3)`: Faction and combat honor points.
  - `BattleScore (4)`: Song-Jin battlefield accomplishment points.
  - `Coin (5)`: Special token currency.
- **Price Calculation & Scaling**:
  - Purchase Price = `GoodTemplate.Price * BuyPriceScalePercent / 100`.
  - Sell-Back Price = `ItemTemplate.PriceGold / BuySellScale` (default `BUY_SELL_SCALE = 4`), with minimum 1 silver. Bound or quest items cannot be sold back.
- **Equipment Repair Dynamics**:
  - *Normal Repair*: Computes repair cost proportional to missing durability. Restores current durability to max durability.
  - *Advanced Repair (Golden Equipment)*: Preserves maximum durability without abrasion wear, whereas cheap repair inflicts permanent maximum durability reduction.
  - Restoring broken equipment (`IsBroken == true`) clears the broken flag and restores combat attribute bonuses.

---

## 3. User Scenarios & Acceptance Criteria

### User Story 1 - Browsing NPC Merchant Catalog (Priority: P1)
A player speaks with an NPC merchant in a town or outpost. The server opens the merchant shop window, providing the inventory of goods, prices, and accepted currency type.

**Acceptance Scenarios**:
1. **Given** a player speaking with an NPC with shop dialogue, **When** the shop interface is opened, **Then** the player receives the shop catalog and pricing configuration.
2. **Given** an invalid shop identifier, **When** requested, **Then** the server safely rejects the request without opening a shop interface.

---

### User Story 2 - Purchasing Goods with Multiple Currencies (Priority: P1)
A player selects a desired good from the active shop and purchases it. The server verifies sufficient balance in the target currency, checks for available inventory grid space, deducts the price, and delivers the item into the player's inventory bag.

**Acceptance Scenarios**:
1. **Given** a player with sufficient silver and free bag space, **When** purchasing an item priced in silver, **Then** silver is deducted and the item appears in the bag.
2. **Given** a player purchasing from a specialty shop using FuYuan or Repute, **When** the purchase completes, **Then** the specified currency attribute is decremented while silver remains untouched.
3. **Given** a player with insufficient currency, **When** attempting a purchase, **Then** the transaction is rejected with an error and no state is mutated.
4. **Given** a player whose inventory bag has no free footprint for the item, **When** attempting a purchase, **Then** the transaction is rejected with an inventory full notification.

---

### User Story 3 - Selling Items Back to NPC Merchant (Priority: P1)
A player sells an unwanted inventory item to the shop for silver. The server verifies item ownership, eligibility, and applies the standard sell-back scaling divisor.

**Acceptance Scenarios**:
1. **Given** an eligible equipment or consumable in the player's bag, **When** the player sells it to the merchant, **Then** the item is removed and silver equal to `Price / BUY_SELL_SCALE` is credited.
2. **Given** a soul-bound or quest item, **When** attempting to sell, **Then** the transaction is rejected.

---

### User Story 4 - Equipment Repair & Durability Restoration (Priority: P1)
A player requests repair on damaged equipment from a blacksmith. The server computes the repair cost, deducts the silver, restores current durability, and unbreaks broken gear.

**Acceptance Scenarios**:
1. **Given** a damaged item, **When** standard repair is performed, **Then** repair fee is deducted and durability is restored to max.
2. **Given** a golden equipment item, **When** costly advanced repair is performed, **Then** maximum durability is preserved intact.
3. **Given** a broken item with 0 durability whose bonuses were disabled, **When** repaired, **Then** the broken status is cleared and combat bonuses are re-enabled.

---

## 4. Edge Cases

- **Concurrent Purchasing Near Capacity Limit**: When buying items rapidly, grid space reservations must be strictly serialized on the zone thread.
- **Zero or Negative Money Overflow**: Arithmetic checks prevent integer overflow or negative balances on transactions.
- **Selling Equipped Items**: Client cannot sell items directly from equipment slots without first moving them to bag inventory.

---

## 5. Functional Requirements

- **FR-001**: Implement `NpcShopZoneModule : IZoneModule` registering command routes for buy, sell back, and repair.
- **FR-002**: Maintain active shop session in `PlayerShopSessionSlot` on `PlayerZoneState`.
- **FR-003**: Support multi-currency deductions across Gold, FuYuan, Repute, Honor, BattleScore, and Coin.
- **FR-004**: Calculate item sell-back value using `ItemTemplate.PriceGold / ShopConsts.BuySellScale` with min 1 silver.
- **FR-005**: Support normal and advanced golden equipment repair using `EquipmentRepairCalculator`.

---

## 6. Success Criteria

- **SC-001**: NPC shop browsing, purchasing, selling, and repair operations execute with 100% deterministic success in offline unit and integration tests.
- **SC-002**: Differential parity tests (J09) pass cleanly against live reference server when `JX_ORACLE_HOST` is configured, and skip gracefully when offline.
- **SC-003**: 0 warnings, 0 errors under Release `-warnaserror` compilation.
