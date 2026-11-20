# Feature Specification: Repository Box, Secondary Password & Revival Binding

**Feature Branch**: `018-module-repository-box`  
**Created**: 2026-11-15  
**Status**: Specification Complete  
**Input**: Character storage box (`pos_repositoryroom` = 4, 6x10 grid), atomic silver deposit/withdraw with negative guards, secondary password security (`0x84` sub-opcodes 0..4), and revival point town binding.

---

## 1. Overview & Context

In JX Reborn, safe storage and account security are paramount to player asset protection:
- **Repository Box**: In every safe town, players can access their personal storage box (`pos_repositoryroom` = 4) to store surplus inventory items and keep silver safe from death-penalty drops.
- **Secondary Password (PIN)**: To defend against unauthorized withdrawals or compromised credentials, characters can protect their repository with a secondary password (hashed with MD5). Storage retrieval or silver withdrawal is locked until unlocked by the correct PIN.
- **Revival Point Binding**: Interacting with town guides and elders binds the player's respawn coordinates (`RevivalMapId`, `RevivalPointId`), ensuring that upon defeat or return, the character awakens at their chosen home town.

Architecturally, `018-module-repository-box` is implemented as an isolated plug-in module (`RepositoryBoxZoneModule : IZoneModule`) adhering to the Golden Reference Open/Closed pattern established by `016-module-chat-channels`.

---

## 2. Clarifications & Ground-Truth Context

- **Storage Layout**:
  - Main repository page: 6 columns x 10 rows (`[6, 10]`, 60 cells) identical in cell footprint to `pos_equiproom`.
- **Atomic Money Transfer**:
  - Deposit (`Direction = 0`): Deducts silver from `InventoryMoney` and credits `RepositoryMoney`.
  - Withdraw (`Direction = 1`): Deducts silver from `RepositoryMoney` and credits `InventoryMoney`. Requires box to be in an unlocked state if a PIN is configured.
  - Strict negative balance guard: `Amount <= 0` or amounts greater than available source funds are immediately rejected with zero balance change.
- **Secondary Password Operations (`c2s_boxoperate`, SubOpcodes 0..4)**:
  - `0 (Set)`: Sets initial 32-character MD5 password hash and unlocks box.
  - `1 (Unlock)`: Verifies candidate password against stored hash; unlocks box on match.
  - `2 (Lock)`: Explicitly relocks repository.
  - `3 (Change)`: Verifies old password; updates to new password hash.
  - `4 (Cancel)`: Verifies current password; clears password hash to null (unlocked by default).
- **Revival Point Binding**:
  - Talking to a town guide or village elder sets `RevivalMapId` and `RevivalPointId`.

---

## 3. User Scenarios & Acceptance Criteria

### User Story 1 - Accessing Repository Box (Priority: P1)
A player clicks an NPC or uses the storage dialogue. The server opens the repository window, displaying stored items and current repository silver balance.

**Acceptance Scenarios**:
1. **Given** a player in a town, **When** opening the storage box, **Then** the client receives the storage box open packet and current silver balances.

---

### User Story 2 - Storing and Retrieving Items (Priority: P1)
A player moves items between the inventory bag (`pos_equiproom` = 3) and the storage box (`pos_repositoryroom` = 4).

**Acceptance Scenarios**:
1. **Given** an unlocked repository box with sufficient grid cells, **When** the player moves an item into the repository, **Then** the item is removed from bag and placed into the repository grid.
2. **Given** a locked repository box, **When** attempting to withdraw an item from the repository, **Then** the action is rejected.

---

### User Story 3 - Atomic Silver Deposit & Withdrawal (Priority: P1)
A player deposits silver from their bag into the repository, or withdraws silver from the repository to their bag.

**Acceptance Scenarios**:
1. **Given** 10,000 silver in bag, **When** depositing 5,000 silver, **Then** inventory money becomes 5,000 and repository money increases by 5,000.
2. **Given** 5,000 silver in repository and an unlocked box, **When** withdrawing 2,000 silver, **Then** repository money becomes 3,000 and inventory money increases by 2,000.
3. **Given** an attempt to deposit or withdraw a negative or zero amount, **When** submitted, **Then** the transaction is rejected with no balance changes.
4. **Given** an attempt to withdraw more silver than stored in repository, **When** submitted, **Then** the request is rejected with an insufficient funds error.

---

### User Story 4 - Secondary Password Protection (Priority: P1)
A player sets a PIN to secure their repository box against unauthorized access.

**Acceptance Scenarios**:
1. **Given** no PIN set, **When** setting a new PIN, **Then** the MD5 hash is stored and subsequent sessions require unlocking.
2. **Given** a locked repository, **When** providing the correct PIN, **Then** the repository unlocks for the remainder of the session.
3. **Given** a locked repository, **When** providing an incorrect PIN, **Then** access is denied and the box remains locked.

---

### User Story 5 - Revival Point Town Binding (Priority: P1)
A player requests to set their home town respawn point at a village elder or town guard.

**Acceptance Scenarios**:
1. **Given** a player in Ba Lăng Huyện, **When** confirming revival point binding, **Then** `RevivalMapId` and `RevivalPointId` update to the town's respawn coordinates and persist to storage.

---

## 4. Edge Cases

- **Abrupt Disconnect While Storage Open**: On player detachment, repository session state is cleared; box reverts to locked on next login.
- **Concurrent Money Transfer Requests**: Serialized on the 18-Hz zone loop; atomic balance verification prevents double-spending or negative balances.

---

## 5. Functional Requirements

- **FR-001**: Implement `RepositoryBoxZoneModule : IZoneModule` registering command routes for money deposit/withdraw, box PIN operations, and revival binding.
- **FR-002**: Extend `PlayerInventoryGrid` to manage `_repositoryRoomGrid[6, 10]`.
- **FR-003**: Enforce secondary password lock guards before permitting withdrawals or item removals from storage.
- **FR-004**: Enforce strict negative and overflow bounds checking on all money transfers.

---

## 6. Success Criteria

- **SC-001**: Repository operations pass 100% in offline unit and integration tests.
- **SC-002**: Differential parity tests (J10) pass cleanly against live reference server when `JX_ORACLE_HOST` is configured, and skip gracefully when offline.
- **SC-003**: 0 warnings, 0 errors under Release `-warnaserror` compilation.
