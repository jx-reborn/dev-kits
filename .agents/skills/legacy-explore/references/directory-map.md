# `legacy-server` Architecture & Directory Map

This reference maps the runtime layout of `legacy-server` (`$env:LEGACY_SERVER_DIR` or sibling `../legacy-server`) across GameServer (`server1/`), Gateway/Relay (`gateway/`), data tables, and native binaries.

---

## 1. Top-Level Topology

| Directory / Binary | Role |
| :--- | :--- |
| `server1/` | Main GameServer instance: Lua scripts (`script/`), data tables (`settings/`), maps (`maps/`), game engine binary (`jx_linux_y`), and preload `.so` plugins. |
| `gateway/bishop_y` | Login/authentication gateway (`bishop.cfg`). Validates accounts and routes clients to `goddess_y` / `jx_linux_y`. |
| `gateway/goddess_y` | Role/character database server (`goddess.cfg`). Persists character blobs, TaskIDs, inventory, and role lists. |
| `gateway/s3relay/` | Cross-server coordinator (`s3relay_y`). Runs scheduled world tasks (`relaysetting/task/`), Tong/clan state, ladder rankings, chat channels, and broadcasts `GlobalExecute` / `RemoteExecute` calls back into `server1/`. |

---

## 2. `server1/script/` — Lua Gameplay Layer

| Subdirectory | Purpose |
| :--- | :--- |
| `global/` | Global NPC scripts (`seasonnpc.lua`, `fuyuan.lua`, login/logout hooks, death hooks, custom server configs such as `global/nobitaxd/`). |
| `task/` | Quest & progression frameworks: `newtask/tasklink/` (Dã Tẩu), `newtask/master/` & `branch/` (Chính tuyến / Phụ tuyến), `150skilltask/`, `lv120skill/`, `metempsychosis/` (Trùng sinh), `tollgate/` (Sát thủ / Tín sứ), `partner/`, `random/`. |
| `item/` | Right-click / usable item callbacks (`main(nItemIdx)`, `GetDesc(nItemIdx)`). Linked from the `Script` column in `settings/item/.../magicscript.txt` or `questkey.txt`. |
| `missions/` | Instanced / timed multi-player activities (Vượt Ải `challengeoftime`, Liên Đấu `leaguematch`, Phong Lăng Độ, Viêm Đế, Bạch Hổ Đường, etc.). |
| `battles/` | Tống Kim (Song Jin) battlegrounds: rules, timers, marshals, point & rank rewards. |
| `activitysys/` & `dailogsys/` | Declarative activity system (`g_activity.lua`, `playerfunlib.lua`) and modern dialog builder (`CreateNewSayEx`). |
| `event/`, `vng_event/`, `vng_feature/`, `sukien_nam/` | Seasonal events, legacy-specific features, and custom server mechanics. |
| `skill/` & `npclevelscript/` | Per-level skill attribute scaling formulas and dynamic monster stat scaling scripts. |
| `tong/` | Bang hội (clan/guild) scripts cooperating with `s3relay`. |
| `script_protocol/` | Scripted C2S/S2C custom protocol handlers bridged from Lua. |
| Regional folders (GBK-on-NTFS names) | Town/map NPC scripts grouped by geography (GBK bytes displayed in CP1252 on Windows): `¶«±±Çø` (Đông Bắc / 东北区), `½­ÄÏÇø` (Giang Nam / 江南区), `Á½ºþÇø` (Lưỡng Hồ / 两湖区), `Î÷±±±±Çø` (Tây Bắc Bắc / 西北北区), `Î÷±±ÄÏÇø` (Tây Bắc Nam / 西北南区), `Î÷ÄÏ±±Çø` (Tây Nam Bắc / 西南北区), `Î÷ÄÏÄÏÇø` (Tây Nam Nam / 西南南区), `ÖÐÔ­±±Çø` (Trung Nguyên Bắc / 中原北区), `ÖÐÔ­ÄÏÇø` (Trung Nguyên Nam / 中原南区). |

---

## 3. `server1/settings/` — Tab-Delimited Tables & Configs

> **CRITICAL ITEM VERSION TRAP (`settings/item/004/` vs `settings/item/`)**:
> `server1/settings/item/` contains versioned subdirectories (`000`..`004`) alongside root `.txt` files. Modern legacy / `legacy` items (`nItemVer = 4`) resolve against **`server1/settings/item/004/`** (e.g., `004/magicscript.txt`, `004/questkey.txt`, `004/goldequip.txt`, `004/potion.txt`). Always check `settings/item/004/` first, and verify whether `settings/item/` root differs if `AddItem` specifies an older version.

