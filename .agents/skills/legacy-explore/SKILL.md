---
name: legacy-explore
description: >-
  Explore, trace, and document gameplay features, NPCs, quests/tasks, items, skills,
  missions, scheduled relay events, and native C++ engine boundaries in legacy
  (legacy-server). Use when the user asks how a specific legacy mechanic works,
  where a feature is implemented across Lua scripts, .txt/.ini settings, s3relay,
  or native binaries, or wants a verified technical breakdown before porting or modifying logic.
---

# `legacy` Feature Exploration Skill

Systematically trace any feature in `legacy-server` (resolved via `$env:LEGACY_SERVER_DIR` or sibling `../legacy-server`, per `docs/DEVELOPMENT.md`) from entry points (`server1/script/`, `server1/settings/`, `gateway/s3relay/`) down to state machines, data tables, and (when needed) C++ engine binaries—with **zero guessing**.

---

## 1. Strict Grounding Rules ("No Read, No Claim")

1. **Zero Generic JX1 Memory**: Never assume TaskIDs, formulas, item IDs, or quest rules from general legacy JX1 knowledge. `legacy-server` contains extensive custom edits—only report what is present on disk.
2. **Evidence Required for Every Claim**: Every file path, function behavior, TaskID, MissionV, `(G, D, P)` item tuple, formula, or opcode MUST come from tool output observed in the current session, cited as `[filename](file://<resolved-legacy-root>/...#L10-L25)` using the absolute path printed by the helper scripts.
3. **Signature ≠ Behavior**: Seeing a filename or `function foo()` in search results only proves existence. Never describe what `foo()` does until you read its body.
4. **Mandatory `Open Gaps` Section**: Never fill unread branches by inference. Place any unread helper, uninspected `.txt` table, or unverified C++ binding in **Open Gaps**.
5. **Never Guess Around Encoding or Wide `.txt` Tables**:
   - Many `.lua`, `.ini`, and `.txt` files mix **TCVN3 (ABC Vietnamese)**, **GB18030 (Chinese)**, and **UTF-8** in the same file, causing standard UTF-8 tools (`read_file`, `rg`) to fail or miss Vietnamese strings.
   - Always use [read_jx_file.py](./scripts/read_jx_file.py) to search/read mixed-encoding text and [read_tabfile.py](./scripts/read_tabfile.py) to map `.txt` columns deterministically (never count tabs manually).

---

## 2. Choose the Exploration Depth Tier

Default to **Tier 2** when the user asks about a specific feature, unless they ask for a quick overview (**Tier 1**) or C++/packet internals (**Tier 3**):

- **Tier 1 — Feature Map (Quick Overview)**: Entry files, core functions, primary `.txt`/`.ini` tables, main TaskIDs, and high-level execution flow.
- **Tier 2 — Deep Trace (Full Gameplay & Data Specification)**:
  - Complete dialog tree & menu callbacks.
  - Full state machine (every `TaskID`, `GetByte`/`SetByte` slot, `MissionV`, daily quota/streak).
  - Branch-by-branch logic, exact completion checks, and exact reward/cost/rate formulas.
  - Real column schemas + sample rows extracted via `read_tabfile.py`.
  - Distinction between base legacy script logic and custom server additions.
  - Cross-system hooks (`s3relay` schedules, `g_activity`, `RelayLadder`, `WriteLog`, item/death scripts).
- **Tier 3 — Engine & Wire Trace (C++ / Binary)**:
  - Crosses Lua C-API bindings or packet handlers into `server1/jx_linux_y`, `server1/*.so` (`preload.txt`), `gateway/s3relay/s3relay_y`, `bishop_y`, or `goddess_y` using `rea` MCP (Ghidra).
  - **Escalation Gate**: Do **not** invoke Ghidra if Lua + `.txt`/`.ini` fully defines the mechanic.

---

## 3. Multi-Pass Exploration Workflow

### Pass 1: Locate & Map Skeleton
1. Read [directory-map.md](./references/directory-map.md) if unsure where the feature lives (pay special attention to **`server1/settings/item/004/`** for active items and **`gateway/s3relay/relaysetting/task/`** for scheduled events/bosses).
2. Both helper scripts automatically resolve paths relative to `legacy-server` (using `$env:LEGACY_SERVER_DIR` or walking up for sibling `legacy-server/`). Search across `server1/script/`, `server1/settings/`, and `gateway/s3relay/` using Unicode Vietnamese, Chinese, or ASCII identifiers:
   ```powershell
   python .agents/skills/legacy-explore/scripts/read_jx_file.py search "Dã Tẩu" --path server1/script --glob "*.lua" -i
   ```
