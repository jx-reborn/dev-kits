# JXReborn Differential Parity Journeys Backlog

This document tracks the authoritative backlog and execution status of end-to-end **Differential Parity Journeys** run side-by-side between the **`legacy` Containerized Zero-Sim Oracle (`ghcr.io/cutect/jx-runtime-standalone:latest`)** and **`JXReborn` (SUT)** through the real headless game client (`qa/JXReborn.ClientDriver`), governed by [`docs/differential-parity-standard.md`](differential-parity-standard.md) (`v1.1`).

---

## 1. Mandatory Execution Rules

1. **Isolated `T=0` Clean State & Fixed Identifiers (`1 Journey = 1 Test Class + Fixed Account/Role`)**:
   - All parity suites execute via `.\qa\scripts\run-parity.ps1` on isolated ports `15622` / `16666` with `--tmpfs /var/lib/mysql` (ephemeral `T=0` clean database state per run).
   - Every Journey uses a dedicated test class and **fixed, unique ASCII `Account` and `RoleName` pairs** (never randomized timestamps) for deterministic trace correlation between `Oracle` and `SUT`.
   - Multi-actor scenarios (PvP, Party, Trade, Stalls, Guilds) use dedicated `_a` / `_b` pairs per Journey.
2. **Zero Asymmetric Memory Cheats & Symmetric Step-0 GM Token Seeding `(6, 1, 4258)`**:
   - **Zero one-sided RAM injection**: Internal C# backdoor teleports or server-side currency/EXP/item cheats are strictly forbidden in parity journeys.
   - **Symmetric Step-0 Seeding (`TestGmTokenDriver`)**: Journeys requiring `Lv10+`, currency, or starter kits (`J03`, `J05`, `J06`, `J08`, `J09`–`J16`) invoke the **Test GM Handbook `(6, 1, 4258)`** (`camnang_gm.lua` on Oracle ↔ `TestGmTokenItemScript` on SUT) identically through real game client packets (`0x59` → `0x63` → `0x5F`).
   - **Root-Cause Parity Fixes**: Preserve existing symbol identifiers; whenever `JXReborn` diverges unintentionally from `legacy`, diagnose root cause across packets, formulas, state machines, and configuration tables before fixing cleanly.
3. **Per-Journey Commit & Verification**:
   - After completing and verifying a Journey (plus offline regression suites), commit cleanly using Conventional Commits before advancing to the next Journey.

---

## 2. Complete Journey Backlog & Fixed Identities

### Phase I — Core Golden Parity Journeys (`001`–`016`)

