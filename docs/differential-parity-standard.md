# JXReborn Differential Parity Standard (`v1.2`)

This document defines the **Differential Parity Testing** standard using real client binaries (`game.exe` automated via `qa/JXReborn.ClientDriver`) in a **3-mode Hybrid model (`Live` / `Record` / `Replay`)** comparing the **Containerized Zero-Sim Live Reference Oracle (`legacy`)** against the **System Under Test (`JXReborn`)**, ensuring **JX Reborn** achieves **parity equal to or better than the legacy server** (per **Constitution `v1.10.0`**).

---

## 1. Dual-Run Differential Harness Architecture

```text
                        ┌─────────────────────────┐
                        │  IJourneyScenario       │
                        │  (Step-by-Step Actions) │
                        └────────────┬────────────┘
                                     │
                  ┌─────────────────┴─────────────────┐
                  ▼                                   ▼
   ┌─────────────────────────────┐     ┌─────────────────────────────┐
   │ JxHeadlessClient (Target A) │     │ JxHeadlessClient (Target B) │
   │ → Live Oracle (legacy)      │     │ → New Server (JXReborn)     │
   │   Container T=0 (tmpfs DB)  │     │   In-Process Ephemeral DB   │
   └──────────────┬──────────────┘     └──────────────┬──────────────┘
                  │ WorldDomSnapshot                  │ WorldDomSnapshot
                  ▼                                   ▼
   ┌─────────────────────────────┐     ┌─────────────────────────────┐
   │    WorldDomNormalizer       │     │    WorldDomNormalizer       │
   └──────────────┬──────────────┘     └──────────────┬──────────────┘
                  └─────────────────┬─────────────────┘
                                    ▼
                       ┌─────────────────────────┐
                       │     StateDiffEngine     │
                       │  + KnownDeviations      │
                       └─────────────────────────┘
```

* **Single Probe**: `JxHeadlessClient` (`qa/JXReborn.ClientDriver`) drives real `game.exe` instances on Windows.
* **Isolated `T=0` Execution**: Each target runs in an isolated client sandbox (`JxHeadlessSandbox`) backed by a completely blank initial database (`--tmpfs /var/lib/mysql` for Oracle; ephemeral In-Memory/SQLite DB for `JXReborn`).

---

## 2. Environment Configuration, 3 Zero-Sim Profiles & Symmetric Seeding

### 2.1. Environment Variables & Port Isolation (Zero Hardcoded Server IPs)
The `legacy` reference server runs entirely locally inside **`ghcr.io/cutect/jx-runtime-standalone:latest`** (`wslc` or `docker`) on isolated test ports (`15622` / `16666`), avoiding remote host dependencies and port conflicts with any local `jx-manager` instance (`5622` / `6666`):
- `LEGACY_ORACLE_HOST`: IP/hostname of the `legacy` Bishop gateway (e.g. `127.0.0.1`). When unset, all `[Trait("Category", "DifferentialParity")]` tests skip safely during offline `dotnet test` runs.
- `LEGACY_ORACLE_PORT`: Bishop port (defaults to `15622` when orchestrated via `qa/scripts/run-parity.ps1`).
- `LEGACY_ORACLE_PROFILE`: `minimal` (default for `J01`–`J08`), `kit` (auto-grants GM Token `(6, 1, 4258)` to all accounts for Preset Kits `J09`–`J16`), or `vanilla` (full 139 original maps).
- `LEGACY_ORACLE_CONTAINER`: Container name (defaults to `jx-oracle-test`) so `OracleContainerDiagnostics` automatically attaches `gameserver.log` / `bishop.log` / `s3relay.log` / `goddess.log` tails on divergence.
- `LEGACY_ORACLE_ACCOUNT` / `LEGACY_ORACLE_PASSWORD`: Dedicated test account credentials.
- `LEGACY_PARITY_MODE`: Hybrid execution mode (`Live` | `Record` | `Replay`), controlled via `-Mode` in `qa/scripts/run-parity.ps1`:
  1. **`Live` *(default when `LEGACY_ORACLE_HOST` is set)*:** Boots `legacy` `T=0` container and executes Oracle vs. `JXReborn` side-by-side.
  2. **`Record`:** Executes `Live` dual-run and **only after all assertions PASS** persists the golden baseline file `qa/JXReborn.ClientDriver.Tests/DifferentialParity/Baselines/<JourneyId>.oracle.json` with `ContentSha256` and `PackageArchiveSha256`.
  3. **`Replay` *(Offline — Zero Docker)*:** Requires no `legacy` container; loads and verifies the `ContentSha256` signature of `<JourneyId>.oracle.json` via `OracleBaselineStore.LoadAndVerify`, then drives `JXReborn` + `game.exe` through `StateDiffEngine`. **Manual edits to `*.oracle.json` files are strictly forbidden** (automatically rejected by SHA-256 verification and `OracleBaselineStoreTests`).

