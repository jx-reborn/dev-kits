# Quickstart & Verification Guide: Core PK & Combat Rules

**Feature Branch**: `014-core-pk-combat-rules`  
**Date**: 2026-11-11  
**Spec**: [spec.md](spec.md) | **Plan**: [plan.md](plan.md)

---

## 1. Prerequisites

- **.NET 10 SDK** (`net10.0`)
- **Repository Root**: repository root
- **Canonical Tier-1 Data**: `data/players/progression.players.json` (contains `pkRules`: `pkPenaltyTable[0..10]`, `blockLeavePeaceDebtThresholdPercent = -80`, `blockEnmityDebtThresholdPercent = -50`, `butcherPkExercisePenalty = 1`, `peaceSwitchCooldownSeconds = 180`)

---

## 2. Build & Tier-2 Asset Snapshot Compilation

### 2.1 Zero-Warning Solution Build
Verify that all assemblies (`Primitives`, `GameAssets`, `ZoneEngine`, `Scripts`, `Adapter.Legacy`, `DataExporter`, and all test suites) compile cleanly with zero warnings:

```powershell
# From the repository root
dotnet build JXReborn.slnx -warnaserror
```

### 2.2 Compile & Verify `PkRulesTemplate` in `jxreborn-data.pack`
Compile the canonical JSON data (`data/players/progression.players.json`) into the binary `MemoryPack` snapshot (`artifacts/jxreborn-data.pack`) and verify SHA-256 manifest round-trip integrity:

```powershell
dotnet run --project tools/JXReborn.DataExporter -- compile --data-root data --output artifacts/jxreborn-data.pack
```

---

## 3. Three-Tier Automated Verification Guide

### Tier 1 — Symmetric Wire Protocol & Framing Unit Tests (`JXReborn.Protocol.Tests`)

Validates byte-for-byte `Read` / `Write` symmetry, `ProtocolSizeTable` C2S lengths (`0x54 => 2`, `0x76 => 2`, `0x77 => 5`), `m_wLength = TotalSize - 1` variable-length headers on `0x91` / `0x92`, and UTF-8 $\leftrightarrow$ `TCVN3` opponent name transcoding across all 8 PK opcodes (`0x54`, `0x76`, `0x77`, `0x58`, `0x90`, `0x91`, `0x92`, `0x93`):

```powershell
dotnet test tests/JXReborn.Protocol.Tests --filter "FullyQualifiedName~PkWirePacketTests" --no-build
```

**Expected Verification Outcomes**:
- `PlayerApplySetPkC2SPacket` (`0x54`, `2B`) and `PkApplyEnmityC2SPacket` (`0x77`, `5B`) decode exact payload fields and match `ProtocolSizeTable` entries.
- `PkSyncEnmityStateS2CPacket` (`0x91`) and `PkSyncExerciseStateS2CPacket` (`0x92`) serialize `8 + N` bytes with `PacketLength = (ushort)(7 + N)` (`TotalSize - 1`) and exact `TCVN3` bytes.
- `NpcChangeCurCampS2CPacket` (`0x58`, `6B`), `PkSyncNormalFlagS2CPacket` (`0x90`, `2B`), and `PkSyncPkValueS2CPacket` (`0x93`, `5B`) round-trip identically against the hex vectors in [pk-and-pvp-wire-protocol.md](contracts/pk-and-pvp-wire-protocol.md).

---

### Tier 2 — Deterministic 18-Hz `ZoneSimulationHarness` Tests (`JXReborn.ZoneEngine.Tests`)

Runs headless, single-threaded 18-Hz zone ticks without network sockets or wall-clock `Thread.Sleep` delays.

#### 2A. PvP Relation Matrix, Enmity `200f` Warmup + `10,800f` Expiry & Camp Flag Tests

```powershell
dotnet test tests/JXReborn.ZoneEngine.Tests --filter "FullyQualifiedName~PvpRelationMatrixAndPkStateTests" --no-build
```

**Scenario Walkthroughs**:
1. **Peace-Zone Cooldown Bypass vs Combat-Zone `3,240`-Frame Lockout**:
   - Switch a player to `Slaughter` inside a combat zone (`FightMode = 1`) $\rightarrow$ attempt `Peace` switch at frame `100` $\rightarrow$ rejected (`CooldownRemainingFrames = 3,140`).
   - Move the player onto a Peace-Zone region (`FightMode = 0`) $\rightarrow$ re-send `Peace` switch $\rightarrow$ immediately succeeds (`bForce = true`, `0x90` `PkFlag = 0`).
2. **Temporary Camp Flag Toggle (`BaseCamp` Preservation)**:
   - Righteous (`BaseCamp = EntityCamp.Justice`) player toggles `SetTemporaryPkCampZoneCommand(EnableFreePkCamp: true)` $\rightarrow$ `CurrentCamp` becomes `EntityCamp.Free (4)` and broadcasts `0x58`; toggling `false` restores `CurrentCamp = EntityCamp.Justice (1)`.