| ID | Journey Name | Test Class | Fixed Account(s) | Fixed RoleName(s) | Parity Scope (`Oracle` vs `SUT`) | Status |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| **`J01`** | **Auth, All Series/Genders & Starter Villages** | `DifferentialParityWorldTests` | `parity_test1`, `j01ser0g0`–`j01ser4g1`, `j01vil53`, `j01vil101`, `j01vil20` | `ParityHero`, `J01Ser0G0`–`J01Ser4G1`, `J01Vil53`, `J01Vil101`, `J01Vil20` | Gateway login (`15622`), create all 5 series × 2 genders (`Lv1`, `0` silver, `0` `KnownDeviations`), enter starter villages, dismiss `login_hint.lua` banner (`0x5F`), and verify all 3 profiles (`minimal`, `kit`, `vanilla`). | ✅ Verified Live |
| **`J02A`** | **Town Obstacle Navigation, Speed & Gate FightMode** | `DifferentialParityMovementAndDialogTests` | `j02agate`, `j02asped` | `J02AGate`, `J02ASped` | Pre-flight homing to *Ba Lăng Huyện* center, obstacle-aware pathfinding, `Walk`/`Run` speed parity at `18 TPS` (`>= 95%`, `0x46` clock sync), and South Gate `FightMode 0 ↔ 1` toggle. | ✅ Verified Live |
| **`J03`** | **Static NPC Dialog & Unfunded Travel Guard** | `DifferentialParityMovementAndDialogTests` | `j03xaphu` | `J03XaPhu` | Out-of-range dialog rejection, `Level < 5` travel block (`station.lua:451` `Talk`: `"Bên ngoài rất loạn lạc..."`), symmetric `Lv10` GM Token seeding `(6, 1, 4258)`, Station Master dialog (`0x6E` → `0x63`), teleport choice (`0x5F`), and insufficient-funds rejection. | ✅ Verified Live |
| **`J02B`** | **Boundary Portal & Cross-Map Handoff** | `DifferentialParityPortalHandoffTests` | `j02portal` | `J02Portal` | Round-trip traversal from *Ba Lăng Huyện* (`53`) via East Gate → *Vũ Lăng Động* (`199`) → return to *Ba Lăng Huyện* (`53`) and re-enter safe town (`FightMode 0`), verifying `MapId`, arrival coordinates, `FightMode` (`0 ↔ 1`), and uninterrupted `0x49`/`0x4E` wire framing without socket/cipher drops. | ✅ Verified Live |
| **`J04`** | **Combat, Monster AI, Progression & Loot** | `DifferentialParityCombatAndLootTests` | `j04combat` | `J04Combat` | Exit *Ba Lăng Huyện* East Gate to fight monsters (`0x4D`/`0x87`), verify `Life`/`Exp`/`Damage`/`Defense` curves (`stupidanimal.lua`), `Doing` state transitions (`attack`/`hurt`/`death`), ground drops (`0x64` with `ObjId >= 1000`, `moneyobj.txt` `267..271`), item pickup (`0x5A` → `0x67`/`0x5F`/`0x61`), and return to town (`FightMode 0`). | ✅ Verified Live |
| **`J05`** | **PK, Camp State Machine & Sparring** | `DifferentialParityPkAndSparringTests` | `j05pvpa`, `j05pvpb` | `J05PvpA`, `J05PvpB` | Verify novice `Camp=0` raw `0x76` behavior (legacy server relies on UI lock while `JXReborn` hardens server-side against packet injection), symmetric `Lv10` GM Token seeding, *Võ Đang Đạo Nhân* induction dialog (`Talk` 3 pages → `Say` sect join → `Map 81`), `Fight (1)` & `Slaughter (2)` switches (`0x76`→`0x90`), 3-minute outdoor cooldown, Justice same-camp (`Camp=1`) immunity, cooldown clear in Peace Zone, and Enmity (`0x77`→`0x91` `200`-frame warmup → `Active=2`, auto-canceled on safe-zone entry). | ✅ Verified Live |
| **`J06`** | **Factions, Skills & Point Allocation** | `DifferentialParityFactionAndSkillTests` | `j06faction` | `J06Faction` | Symmetric `Lv10` GM Token seeding, series mismatch guard at *Nga My Cẩm Y Ni* (dialog closes after `Talk`), join *Võ Đang* (`Series=4` → `Map 81`, `Camp=1`, `CurFaction=8`, `FirstFaction=8`, 12 sect skills granted), allocate base stats `0x57`→`0x5D` (`+1 Str/Vit/Eng` matching `+3 LifeMax`, `+4 ManaMax` Earth curve), reject `ReqLevel=20` skill (`159`), allocate skill `0x58`→`0x5E` (`0→1`), and enforce `LevelCapReached`. | ✅ Verified Live |
| **`J07`** | **Inventory, Equipment, Potions & Durability** | `DifferentialParityInventoryAndEquipTests` | `j07invent` | `J07Invent` | Bag `6x10` item moves (`0x5B` → `0x62` + `0x8E`), right-click auto-equip (`ApplyUseItem` 3-step `ExchangeItemHand`), unequip weapon, ground drop (`0x5E` → `0x60` + `0x64`) & re-pickup (`0x5A` → `0x67` + `0x5F`), and *Thổ Địa Phù* Town Portal wire genre fix (`TownPortalAlt = 5`). | ✅ Verified Live |
| **`J08`** | **Multi-Channel Chat & System Broadcasts** | `DifferentialParityChatChannelTests` | `j08chata`, `j08chatb` | `J08ChatA`, `J08ChatB` | Symmetric `Lv10` GM Token seeding & *Võ Đang* induction, Nearby chat (`channel=2`), Private Whisper (`channel=3`), and Faction chat (`channel=5`) via `0x56` → `0x7E` preserving `TCVN3` Vietnamese text & `TEncodeText` color tags (`[0x02,R,G,B]`), plus `0x60` subscription mask (`0x10` mute blocks Faction while forcing Nearby `0x02` active and allowing Whispers, then restoring `0xFFFFFFFF`). | ✅ Verified Live |

---

### Phase II — Gameplay, Social & Economy Module Journeys (`017`–`028`)

