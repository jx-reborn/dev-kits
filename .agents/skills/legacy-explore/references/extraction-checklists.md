# Domain-Specific Extraction Checklists (Tier 2 Deep Trace)

When performing a **Tier 2 (Deep Trace)** or **Tier 3 (Engine Trace)** exploration, select every checklist below that matches the target feature and extract all applicable items from verified files. Any item not yet read must be listed explicitly under **Open Gaps / Unverified**.

---

## Checklist A: NPC, Dialog, Quest & Task Systems
1. **Entry & Spawn**:
   - Which NPC script handles interaction (`main()`, `OnTimer()`, `OnDeath()`)?
   - Where is the NPC spawned (`server1/npcinfo.lua`, map startup scripts, `AddNpc` / `AddNpcEx`, or static map `.dat`)? What is its `npcs.txt` template ID and `MapID, X, Y`?
2. **Dialog Tree & Callbacks**:
   - Every menu option in `Say(...)`, `Describe(...)`, or `CreateNewSayEx(...)`, the condition showing/hiding each option, and the exact callback function invoked (`"Label/FuncName"` or `{ "Label", Func, {args} }`).
3. **Eligibility & Daily/Weekly Quotas**:
   - Level, faction (`GetLastFactionNumber`), camp, TransLife (`ST_GetTransLifeCount`), PK, money/item prerequisites.
   - Daily/weekly reset logic (`GetLocalDate("%y%m%d")` / `"%Y%m%d"`, streak/cap TaskIDs).
4. **Complete TaskID & Bit/Byte State Machine**:
   - List **every** `GetTask` / `SetTask` / `GetBit` / `SetBit` / `GetByte` / `SetByte` / `GetTaskTemp` ID touched.
   - Document exact meaning of each byte (`GetByte(val, 1..4)`) or state value (`0, 1, 2...`), and which function transitions from state A → B.
5. **Sub-Types / Branches & Completion Verification**:
   - If the quest has multiple types/branches, document **each branch** (which `.txt` table it reads, how rows are weighted/selected, and exact checks when turning in: item removal `ConsumeEquiproomItem` / `DelItem`, money `Pay`, stats, etc.).
6. **Rewards, Cancellations & Penalties**:
   - Exact EXP/money/item reward formulas, milestone bonuses, random award tables (`awardtemplet.lua` / `.txt`), cancellation costs, and failure penalties.
7. **Custom Modifications vs Base Logic**:
   - Highlight custom config variables (e.g. `cfg_server.lua`), custom TaskIDs, or custom shortcuts added on top of legacy scripts.

---

## Checklist B: Items, Boxes & Consumables (`magicscript` / `questkey`)
1. **Catalog Identity**:
   - Exact `(Genre, Detail, Particular, Level, Series)` in `server1/settings/item/004/magicscript.txt` (or `questkey.txt` / `goldequip.txt`), plus `TabFileRow`, `Name`, stackability, and `Script` path.
2. **Use Lifecycle (`main(nItemIdx)`)**:
   - Prerequisite checks: level, map restrictions (`GetWorldPos`, `check_map`), bag free cells (`CalcFreeItemCellCount`), tired state (`GetTiredDegree`), expiration date (`ITEM_GetExpiredTime` or `GetItemParam`), cooldown TaskIDs.
   - **Consumption semantics**: Verify every `return` path! In `legacy` item scripts, `return 0` **consumes/deletes** 1 item, whereas `return 1` **keeps** the item in the player's inventory without consuming it.
3. **Item Parameters & Tooltip**:
   - `GetItemParam(nItemIdx, 1..6)` / `SetSpecItemParam` encoding and `GetDesc(nItemIdx)` formatting.
4. **Reward / Loot Table**:
   - `tbAwardTemplet:GiveAwardByList(...)` structure: exact rates (`nRate`), expiration (`nExpiredTime`), bind state (`nBindState`), announcements (`Msg2SubWorld` / `AddGlobalNews`), and `WriteLog` audit format.

---

## Checklist C: Missions, Instanced Events & Battles (Tống Kim, Vượt Ải, Liên Đấu...)
1. **Schedule & Trigger (`s3relay` → `server1`)**:
   - Relay task schedule in `gateway/s3relay/relaysetting/task/` (`.ini` + `.lua`), `GlobalExecute` / `RemoteExecute` entry point in `server1/script/`.
2. **Mission Constants & State**:
   - `MISSIONID`, all `MS_STATE` values (signup, prep, active combat, settlement, close), `MissionV` (`GetMissionV`/`SetMissionV`) indices, and `MissionS` string slots.
3. **Timers**:
   - Timer IDs registered in `server1/settings/timertask.txt`, frame intervals (`18 frames = 1 second` in JX1 engine!), and total phase durations.
4. **Maps, Traps & Revive Points**:
   - Participating `MapID`s, signup NPCs, camp assignments (`SetCurCamp`, `SetTmpCamp`), trap scripts, death handlers (`SetDeathScript`), and logout/disconnect cleanup (`OnLeave`, `SetLogoutRV`).
5. **Scoring, Victory & Settlement**:
   - Point calculation, rank thresholds, win/draw/loss conditions, ladder updates (`RelayLadder`), and reward distribution.

---

## Checklist D: Skills, Buffs & Combat Mechanics
1. **`skills.txt` & `missles.txt` Schema**:
   - Skill ID, `SkillStyle`, `LvlSetScript` (`server1/script/skill/*.lua`), target/range/cast parameters, missile linkage.
2. **Lua Skill Scaling (`GetSkillLevelData`)**:
   - Exact `magicattrib` names returned per skill level and mathematical curve formulas (`Link`/`Line`/`Param1..3`) for levels `1..20+`, synergy (`addskilldamage1..6`).
3. **Engine Combat / Attribute Execution (Tier 3 if needed)**:
   - How `jx_linux_y` evaluates the specific `magicattrib` enum if Lua only supplies parameters.

---

## Checklist E: Native Engine & Preload `.so` Boundary (Tier 3)
1. **Lua C-API Registration**:
   - Exact C++ function registered for the Lua call in `jx_linux_y` (or intercepted in `server1/*.so` listed in `preload.txt`).
2. **Data Structures & Constants**:
   - Verified offsets, caps, or math formulas from decompiled C++ (`rea` MCP).
3. **Wire Protocol (if client-visible)**:
   - Exact C2S / S2C opcode, fixed vs variable packet length, and field-by-field byte offsets.
