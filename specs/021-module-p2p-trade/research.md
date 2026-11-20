# Research & Ground-Truth Analysis: Spec 021 Zero-Dupe P2P Trade

## 1. Legacy 6.0 Wire Protocol & Trade State Machine

### A. Trade Request & Acceptance
- `0x6B` (`c2s_tradeapplystart`, 9 bytes C2S): Target NpcId + anti-cheat value `0x451Fu`.
- `0x71` (`s2c_tradeapplystart`, 9 bytes S2C): Target player notification (`nDestIdx`, `dwApplicantNpcId`).
- `0x75` (`c2s_tradereplystart`, 10 bytes C2S): Decision (1=Accept, 0=Decline) + anti-cheat value `0x67CAu`.
- `0x75` (`s2c_tradechangestate`, 6 bytes S2C): State switch (`m_btState = 2`, `m_dwNpcID`).

### B. Money & Item Offerings
- `0x6C` (`c2s_trademovemoney`, 5 bytes C2S): `int m_nMoney`.
- `0x72` (`s2c_trademoneysync`, 5 bytes S2C): Partner's money sync.
- Item movement: Uses `0x5B` (`c2s_playermoveitem`) targeting `pos_traderoom = 5` (10 width x 4 height = 40 cells).

### C. Decision & Locking Protocol
- `0x6D` (`c2s_tradedecision`, 2 bytes C2S):
  - `0`: Cancel.
  - `1`: Confirm (OK).
  - `2`: Lock.
  - `3`: Unlock.
  - `4`: Cancel OK.
- `0x78` (`s2c_tradedecision`, 2 bytes S2C): Partner decision echo.
- `0x77` (`s2c_tradepressoksync`, 5 bytes S2C): `(SelfLock, DestLock, SelfOk, DestOk)`.
- `0x8E` (`s2c_itemexchangefinish`, 1 byte S2C): Trade finalized notification.

---

## 2. Anti-Dupe Safeguards Architecture

- In-memory negotiation executes on the 18-Hz simulation loop.
- When both players confirm, execution executes in `ITradeAppService.ExecuteTradeSwapAsync` inside an atomic transaction:
  1. Validates monotonic `LeaseEpoch` of Character A matches current active lease.
  2. Validates monotonic `LeaseEpoch` of Character B matches current active lease.
  3. Verifies both characters have the offered items and money.
  4. Swaps assets and persists audit log (`TradeAuditLog`).
