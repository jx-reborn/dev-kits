# Feature Specification: Personal City Stalls & Atomic Market Buyout

**Feature Branch**: `022-module-player-stalls`  
**Created**: 2026-11-15  
**Status**: Specification Complete  
**Input**: Player personal stall setup in town safe zones, 1..60 items pricing, TCVN3 advertisement banner, AOI billboard broadcasting (`PLAYER_MENU_STATE_STALL` = 5 via 0x76), stall catalog browsing, atomic buyout, and regional city tax deduction (10% Cities, 6% Villages, 3% Sects).

---

## 1. Overview & Context

Personal vending stalls ("Bày Bán") represent the decentralized marketplace of JX Reborn. In safe town districts, players set up stalls to sell equipment, gems, and consumables while away or actively trading:
- **Safe Town Setting**: Stalls can only be established in safe non-combat zones (`FightMode == 0`) by characters of Level >= 10.
- **Advertising & Posture**: A stalling player assumes the stall posture (`PLAYER_MENU_STATE_STALL = 5`) and broadcasts a custom advertisement banner (up to 32 bytes in TCVN3) to observers in the 3x3 region Area of Interest.
- **Fair Market Buyout**: Browsers view the seller's catalog and buy items directly. The server atomically transfers silver from buyer to seller, applies regional transaction tax, and transfers item ownership without risk of item duplication.

Architecturally, `022-module-player-stalls` is implemented as an isolated plug-in module (`PlayerStallZoneModule : IZoneModule`) adhering to the Golden Reference Open/Closed pattern established by `016-module-chat-channels`.

---

## 2. Clarifications & Ground-Truth Context

- **Eligible Zones & Restrictions**:
  - Requires `FightMode == 0` (safe town zone).
  - Minimum player level: 10 (`server1/settings/taxrates.ini`).
  - Max priced items per stall: 60 (`MAX_STALL_ITEM = 60`).
  - Stall banner text: Up to 32 bytes (`TCVN3`).
- **Regional Market Tax Matrix**:
  - 7 Major Cities (Thành Đô, Phượng Tường, Biện Kinh, Tương Dương, Dương Châu, Đại Lý, Lâm An): **10%**.
  - 8 Novice Villages (Giang Tân, Ba Lăng, Vĩnh Lạc, Chu Tiên, Đạo Hương, Thạch Cổ, Long Tuyền): **6%**.
  - 10 Sects (Nga Mi, Đường Môn, Thiên Nhẫn, Thiên Vương, Võ Đang, Thiếu Lâm, Cái Bang, Côn Lôn, Thúy Yên, Ngũ Độc): **3%**.
  - Other safe areas: **10%**.
  - Formula: $\text{Tax} = \lfloor \frac{\text{Price} \times \text{TaxRate}}{100} \rfloor$; $\text{NetSellerProceeds} = \text{Price} - \text{Tax}$.
- **Sub-Protocol Codes (`0x7C` C2S / `0x9D` S2C)**:
  - `0`: Mark Item Price / Buy Result.
  - `1`: Set Banner / Echo Banner.
  - `2`: Buy Item / View Catalog.
  - `3`: Close Stall / Self Price Sync.
  - `4`: Request Banner / Close Notification.
  - `5`: Request Catalog / Level Rejection.
  - `6`: Request Tax Rate / Sell Success Notification.
  - `7`: Map Tax Rate Sync.

---

## 3. User Scenarios & Acceptance Criteria

### User Story 1 - Setting Up Stall & Pricing Goods (Priority: P1)
A level 10+ player in a town safe zone sets up a stall, marks prices on inventory items, and sets an advertisement banner.

**Acceptance Scenarios**:
1. **Given** a level 10+ player in a safe zone, **When** opening a stall and pricing goods, **Then** the stall state becomes active, movement is locked, and the stall banner is set.
2. **Given** a player in a combat zone or below level 10, **When** attempting to stall, **Then** the request is rejected.

---

### User Story 2 - AOI Stall Perception (Priority: P1)
Nearby observers in the 3x3 region Area of Interest observe the seller's stall billboard and stall posture.

**Acceptance Scenarios**:
1. **Given** an active stall, **When** observers enter the AOI, **Then** they receive `0x76` `s2c_syncmenustate` with `m_btState = 5` and the advertisement banner.

---

### User Story 3 - Browsing Stall Catalog & Atomic Buyout (Priority: P1)
A prospective buyer inspects a stall's items and purchases a listed item.

**Acceptance Scenarios**:
1. **Given** an open stall, **When** an observer requests the catalog (`SubType 5`), **Then** the server returns the list of priced items.
2. **Given** a buyer with sufficient silver and inventory space, **When** purchasing an item (`SubType 2`), **Then** the price is deducted from the buyer, the regional city tax is withheld, the net proceeds are credited to the seller, and the item is transferred to the buyer's bag.
3. **Given** two simultaneous buyers attempting to buy the exact same item, **Then** exactly one buyer succeeds and the second buyer receives an "Already Sold" error with zero asset corruption.

---

### User Story 4 - Closing Stall (Priority: P1)
The stall owner exits stall mode. The stall closes, movement unlocks, and the stall billboard is cleared from the AOI.

**Acceptance Scenarios**:
1. **Given** an active stall, **When** the seller closes it, **Then** `m_btState` reverts to 0 and nearby observers perceive the stall closure.

---

## 4. Edge Cases

- **Seller Disconnects**: `OnPlayerDetached` automatically dismantles the stall and clears priced items.
- **Inventory Full on Buyout**: If buyer's bag cannot fit the item's dimensions, transaction is aborted before silver is deducted.

---

## 5. Functional Requirements

- **FR-001**: Implement `PlayerStallZoneModule : IZoneModule` registering stall commands.
- **FR-002**: Track stall state in `PlayerStallSessionSlot : IPlayerFeatureSlot`.
- **FR-003**: Broadcast `PLAYER_MENU_STATE_STALL = 5` and banner in 3x3 AOI via `0x76` `SyncMenuStateS2CPacket`.
- **FR-004**: Calculate and deduct regional tax based on `IStallTaxCatalog`.
- **FR-005**: Execute buyout atomically to prevent duplication during race conditions.

---

## 6. Success Criteria

- **SC-001**: Stall setup, browsing, and atomic buyout pass 100% in offline unit and integration tests.
- **SC-002**: Differential parity tests (J14) pass cleanly against live reference server when `JX_ORACLE_HOST` is configured, and skip gracefully when offline.
- **SC-003**: 0 warnings, 0 errors under Release `-warnaserror` compilation.
