# Feature Specification: Two-Phase Zero-Dupe P2P Player Trade

**Feature Branch**: `021-module-p2p-trade`  
**Created**: 2026-11-15  
**Status**: Specification Complete  
**Input**: 2-phase secure peer-to-peer player trading, mutual lock/confirm states, LeaseEpoch fencing, transactional asset swap, TradeAuditLog persistence, and disconnect rollback.

---

## 1. Overview & Context

Direct player-to-player trading is a vital component of the MMORPG economy. However, concurrent race conditions, packet manipulation, or sudden network disconnects have historically been the primary attack vectors for item duplication exploits.
- **Two-Phase Commit Protocol**: Trade progresses strictly through distinct, observable phases: Application $\rightarrow$ Offer Placement $\rightarrow$ Mutual Lock $\rightarrow$ Mutual Confirmation $\rightarrow$ Final Atomic Swap.
- **Modification Guard**: Once locked, any attempt to add, remove, or modify items or silver instantly breaks the lock for both participants.
- **Zero-Dupe Assurance**: The swap is executed atomically on the control plane inside a transactional boundary, validating active `LeaseEpoch` fencing for both characters. Disconnections during active trading trigger clean inventory rollback without item creation or loss.

Architecturally, `021-module-p2p-trade` is implemented as an isolated plug-in module (`P2PTradeZoneModule : IZoneModule`) adhering to the Golden Reference Open/Closed pattern established by `016-module-chat-channels`.

---

## 2. Clarifications & Ground-Truth Context

- **Trade Room Dimensions**:
  - `pos_traderoom` = 5: 10 columns x 4 rows = 40 cells (`TRADE_ROOM_WIDTH = 10`, `TRADE_ROOM_HEIGHT = 4`, `MAX_TRADE_ITEM = 40`).
- **Protocol State Machine (`c2s_tradedecision`, opcode `0x6D`)**:
  - `0`: Cancel / exit trade.
  - `1`: Confirm trade (OK).
  - `2`: Lock trade.
  - `3`: Unlock trade.
  - `4`: Cancel OK.
- **Trade Sync Packets**:
  - `0x77` (`s2c_tradepressoksync`): `(SelfLock, DestLock, SelfOk, DestOk)`.
  - `0x72` (`s2c_trademoneysync`): Partner's offered silver.
  - `0x8E` (`s2c_itemexchangefinish`): Final completion notification.
- **Audit Logging**:
  - Every completed swap produces a `TradeAuditLog` recording timestamp, both character IDs, item IDs swapped, silver transferred, and lease epochs.

---

## 3. User Scenarios & Acceptance Criteria

### User Story 1 - Initiating and Accepting Trade (Priority: P1)
Player A targets Player B and invites them to trade. Player B accepts the invitation, opening the bilateral trade interface for both players.

**Acceptance Scenarios**:
1. **Given** two nearby players, **When** Player A invites Player B and Player B accepts, **Then** both players transition to the trading state and open trade rooms.
2. **Given** a player who is already trading or dead, **When** invited, **Then** the trade invitation is rejected.

---

### User Story 2 - Offering Items and Silver (Priority: P1)
Both players place items from their bag into their respective trade windows and enter offered silver amounts.

**Acceptance Scenarios**:
1. **Given** an open trade session, **When** Player A moves an item into `pos_traderoom`, **Then** the item is reserved and reflected in Player B's view.
2. **Given** Player A offering silver, **When** submitted, **Then** the silver amount is verified against Player A's bag balance and synchronized to Player B.

---

### User Story 3 - Mutual Lock & Confirmation (Priority: P1)
Both players review offerings and press Lock. Once both are locked, neither player can modify their offerings. Both players then press Confirm.

**Acceptance Scenarios**:
1. **Given** both players locked, **When** both players confirm, **Then** the atomic swap executes.
2. **Given** Player A locked and Player B modifying silver or items, **Then** Player A's lock is broken and resets to unlocked.

---

### User Story 4 - Atomic Swap & Audit Trail (Priority: P1)
When mutual confirmation is achieved, the server executes the swap atomically. Items and silver are swapped between participants, and an audit record is created.

**Acceptance Scenarios**:
1. **Given** a confirmed trade, **When** swapped, **Then** Player A receives Player B's offerings and Player B receives Player A's offerings.
2. **Given** insufficient bag space for incoming items, **When** swap executes, **Then** the trade rolls back with no asset transfer.

---

### User Story 5 - Abrupt Disconnect Rollback (Priority: P1)
If either player loses connection during trading, all placed items and silver immediately return to their respective owners.

**Acceptance Scenarios**:
1. **Given** items in the trade room, **When** a player disconnects, **Then** `OnPlayerDetached` rolls back items to the owner's bag with zero item duplication or loss.

---

## 4. Edge Cases

- **Concurrent Reconnect & Trade**: Lease epoch fencing ensures that if a player session reconnects and obtains a new lease epoch, any pending swap on the previous epoch is rejected.
- **Trade Cancellation Race**: If Player A confirms while Player B cancels, cancellation takes immediate precedence and no assets transfer.

---

## 5. Functional Requirements

- **FR-001**: Implement `P2PTradeZoneModule : IZoneModule` registering trade commands.
- **FR-002**: Track trade state machine in `PlayerTradeSessionSlot : IPlayerFeatureSlot`.
- **FR-003**: Enforce 2-phase lock/confirm transitions with instant lock invalidation upon alteration.
- **FR-004**: Execute final swap via control plane `ITradeAppService.ExecuteTradeSwapAsync` verifying `LeaseEpoch` and logging `TradeAuditLog`.
- **FR-005**: Rollback all offerings on detachment or cancellation.

---

## 6. Success Criteria

- **SC-001**: Trade lifecycle passes 100% in offline unit and integration tests with zero duplication.
- **SC-002**: Differential parity tests (J13) pass cleanly against live reference server when `JX_ORACLE_HOST` is configured, and skip gracefully when offline.
- **SC-003**: 0 warnings, 0 errors under Release `-warnaserror` compilation.
