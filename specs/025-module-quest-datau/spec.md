# Specification: 8,000 Dã Tẩu Quest Engine (Spec 025 / Parity Journey J16)

## Summary
Implements the classic 8,000 Dã Tẩu quest chain hierarchy ($20 \text{ Tasks} \times 20 \text{ Links} \times 20 \text{ Loops}$), procedural quest generation across 4 core categories (Progress Delta, Item Delivery, Green Equipment Delivery with exact affix matching, Monster Kill Hunts), regional treasure hunt coordinate matching with `trea_pos.txt` tolerances, declarative milestone rewards (10..8000) from `award_link.txt`, non-cancellation streak preservation, fast-complete rules, and 3-way `Prise` reward selection via wire opcodes `0x80`, `0x8C`, and `0x5F`.

## Core Rules & Formulas

### 1. Quest Chain Hierarchy & Progression
1. **3-Tier Hierarchy**:
   - 1 Loop = 20 Links.
   - 1 Link = 20 Tasks.
   - Total Chain = 20 Loops $\times$ 20 Links $\times$ 20 Tasks = 8,000 Tasks.
2. **Streak Formula**:
   $$\text{Streak} = \text{Loops} \times 400 + \text{Links} \times 20 + \text{Tasks} + 1$$
3. **Daily Quotas**:
   - Standard daily quota: 40 tasks.
   - Extended quota: Expandable via Kim Nguyên Bảo (KNB) redemption.

### 2. Quest Categories & Validation
1. **Item Delivery (`BuyGoods`, `FindMaps`, `WorldMaps`)**:
   - Directs player to submit designated consumables or map fragments (Sơn Hà Xã Tắc).
   - Target items verified and consumed atomically from player bag.
2. **Green Equipment Delivery (`FindGoods`)**:
   - Demands green equipment piece matching `Genre`, `Detail`, `Particular`, `Series`, and `Level`.
   - Requires exact magic attribute line matching specified `MagicEnName` within $[\text{MinValue}, \text{MaxValue}]$.
   - Item consumed upon turn-in.
3. **Equipment Showing (`ShowGoods`)**:
   - Requires presenting green/blue equipment meeting affix criteria.
   - Equipment verified in inventory and NOT consumed.
4. **Progress Delta (`UpGround`)**:
   - Records baseline snapshot at acceptance: EXP, Repute, FuYuan, PK reduction, or Song-Jin battle points.
   - Requires achieving target increase $\Delta$: $\text{Current} - \text{Baseline} \ge \text{TargetDelta}$.
5. **Monster Kill Hunts (`WorldMaps` / `KillMonsters`)**:
   - Defeating specified regular monsters (e.g. 99 kills) or elite/boss target in designated map zone.
6. **Regional Treasure Hunt (`trea_pos.txt`)**:
   - Verifies item delivery or digging coordinate against 245 canonical positions.
   - Matching tolerance:
     $$|\text{TreaPos.WorldX} - \text{Player.WorldX}| \le 6 \quad \text{and} \quad |\text{TreaPos.WorldY} - \text{Player.WorldY}| \le 9$$

### 3. Milestone Rewards & Declarative Table (`award_link.txt`)
1. **Turn-In Rewards (`Prise`)**:
   - 3 randomly generated options presented via dialog: Money, EXP, Mystery Chest, Reroll Chance, or Cancellation Privilege.
2. **Fixed Milestone Rewards**:
   - Every 40 tasks awards fixed milestone tier items.
   - Milestone 40: Level 90 Skill Book (`6, 1, 2426`).
   - Milestone 1000..3000: Full Định Quốc Set (Hộ Uyển, Yêu Đái, Nhuyễn Ngoa, Trường Sam, Phát Quán).
   - Milestone 3280..3800: An Bang Set (Kê Huyết Thạch, Điền Hoàng Thạch, Cúc Hoa Thạch, Băng Tinh Thạch).
   - Milestone 8000: Càn Khôn Liên Hoa (`6, 1, 5123`) and global server announcement broadcast.
   - Inventory guard: Requires $\ge 6$ free bag cells to accept milestone rewards.

### 4. Cancellation & Fast-Complete Rules
1. **Streak Preservation**:
   - Canceling with "Lệnh bài Dã Tẩu" or Cancellation Privilege (Award Type 5) consumes 1 privilege and **preserves streak intact**.
   - Canceling with 100 Sơn Hà Xã Tắc fragments consumes 100 fragments and preserves streak.
2. **Normal Cancellation Penalty**:
   - Canceling without tokens resets `TotalCompletedCount` and `DailyStreak` to 0.
   - Enforces 10-minute punishment cooldown before player can accept new quests.
3. **Fast-Complete**:
   - Consumes Thủy Tinh / Tinh Hồng Bảo Thạch / Tiền Đồng / silver.
   - Preserves streak and advances chain progression. Capped at daily limit (30).