### 2.2. Three Zero-Sim Package Profiles (`qa/scripts/pack-legacy-profiles.ps1`)
The packager `qa/scripts/pack_legacy_profiles.py` (invoked via `.\qa\scripts\pack-legacy-profiles.ps1`) generates 3 GNU-tar `.tar.gz` archives under `artifacts/packages/` (preserving 100% raw `GBK` filenames on `ext4` and `0755` executable permissions on `bishop_y`, `goddess_y`, `s3relay_y`, `jx_linux_y`), while **stripping 100% of Sim/Bot overlays** (`6` `LD_PRELOAD` libraries: `vdk.so`, `hang_sap_bot.so`, `libliendau_sims.so`, `aig_nhat.so`, `canxi_dotim_bayban.so`, `libpreloadclean.so` and `287` `simcity`/`sims`/`vsd`/`autoingame` scripts):

| Profile (`-Profile`) | Archive File | Configuration Profile | Test Purpose |
| :--- | :--- | :--- | :--- |
| **`vanilla`** | `legacy-vanilla.tar.gz` | Full original `139` maps (`worldset.ini`), `CFG_HoTroTanThu=0` disabled, **no** GM Token granted | Smoke/benchmark startup for the full legacy 6.0 world |
| **`minimal`** *(default)* | `legacy-test-minimal.tar.gz` | Minimal `8` essential maps (`53`, `20`, `101`, `199`, `81`, `13`, `380`, `464`), `< 12s` boot, `CFG_HoTroTanThu=0` disabled (`Lv1`, `0` silver), grants GM Token `(6, 1, 4258)` only to designated test accounts (`gm*`, `j03*`, `j05*`, `j06*`, `j08*`, `j09*`..`j16*`) | Clean execution of all Core Golden Parity Journeys (`J01`–`J08`) without distorting `Lv1` baselines for `J01`/`J02A`/`J02B`/`J04`/`J07` |
| **`kit`** | `legacy-test-kit.tar.gz` | Same `8` minimal maps as `minimal`, but auto-grants GM Token `(6, 1, 4258)` to **100% of accounts** on world entry (`AutoGrantGmTokenOnLogin = true` on `JXReborn`) | Rapid execution of mid/endgame journeys (`J09`–`J16`) and interactive manual debugging |

### 2.3. Ephemeral `T=0` State With `--tmpfs /var/lib/mysql` (`qa/scripts/run-parity.ps1`)
- `.\qa\scripts\run-parity.ps1` launches `ghcr.io/cutect/jx-runtime-standalone:latest` with `--tmpfs /var/lib/mysql:rw,noexec,nosuid,size=512m`. Each fresh container startup (or `-Repack` with `FORCE_EXTRACT=1`) boots MariaDB and `goddess_y` from a **clean `T=0` baseline**, eliminating leftover character/bag state across runs and removing the need for manual DB cleanup scripts.

### 2.4. Symmetric Step-0 Seeding Standard via GM Token `(6, 1, 4258)` (`TestGmTokenDriver`)
- **Zero asymmetric RAM cheats**: Directly mutating C# server memory (`player.InventoryMoney = ...`, `player.Level = ...`) or configuring mismatched `initialNewbieLevel` between SUT and Oracle in parity tests is strictly forbidden.
- **Approved Step-0 Symmetric Seeding**: Whenever a journey requires pre-requisites above novice `Lv1` (e.g., `Lv10` for Station Master `J03` / Wudang sect `J05`–`J08`, or silver/materials/damaged gear for `J09`–`J16`), the test drives **`TestGmTokenDriver`** to activate **GM Handbook `(6, 1, 4258)`** via real `game.exe` wire packets (`0x59` `c2s_playereatitem` → `0x63` `s2c_scriptaction` → `0x5F` `c2s_playerselui`) symmetrically on **both targets**:
  - Oracle (`jx_linux_y`): Executes `server1/script/global/nobitaxd/item/camnang_gm.lua`.
  - SUT (`JXReborn`): Executes `TestGmTokenItemScript` (`src/JXReborn.Scripts/Items/TestGmTokenItemScript.cs`, script key `\script\global\nobitaxd\item\camnang_gm.lua`).
- **Production Safety Lockout**: On `JXReborn`, both granting and executing `TestGmTokenItemScript` are strictly gated behind `ZoneEngineOptions.EnableTestGmToken` and `ZoneEngineOptions.AutoGrantGmTokenOnLogin` (mandatory `false` default in production).

---

