# Specification: Guilds/Tong System (Spec 024 / Parity Journey J16)

## Summary
Implements classic guild (Tong/Bang Hội) creation, hierarchical rank management (Master, Director, Manager, Member), automated treasury upkeep EXP conversion, isolated guild chat channel 6, and wire protocol synchronization for JXReborn.

## Core Rules & Formulas
1. **Guild Creation Requirements**:
   - Master Character Level $\ge 50$.
   - Master Leadership Level $\ge 20$.
   - Token Item: *Nhạc Vương Kiếm* (`Genre = 4`, `DetailType = 195`), consumed on creation.
   - Cash Fee: 1,000,000 silver, deducted from inventory.
   - Alignment: Initial player camp must be `CampKind.Free` (`4`). Master chooses guild alignment (`Justice=1`, `Evil=2`, `Balance=3`).
   - Guards: Must not already be in a guild or party; guild name must be $\le 8$ characters and globally unique.
2. **Hierarchy & Authority**:
   - `Master` (Bang Chủ, rank 3): 1 per guild. Full administrative control.
   - `Director` (Trưởng Lão, rank 2): Max 7 slots. Appointed by Master. Can instate and kick Managers/Members.
   - `Manager` (Đường Chủ, rank 1): Max 56 slots. Appointed by Master or Director.
   - `Member` (Bang Chúng, rank 0): Standard member.
3. **Automated Treasury Upkeep**:
   - Upkeep Interval: Every 750 seconds (13,500 frames @ 18Hz).
   - If `TreasuryMoney >= 1,000,000`:
     - Upkeep Fee: $5000 + \max(0, \text{MemberCount} - 100) \times 50$.
     - EXP Awarded: $+120$ guild experience points.
   - If `TreasuryMoney < 1,000,000`:
     - Upkeep Fee: $0$ (no deduction).
     - EXP Awarded: $0$ (no EXP gained).
4. **Chat Channel 6 Isolation**:
   - Messages sent on `ChatChannelKind.Tong` (`6`) are strictly routed only to online members belonging to the same `GuildId`.
5. **Wire Protocol**:
   - Opcode `0x9A` (`c2s_tongextend`), `0xA5` / `0xA8` (`c2s_tongexmsg`), `0xFD` (`c2s_extendtong`).
   - Opcode `0xA0` (`s2c_tongcreate`), `0xC3` (`s2c_tongexmsg`), `0xFD` (`s2c_extendtong`).