| File / Folder | Contents |
| :--- | :--- |
| `npcs.txt` | Master NPC & monster template table (ID = `TabFileRow - 2` or 1-based index depending on loader; always verify row offset against `AddNpc`/`AddNpcEx` calls!). |
| `item/004/magicscript.txt` | Scripted usable items (`Genre = 6, Detail = 1, Particular = ID`), linking to `Script` (`.lua`). |
| `item/004/questkey.txt` | Quest key items (`Genre = 6, Detail = 0` or `Genre = 4`). |
| `item/004/{meleeweapon,armor,helm,boot,belt,cuff,amulet,ring,pendant,horse,mask,mantle,signet,shipin,goldequip,platinaequip}.txt` | Equipment base stats, `magicattrib.txt` affixes, Hoàng Kim (`goldequip.txt`) & Bạch Kim (`platinaequip.txt`) definitions. |
| `skills.txt` & `missles.txt` | Skill IDs, cast modes, missile IDs, and links to `script/skill/*.lua`. |
| `buysell.txt` & `goods.txt` | NPC shop inventories (`buysell.txt` maps ShopID → list of `goods.txt` indices). |
| `maplist.ini` | Map IDs, map names, `.wor` folder paths. |
| `timertask.txt` & `systemtimetask.txt` | Timer ID → Lua script mappings triggered by `StartTimer` / `StartMissionTimer`. |
| `droprate/` & `item/*droprate.ini` | Monster and boss drop tables. |
| `task/` | TaskLink tables (`tasklink_*.txt`, `award_*.txt`, `levellink.txt`), `player_task_def.txt`, `missions.txt`. |

---

## 4. `gateway/s3relay/` — Scheduled Tasks & Cross-Server Logic

Many world features **do not start inside `server1/`** — they are triggered on schedule by `s3relay_y`:
- `gateway/s3relay/relaysetting/task/` (`tasklist.ini` and subfolders `boss`, `makegoldboss`, `tongkim`, `phonglangdo`, `congthanh`, `leaguematch`, `citydefence`, `sevencity`, `festivals`, `hoatdong`, `thongbaohethong`).
- Relay Lua scripts call `GlobalExecute("dwf \\script\\...\\file.lua func_name(...) ")` or `RemoteExecute(...)` to invoke functions inside `server1/script/`.
- Always check `gateway/s3relay/` if a `server1/script/` function has no local caller in `server1/`!

---

## 5. Native Binaries & Preload Hooks (Tier 3 Only)

| Binary | Location | Role |
| :--- | :--- | :--- |
| `jx_linux_y` | `server1/jx_linux_y` | Core C++ GameServer engine: Lua 4/5 C-API bindings (`IncludeLib`, `GetTask`, `AddItem`, etc.), combat loop, sync packets, map/region grid. |
| Preload `.so` hooks | `server1/preload.txt` + `aig_nhat.so`, `canxi_dotim_bayban.so`, `hang_sap_bot.so`, `libliendau_sims.so`, `vdk.so`, `libpreloadclean.so` | Custom `LD_PRELOAD` shared libraries hooking or extending `jx_linux_y` functions at runtime (check corresponding `.ini` files in `server1/`). |
| `libheaven.so`, `librainbow.so` | `server1/` & `gateway/` | Networking / IOCP-epoll socket transport libraries. |

---

## 6. Text Encoding Reality in `legacy-server`

Files in `legacy-server` routinely mix **three encodings inside the exact same file**:
1. **TCVN3 (ABC / TCVN 5712:1993)** single-byte Vietnamese strings (`0xA1..0xFE` in `Say`, `Msg2Player`, `npcs.txt`, `magicscript.txt`).
2. **GB18030 / GBK** Simplified Chinese comments and original legacy keys (`-- 野叟任务链脚本`).
3. **UTF-8 / ASCII** newer custom comments and scripts.

Standard UTF-8 file readers (`read_file`, `rg`) will either reject mixed files as "binary" or fail to match Unicode Vietnamese queries like `"Dã Tẩu"`. Always use `scripts/read_jx_file.py` and `scripts/read_tabfile.py` so TCVN3, GB18030, and UTF-8 are decoded deterministically.