## 3. `WorldDomSnapshot` Normalization Rules (`WorldDomNormalizer`)

To eliminate false positives caused by normal runtime differences between legacy C++ and .NET 10, `WorldDomNormalizer` classifies snapshot fields into three buckets:

| Normalization Group | `WorldDomSnapshot` Fields | Rationale & Rule |
| :--- | :--- | :--- |
| **Ignore** | `Pid`, `TickCount`, `Hwnd`, `Player.DwId`, `Player.NpcIndex`, `Player.SubWorldIndex`, `Player.RegionIndex`, `Npc.ArrayIndex`, `Npc.DwId`, `S2c.Seq`, `S2c.TickCount`, `S2c.TimestampMs`, `Dialog.Sequence`, `Dialog.OpenedTick` | Process-local ephemeral handles, OS window IDs, and per-run timestamps. |
| **Exact Match** | `Net.*`, `Login.Status`, `Login.Result`, `Player.InWorld`, `Player.Level`, `Player.Series`, `Player.Camp`, `Player.CurrentCamp`, `Player.PkFlag`, `Player.PkValue`, `Player.FightMode`, `Player.MapId`, `Player.Money`, `Player.LifeMax`, `Player.ManaMax`, `Dialog.IsOpen`, `Dialog.Prompt`, `Dialog.Options`, `SystemMessages[].Text`, `S2C Opcode` sequence emitted during step | Authoritative domain state and wire protocol contracts MUST match 100% between Oracle and SUT. |
| **Tolerance Match** | `Player.MpsX`, `Player.MpsY` (default `±64` Mps movement tolerance), `Player.Life`, `Player.Mana` (permits natural tick regeneration deltas) | Accounts for minor frame-sampling phase differences across two independent TCP server processes. |

---

## 4. `StateDiffEngine` & `KnownDeviations` Governance

1. **Per-Step Diff Reporting & Container Diagnostics**:
   - On step failure, `StateDiffEngine` emits a structured diff identifying `ScenarioName`, `StepIndex/StepName`, `FieldPath`, `OracleValue`, `NewServerValue`, `RecentS2c` opcodes on both targets, plus automatic log tails from `OracleContainerDiagnostics` (`gameserver.log`, `bishop.log`).
2. **`KnownDeviations` (Intentional Quality & Security Improvements)**:
   - Whenever `JXReborn` intentionally diverges from `legacy` to **improve gameplay integrity or security** (for instance, rejecting forged C2S `0x76` PK-mode switch packets server-side for novice `Camp=0` characters rather than relying solely on client UI button locks, or fixing the legacy 10-minute Enmity expiry bug where sparring cleanup was called by mistake), that divergence MUST be verified via binary evidence and explicitly declared in `KnownDeviations` (or paired symmetric assertions) with all 3 fields:
     1. `RuleId` / `ScenarioStep`
     2. `FieldOrOpcode`
     3. `Justification` (Technical/security rationale proving why `JXReborn` improves upon the legacy behavior).
   - With `CFG_HoTroTanThu=1` mod overlays stripped across all 3 Zero-Sim profiles, novice login flow `J01` achieves **0 `KnownDeviations`** between Oracle and `JXReborn`.
   - Any unverified or undeclared divergence is treated as a regression failure (**FAIL**).

---

## 5. Differential Parity Journey Catalog (`J01`–`J16`)

All differential parity suites live under `qa/JXReborn.ClientDriver.Tests/DifferentialParity/` (complying with [`docs/testing-architecture-standard.md`](testing-architecture-standard.md) `v1.4` and tagged `[Trait("Category", "DifferentialParity")]`), tracked across two phases in [`docs/parity-journeys-backlog.md`](parity-journeys-backlog.md):

1. **Phase I — Core Golden Parity Journeys (`J01`, `J02A`, `J02B`, `J03`–`J08`):** Covers auth & 5-series × 2-gender role creation, town obstacle navigation & gate toggles, round-trip cross-map portal handoff, Station Master dialogs (`Level < 5` guard & `Lv10`), monster combat & loot pickup, PK/Enmity/Sparring state machines, faction induction & stat/skill allocation, inventory/equipment/*Thổ Địa Phù* sync, and multi-channel `TCVN3` chat.
2. **Phase II — Gameplay, Social & Economy Journeys (`J09`–`J16`):** Covers NPC Shops & Repair (`017`), Repository Box & secondary PIN (`018`), Party & Elemental Aura (`019`), Friends/Blacklist/Enemies (`020`), Two-Phase P2P Trade (`021`), City Stalls (`022`), Purple Equipment Crafting (`023`), and Guilds / *Dã Tẩu* / World Bosses / Song-Jin / *Vượt Ải* (`024`–`028`).