3. Index large `.lua` files before reading so no handler is missed:
   ```powershell
   python .agents/skills/legacy-explore/scripts/read_jx_file.py functions server1/script/global/seasonnpc.lua
   ```

### Pass 2: Branch-by-Branch Code & Table Extraction
1. Open [extraction-checklists.md](./references/extraction-checklists.md) and select the matching checklist(s) (NPC/Task, Item, Mission/Battle, Skill, or Engine).
2. Read each relevant Lua function body in focused line ranges (do not dump 1,500 lines at once):
   ```powershell
   python .agents/skills/legacy-explore/scripts/read_jx_file.py read server1/script/global/seasonnpc.lua --start 55 --end 180
   ```
3. Follow `Include(...)` files, custom config includes (e.g., `cfg_server.lua`), and `GlobalExecute` callers in `gateway/s3relay/`.
4. Inspect referenced `.txt` tables deterministically (schema first, then filtered/sampled rows):
   ```powershell
   python .agents/skills/legacy-explore/scripts/read_tabfile.py schema server1/settings/task/tasklink_buygoods.txt
   python .agents/skills/legacy-explore/scripts/read_tabfile.py rows server1/settings/item/004/magicscript.txt --where "*=Dã Tẩu" --max 10
   ```

### Pass 3: Native Boundary Check (Tier 3 Only)
- If a crucial rule lives inside an unscripted Lua C-API call (e.g., `ST_GetTransLifeCount`), a preload `.so` plugin (`server1/preload.txt`), or a C2S/S2C packet:
  - **For Linux ELF targets (`server1/jx_linux_y`, `server1/*.so`, `gateway/s3relay/s3relay_y`, `bishop_y`, `goddess_y`)**: On Windows hosts, `rea` MCP's Ghidra P0 rejects ELF (`target_format_unsupported`). Use [ghidra_elf_query.py](./scripts/ghidra_elf_query.py), which invokes Ghidra `analyzeHeadless` directly (with `-readOnly` on already-imported binaries) and caches the project under `artifacts/ghidra-jxlinux/` (`JxLinuxProj`, pre-populated with `jx_linux_y`, `bishop_y`, `goddess_y`, `s3relay_y` via `restore-dev-kit.ps1`):
    ```powershell
    python .agents/skills/legacy-explore/scripts/ghidra_elf_query.py --binary server1/jx_linux_y search-funcs "NewWorld|DoWalk|DoRun|DoJump"
    python .agents/skills/legacy-explore/scripts/ghidra_elf_query.py --binary server1/jx_linux_y decompile 0x<addr>
    python .agents/skills/legacy-explore/scripts/ghidra_elf_query.py --binary server1/jx_linux_y search-strings "NewWorld"
    python .agents/skills/legacy-explore/scripts/ghidra_elf_query.py --binary server1/jx_linux_y xrefs 0x<addr>
    ```
  - **For Windows PE targets (`game.exe`, client `.dll`s)**: Use `rea` MCP tools (`open_binary`, `search_procedures`, `procedure_pseudo_code`, `xrefs`).
  - Otherwise, list the C++ binding names as the engine boundary.

### Pass 4: Self-Audit Before Responding
Verify every single item in your draft report against tool outputs from Passes 1–3:
- [ ] Every file link and `#Lstart-Lend` line number matches actual `read_jx_file.py` / `read_tabfile.py` output.
- [ ] Every `TaskID`, `MissionV`, `(G, D, P)`, and formula was read from code/tables (zero assumed values).
- [ ] Item tables were checked in `settings/item/004/` (not just legacy `settings/item/`).
- [ ] Any function or table mentioned but not read in full is explicitly listed under **Open Gaps**.

---

## 4. Standard Output Structure

Format the exploration report in clean Markdown (matching the user's language, defaulting to Vietnamese if asked in Vietnamese):

1. **Overview & Execution Flow** — Concise summary + `mermaid` flowchart/state diagram when Tier 2+ involves multi-step transitions.
2. **Verified File & Function Map** — Table of verified `.lua`, `.txt`, `.ini`, and `s3relay` files with clickable `#L...` links.
3. **State Machine & Storage Variables (`TaskIDs` / `MissionV` / `ItemParams`)** — Exact IDs, byte/bit packing (`GetByte`/`SetBit`), state values, and where transitions occur.
4. **Detailed Business Logic, Formulas & Table Samples** — Dialog options, per-branch conditions, reward/drop formulas, custom server tweaks, and real `.txt` schema/row samples.
5. **Engine Boundary (Lua C-APIs / Preload `.so`)** — Lua C-API functions used (plus Tier 3 binary findings if performed).
6. **Open Gaps / Next Verification Targets** — Explicit list of unread branches, secondary files, or binary internals not yet inspected.