> **Architecture Note**: All modules `017`–`028` are implemented in `src/` (with full offline Unit/Protocol/Integration suites passing) alongside pre-wired `DifferentialParity*Tests.cs` harnesses and GM Handbook Preset Kits (`Kit J09`, `Kit J10`, `Kit J13-J14`, `Kit J16`) under `qa/JXReborn.ClientDriver.Tests/DifferentialParity/`. Execute sequentially against live oracle via `.\qa\scripts\run-parity.ps1 -Profile kit`.

| ID | Journey Name | Test Class | Fixed Account(s) | Fixed RoleName(s) | Parity Scope (`Oracle` vs `SUT`) | Status |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| **`J09`** | **NPC Shops & Equipment Repair (`017`)** | `DifferentialParityNpcShopTests` | `j09shop` | `J09Shop` | Seed `Kit J09` via GM Token (`-Profile kit` smoke-verified), open merchant/blacksmith UI (`0x84`), purchase item (`0x5D`), sell item for silver (`0x5C`), and repair equipment (`0x7A` → `0x9B`). | 🧪 Ready for Live Calibration (`-Profile kit`) |
| **`J10`** | **Repository Box & Revival Binding (`018`)** | `DifferentialParityRepositoryBoxTests` | `j10repo` | `J10Repo` | Seed `Kit J10` via GM Token, open Repository Box (`0x88`), store/retrieve items (`Place 3 ↔ 4`), deposit/withdraw silver (`0x73`), set/unlock/lock secondary PIN, and bind town revival point. | 🧪 Ready for Live Calibration (`-Profile kit`) |
| **`J11`** | **Party Lifecycle & Team Aura (`019`)** | `DifferentialParityPartyTests` | `j11partya`, `j11partyb` | `J11PartyA`, `J11PartyB` | Create team (`0x53`), invite/join/promote/leave, AOI monster `EXP` sharing (`0x5C`), and captain Five-Elements aura (`0x6F` → `0x87`). | 🧪 Ready for Live Calibration (`-Profile kit`) |
| **`J12`** | **Social Graph: Friends & Enemies (`020`)** | `DifferentialParitySocialGraphTests` | `j12socia`, `j12socib` | `J12SociA`, `J12SociB` | Mutual friend request/accept (`0x61` → `0x62`), Blacklist filtering, auto-logging PK attackers as Enemies, and online/offline status notices. | 🧪 Ready for Live Calibration (`-Profile kit`) |
| **`J13`** | **Zero-Dupe P2P Player Trade (`021`)** | `DifferentialParityPlayerTradeTests` | `j13tradea`, `j13tradeb` | `J13TradeA`, `J13TradeB` | Seed `Kit J13-J14`, trade invite (`0x6B` → `0x75` → `0x21`) → stage items & silver (`0x6C`, `Place 5`) → lock → two-phase confirm (`0x6D`), verifying zero-dupe conservation on completion and cancellation (`0x6A`). | 🧪 Ready for Live Calibration (`-Profile kit`) |
| **`J14`** | **Personal City Stalls (`022`)** | `DifferentialParityPlayerStallTests` | `j14stalla`, `j14stallb` | `J14StallA`, `J14StallB` | Seed `Kit J13-J14`, mark item prices, open safe-zone stall (`0x7B` → `0x7C`), browse stall (`0x7D`), and execute atomic buyout (`0x7E`) with city tax deduction. | 🧪 Ready for Live Calibration (`-Profile kit`) |
| **`J15`** | **Purple Equipment Crafting (`023`)** | `DifferentialParityPurpleCraftingTests` | `j15craft` | `J15Craft` | Open Blacksmith Foundry UI (`0x7F` / `0x92`), upgrade *Huyền Tinh* stones, distill magic ores, and enchase 6-socket Purple equipment. | 🧪 Ready for Live Calibration (`-Profile kit`) |
| **`J16`** | **Guilds, Quests & Missions (`024`–`028`)** | `DifferentialParityEndgameMissionTests` | `j16guilda`, `j16guildb` | `J16GuildA`, `J16GuildB` | Seed `Kit J16` (*Nhạc Vương Kiếm* + `Lv80` + `1M` silver), create Guild & test guild chat (`024`), *Dã Tẩu* quest chain (`025`), World Bosses (`026`), Song-Jin Battleground (`027`), and *Vượt Ải* dungeon (`028`). | 🧪 Ready for Live Calibration (`-Profile kit`) |