3. **`200`-Frame Enmity Warmup & `10,800`-Frame Clean Timeout**:
   - Dispatch `ApplyEnmityZoneCommand` between two combat-ready players $\rightarrow$ both enter `PkEnmityState.Countdown (1)` (`0x91` `btState = 1`).
   - Step frames `1..199`: verify `CombatDamageCalculator.EvaluateRelation` returns `Neutral` (unless already hostile via normal posture) and skills deal `0` damage.
   - Step frame `200`: `PkStateZoneTickSystem` transitions both players to `PkEnmityState.Active (2)` (`0x91` `btState = 2`) and sets `RemainingFrames = 10,800`; verify both direct skills and in-flight `MissileSimulationTickSystem` projectiles resolve `Enemy`.
   - Step `10,800` additional frames without a kill: verify `PkStateZoneTickSystem` cleanly closes **Enmity** on **both** players (`0x91` `btState = 0`, `PkEnmityState.Closed`) rather than calling `ExercisePKClose`.

#### 2B. `1v1 Sparring` Zero-Penalty Defeat vs `Slaughter` `+1` PK & 50% Ground Money Drop

```powershell
dotnet test tests/JXReborn.ZoneEngine.Tests --filter "FullyQualifiedName~PvpDeathPenaltySimulationTests" --no-build
```

**Scenario Walkthroughs**:
1. **Penalty-Free `1v1 Sparring` Defeat (`enumDEATH_MODE_PLAYER_NO_PUNISH`)**:
   - Start sparring via `StartExerciseSparZoneCommand` between two same-camp `Peace` players $\rightarrow$ verify any active Enmity closes first (`0x91` `btState = 0`), mutual `Exercise` opens (`0x92` `btState = 1`), and both resolve `Enemy` exclusively to each other.
   - Deal lethal damage from Sparring Partner A to Partner B $\rightarrow$ verify Sparring closes (`0x92` `btState = 0`), Partner A gains **`+0` `PkValue`**, and Partner B loses **`0` XP**, **`0` inventory money**, **`0` bag/equipped items**, and **`0` equipment durability**.
2. **`Slaughter` Kill (`+1` `PkValue`, Tiered Penalties & `50%` Ground Money Object) + Third-Party Interference**:
   - Have a `Slaughter` attacker kill a `Peace` victim carrying `10,000` inventory money at `PkValue = 2`:
     - Killer `PkValue` increments by `+1` (capped at `10`) and emits `0x93`.
     - Victim loses `20%` (`2,000`) inventory money (`8,000` remaining), and `MapZoneWorker` spawns a free-for-all ground money object worth **`1,000` (`moneyLost / 2`)** at the death coordinates (`0x64`).
     - Victim suffers tiered XP deduction (clamped at `-80%` floor and Lv130+ `HighLevelMaxExpLossByPkValue` cap), `20%` equipped durability abrasion, deterministic bag/equip drop rolls, and **`-1` `PkValue` decay** (`2` $\rightarrow$ `1`) when pre-death XP `> -50%`.
   - Have a third-party `Slaughter` player kill an active Sparring participant $\rightarrow$ verify the killer receives **`+1` `PkValue`** (`ButcherPKExercise`) and the sparring session closes cleanly on both sparring partners.

---

### Tier 3 — Live Loopback TCP Bot Duel Integration Tests (`JXReborn.Integration.Tests`)

Spins up the in-memory `GameServer` TCP listener with dual stream-ciphered legacy client sessions on a shared combat map shard:

```powershell
dotnet test tests/JXReborn.Integration.Tests --filter "FullyQualifiedName~PvpEnmityAndSparringIntegrationTests" --no-build
```

**End-to-End Flow Verified**:
1. **Initial World-Entry Sync**: Both TCP bots authenticate and enter the map $\rightarrow$ assert receipt of `0x90` (`s2c_pksyncnormalflag`) and `0x93` (`s2c_pksyncpkvalue`) in `SendInitialWorldSyncBurst`.
2. **Live Encrypted Enmity Duel**: Bot A sends `0x77` (`c2s_pkapplyenmity`) targeting Bot B's `WorldNpcId` $\rightarrow$ both receive `0x91` (`btState = 1`) $\rightarrow$ zone advances `200` ticks $\rightarrow$ both receive `0x91` (`btState = 2`) $\rightarrow$ Bot A casts `0x4D` (`c2s_playerskill`) until Bot B dies $\rightarrow$ both receive `0x91` (`btState = 0`), Bot A receives `0x93` (`PkValue = 2`), and AOI observers receive `0x64` ground money drop sync.
3. **Arena Sparring Master Challenge Flow**: Bot A and Bot B interact with `ArenaSparringMasterDialogScript` $\rightarrow$ both receive `0x92` (`btState = 1`) $\rightarrow$ lethal duel ends with `0x92` (`btState = 0`), unchanged `PkValue`, unchanged XP/money, and town revival.

---

## 4. Tier 4 — Live Zero-Sim Containerized Differential Parity Gate (`JXReborn.ClientDriver.Tests` — Journey `J05`)

Verifies novice `Camp=0` raw `0x76` packet hardening, symmetric `Lv10` GM Token seeding, Võ Đang sect induction, `Fight`/`Slaughter` stance switching, `3`-minute combat-zone cooldown vs Peace-Zone bypass, same-camp immunity (`Justice`), and `200`-frame Enmity countdown $\rightarrow$ active mutual duel against `jx-runtime-standalone` (`--tmpfs /var/lib/mysql`):

```powershell
.\tools\run-parity.ps1 -Profile minimal -Filter "DifferentialParityPkAndSparringTests"
```

---

## 5. Full Regression Gate

Run the complete offline test suite across the solution before opening a pull request:

```powershell
dotnet test JXReborn.slnx --no-restore --filter "Category!=Stress&Category!=Benchmark&Category!=HeadlessE2E&Category!=DifferentialParity"
```
