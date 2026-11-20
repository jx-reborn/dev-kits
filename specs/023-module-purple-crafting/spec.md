# Specification: Purple Equipment Crafting & Enchasing (Spec 023 / Parity Journey J15)

## Summary
Implements authoritative Purple Equipment Crafting and Enchasing for JXReborn following original sword hero mechanics: XuanJing upgrading (3 stones of level $L$ into level $L+1$), magic property ore extraction ("hút khoáng thạch"), blank purple equipment base forging, sequential 6-line purple equipment enchasing with `EnchaseLevel` persistence and durability preservation, and wire protocol communication via opcodes `0x92` (c2s_foundry_request 204B), `0x7F` (c2s_enchaseritem 50B), `0xA0` (s2c_foundry_result 7B), `0x94`, and `0x95`.

## Core Rules & Formulas

### 1. XuanJing Creation & Upgrading
1. **Creation**:
   - Converts white/blue jewelry (rings, necklaces, pendants) into XuanJing stones Level 1..10.
   - Processing fee: 1,000 silver.
2. **Upgrading (`ore_upgrade.lua`)**:
   - Upgrades 3 stones of same type and level $L$ into 1 stone of level $L+1$.
   - Maximum level: Level 10 stones cannot be upgraded further; returns `RESULT_LEVEL_FULL` (3).
   - Fee: 5,000 silver.
   - Success probability formula:
     $$P_{\text{success}} = \frac{\frac{V_{\text{src}}}{3}}{V_{\text{des}} - \frac{2}{3} V_{\text{src}}}$$
     Where $V_{\text{src}}$ is the total value of input stones and catalysts, and $V_{\text{des}}$ is the target stone value from `ore.txt`.
   - **Failure Safeguard**: On failure, **only stone 1 and catalysts are consumed; stones 2 and 3 are retained safely in player inventory**.

### 2. Magic Property Ore Extraction ("Hút Khoáng Thạch", `magic_distill.lua`)
1. **Requirements**:
   - Blue equipment base (`Genre == 0`).
   - Original Mine stone (`Genre == 6, DetailType == 1, Particular == 149..154`) corresponding to socket position $k \in [1..6]$.
   - Processing fee: 5,000 silver.
2. **Five-Elements Series Lock**:
   - If socket $k \in \{2, 4, 6\}$ (even sockets, representing Five-Elements hidden lines):
     $$\text{Equipment.Series} == \text{OrgMine.Series}$$
     Mismatch immediately returns `RESULT_SERIES_ERROR` (7).
   - If socket $k \in \{1, 3, 5\}$ (odd sockets, explicit lines): series is independent.
3. **Output**:
   - Generates Refined Magic Ore (`Genre == 6, DetailType == 1, Particular == 199 + k`, Level 1..10, retaining affix ID and rolled potency level).

### 3. Blank Purple Equipment Forging (`equip_compound.lua`)
1. Forges white/blue equipment into purple gear base (`Quality = 2`).
2. Sockets 1..6 initialized as unassigned (`arynMagLvl = -1`).
3. Processing fee: 10,000 silver.

### 4. 6-Line Sequential Purple Enchasing (`equip_enchase.lua`)
1. **Sequential Socket Order**:
   - Sockets must be enchased strictly in order 1 through 6. The target socket $j$ is the first socket where magic level is unassigned (`arynMagLvl[j] == -1`).
   - The Magic Ore particular must match $199 + j$. Non-sequential attempts return `RESULT_RULE_ERROR` (8).
2. **Five-Elements Series Lock**:
   - If target socket $j \in \{2, 4, 6\}$, `Equipment.Series == Ore.Series`. Mismatch returns `RESULT_SERIES_ERROR` (7).
3. **Duplicate Affix Prohibition**:
   - The affix ID of the Magic Ore must not already exist on any previous socket of the equipment. Duplicate affixes return `RESULT_MAGIC_ERROR` (6).
4. **Preservation of Durability & Gear**:
   - Current durability (`CurrentDurability`) and maximum durability (`MaxDurability`) are preserved 100%.
   - `EnchaseLevel` increments by 1 on success (capped at 10).
   - **Crucial Failure Rule**: On failure, **equipment is NEVER destroyed**; only the Magic Ore and optional catalysts are consumed. Returns `RESULT_FAIL` (1).

### 5. Wire Protocol & Blacksmith UI
1. Client sends `c2s_foundry_request` (`0x92`, 204 bytes) or `c2s_enchaseritem` (`0x7F`, 50 bytes).
2. Server responds with `s2c_foundry_result` (`0xA0`, 7 bytes: opcode 0xA0, UpgradeType, ResultData int32, ResultCode byte).
3. Server synchronizes item updates via `s2c_syncitem` (`0x5F`, 80 bytes).
4. UI lock / sleep state sent via `s2c_npcsleepmode` (`0x94`, 6 bytes).
