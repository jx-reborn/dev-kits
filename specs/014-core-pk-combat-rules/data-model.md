# Data Model: Core PK & Combat Rules (`014-core-pk-combat-rules`)

**Branch**: `014-core-pk-combat-rules` | **Date**: 2026-11-11 | **Spec**: [spec.md](spec.md) | **Research**: [research.md](research.md)

---

## 1. Primitives & Enumerations (`JXReborn.Primitives.Combat`)

### 1.1 `PkEnmityState` (`byte`)
Represents the server-authoritative Enmity (`Cừu sát`) duel lifecycle:
- `Closed = 0` (`enumPK_ENMITY_STATE_CLOSE`): No active or pending Enmity duel.
- `Countdown = 1` (`enumPK_ENMITY_STATE_TIME`): Mandatory `200`-frame (`~11.11s`) warmup countdown; Enmity does **not** yet override `Peace` or same-camp `Fight` relations.
- `Active = 2` (`enumPK_ENMITY_STATE_PKING`): Active `10,800`-frame (`10`-minute) mutual combat; overrides `Peace` and same-camp `Fight` relations (`Enemy`).

### 1.2 `CombatConsts` Additions
- `SwitchToPeaceCooldownFrames = 3240` (`180s` at 18 Hz — already present, overridden by `PkRulesTemplate.SwitchToPeaceCooldownFrames` when loaded).
- `EnmityWarmupFrames = 200` (`PK_ANMITY_TIME`).
- `EnmityActiveDurationFrames = 10800` (`PK_ANMITY_CLOSE_TIME`).
- `MaxPkValue = 10` (`MAX_DEATH_PUNISH_PK_VALUE`).
- `MinFightExpDebtPercent = -80` (`NotFightExpPercent` in `pkrate.ini`).
- `MinEnmityExpDebtPercent = -50` (`NotEnmityExpPercent` & `NotSubPKExpPercent` in `pkrate.ini`).
- `HighLevelPunishExpThresholdLevel = 130` (threshold selecting the high-level max XP loss lookup table `HighLevelMaxExpLossByPkValue`).

---

## 2. Static Game Asset Templates (`JXReborn.GameAssets.PlayerTemplates`)

Compiled from `data/players/progression.players.json` (`pkRules`) into `artifacts/jxreborn-data.pack` via `MemoryPack`:

### 2.1 `PkPenaltyRuleTemplate`
| Property | C# Type | Constraints / Units | Source Mapping |
| :--- | :--- | :--- | :--- |
| `PkValue` | `byte` | `0..10` | `pkRules.penaltiesByPkValue[].pkValue` |
| `ExpLossPerMil` | `int` | `>= 0` (parts per `1,000` of `RequiredLevelExp`) | `pkpunish.txt` col 2 (`10..350`) |
| `MaxExpLoss` | `long` | `>= 0` (standard `Level < 130` cap = `500,000`) | `pkpunish.txt` col 8 (`500000`) |
| `HighLevelMaxExpLoss` | `long` | `>= 0` (`Level >= 130` cap: `500,000..50,000,000`) | Legacy 6.0 `PkValue` `0..10` XP loss ceiling |
| `MoneyDropPerMil` | `int` | `0..1000` (parts per `1,000` of `InventoryMoney`) | `pkpunish.txt` col 3 (`50..1000`) |
| `BagItemDropPerMil` | `int` | `0..1000` (per-item drop roll out of `1,000`) | `pkpunish.txt` col 4 (`0..800`) |
| `EquipDropPerMil` | `int` | `0..1000` (per-slot drop roll out of `1,000`) | `pkpunish.txt` col 5 (`0..1000`) |
| `DurabilityLossPercent` | `int` | `0..100` (`%` of `MaxDurability` abraded) | `pkpunish.txt` col 9 (`0..100`) |

### 2.2 `PkRulesTemplate`
| Property | C# Type | Default | Description |
| :--- | :--- | :--- | :--- |
| `SwitchToPeaceCooldownFrames` | `int` | `3240` | Cooldown in 18-Hz frames (`180s`) to return to `Peace` when `FightMode != 0` |
| `MaxPkValue` | `byte` | `10` | Upper clamp for `PlayerZoneState.PkValue` |
| `EnmityAddPkValue` | `sbyte` | `2` | Killer `PkValue` gain when Enmity launcher kills target (`EnmityPK=2`) |
| `SlaughterAddPkValue` | `sbyte` | `1` | Killer `PkValue` gain for unlawful Slaughter / over-leveled kill |
| `ButcherExerciseAddPkValue` | `sbyte` | `1` | Killer `PkValue` gain when third-party Slaughter kills Sparring player (`ButcherPKExercise=1`) |
| `VictimDeathPkDecay` | `sbyte` | `-1` | Victim `PkValue` delta on punishable PvP death (`BeKilled=-1`) |
| `MinFightExpDebtPercent` | `int` | `-80` | Minimum XP ratio (`%`) required to leave `Peace` & floor for PvP XP loss |
| `MinEnmityExpDebtPercent` | `int` | `-50` | Minimum XP ratio (`%`) required to launch Enmity or receive `-1` PK decay |
| `PenaltiesByPkValue` | `IReadOnlyList<PkPenaltyRuleTemplate>` | `11` rows (`0..10`) | Indexed O(1) lookup by clamped `PkValue` |

---

## 3. In-Memory 18-Hz Zone Entities (`JXReborn.ZoneEngine`)

### 3.1 `PlayerZoneState` Camp & Party Properties
| Property | C# Type | Persistence | Invariant / Semantics |
| :--- | :--- | :--- | :--- |
| `BaseCamp` | `EntityCamp` | Persisted (derived from Faction / Character `Camp`) | Permanent alignment (`Begin = 0`, `Justice = 1`, `Evil = 2`, `Balance = 3`). Never overwritten by temporary PK flag (`0x54`). |
| `CurrentCamp` | `EntityCamp` | Transient (initialized to `BaseCamp` on attach) | Effective combat camp broadcast in `0x4C`, `0x4D`, and `0x58`. Equals `EntityCamp.Free` (`4`) while temporary PK camp flag is enabled on a non-novice player; restored to `BaseCamp` when disabled or when joining/changing sect. |
| `PartyId` | `uint` | Transient (`0` = no party) | Non-zero identifier shared by active party teammates; grants mutual PvP immunity (`Friend`) in `EvaluateRelation`. |
| `PkState` | `PlayerPkMode` | Persisted (`Character.PkState`) | Normal PK stance (`Peace = 0`, `Fight = 1`, `Slaughter = 2`). |
| `PkValue` | `sbyte` | Persisted (`Character.PkValue`) | Criminal murder counter clamped to `[0, 10]`. |
| `LastPkModeChangeTick` | `long` | Transient | 18-Hz frame tick when `PkState` last changed out of `Peace` (or between `Fight`/`Slaughter`). |

### 3.2 `PlayerPkSessionState` (`IPlayerFeatureSlot`)
Stored in `PlayerZoneState`'s zero-allocation `IPlayerFeatureSlot` array via `PlayerFeatureSlotKey<PlayerPkSessionState>`:

| Property | C# Type | Initial Value | Semantics |
| :--- | :--- | :--- | :--- |
| `EnmityState` | `PkEnmityState` | `PkEnmityState.Closed` | `Closed (0)`, `Countdown (1)`, or `Active (2)` |
| `EnmityAimCharacterId` | `Guid` | `Guid.Empty` | Opponent's `CharacterId` (empty when `Closed`) |
| `EnmityAimNpcId` | `uint` | `0` | Opponent's runtime `NpcId` for wire sync |
| `EnmityRemainingFrames` | `int` | `0` | `200..1` during `Countdown`; `10800..1` during `Active` |
| `IsEnmityLauncher` | `bool` | `false` | `true` on the initiator (`+2` PK on kill), `false` on the target (`+0` PK on self-defense kill) |
| `IsExercising` | `bool` | `false` | `true` while in an active 1v1 Sparring match |
| `ExerciseAimCharacterId` | `Guid` | `Guid.Empty` | Sparring partner's `CharacterId` |
| `ExerciseAimNpcId` | `uint` | `0` | Sparring partner's runtime `NpcId` |
| `PendingSparTargetCharacterId` | `Guid` | `Guid.Empty` | Optional transient challenge proposal target for `ArenaSparringMasterDialogScript` |
| `PendingSparExpiryTick` | `long` | `0` | Expiration tick (e.g., `+540` frames = `30s`) for unanswered Arena Sparring challenge |

**Lifecycle Hook (`OnPlayerDetached`)**:
When `PlayerZoneState` detaches from `MapZoneWorker` for any `PlayerDetachReason` (`CleanLogout`, `SocketClosed`, `HeartbeatTimeout`, `LeaseSuperseded`, `MapTransferred`), `PlayerPkSessionState.OnPlayerDetached` invokes `context.Worker.CloseAllPkStates(player)`, immediately clearing any active Enmity or Sparring on the remaining in-zone opponent and sending `0x91` (`btState = 0`) / `0x92` (`btState = 0`).

### 3.3 `PkRelationContext` (`readonly record struct`)
Stack-allocated value struct passed to `CombatDamageCalculator.EvaluateRelation` by both `MapZoneWorker` (immediate skills) and `MissileSimulationTickSystem` (projectile impacts):

```text
PkRelationContext(
    Guid CharacterId,
    uint RuntimeNpcId,
    EntityCamp BaseCamp,
    EntityCamp CurrentCamp,
    byte FightMode,
    PlayerPkMode PkMode,
    uint PartyId,
    PkEnmityState EnmityState,
    Guid EnmityAimCharacterId,
    bool IsExercising,
    Guid ExerciseAimCharacterId)
```

---

## 4. Authoritative State Machines

### 4.1 Normal PK Mode State Machine (`0x76` $\rightarrow$ `0x90`)

```mermaid
stateDiagram-v2
    [*] --> Peace : Character Created / Attached (Default)
    Peace --> Fight : c2s 0x76 (1) [AllowPkChange && BaseCamp != Begin && XP > -80%]
    Peace --> Slaughter : c2s 0x76 (2) [AllowPkChange && BaseCamp != Begin && XP > -80%]
    Fight --> Slaughter : c2s 0x76 (2) [AllowPkChange && BaseCamp != Begin && XP > -80%]
    Slaughter --> Fight : c2s 0x76 (1) [AllowPkChange && BaseCamp != Begin && XP > -80%]
    Fight --> Peace : c2s 0x76 (0) [FightMode == 0 OR Elapsed >= 3240 Frames]
    Slaughter --> Peace : c2s 0x76 (0) [FightMode == 0 OR Elapsed >= 3240 Frames]
    Fight --> Peace : Forced on PvP Death if XP <= -80%
    Slaughter --> Peace : Forced on PvP Death if XP <= -80%
```

### 4.2 Targeted Enmity State Machine (`0` $\rightarrow$ `1` $\rightarrow$ `2` $\rightarrow$ `0`)

```mermaid
stateDiagram-v2
    [*] --> Closed
    Closed --> Countdown : c2s 0x77 (11 Gates Pass)\nRemainingFrames = 200\ns2c 0x91 (btState=1) to Both
    Countdown --> Active : 200 Frames Elapsed (Both Alive & FightMode != 0)\nRemainingFrames = 10800\ns2c 0x91 (btState=2) to Both
    Countdown --> Closed : Death / FightMode==0 / Detach / StartExerciseSpar\ns2c 0x91 (btState=0) to Both
    Active --> Closed : 10800 Frames Elapsed (Timeout Bug Fixed!)\ns2c 0x91 (btState=0) to Both
    Active --> Closed : Death / FightMode==0 / Detach / StartExerciseSpar\ns2c 0x91 (btState=0) to Both
```

### 4.3 1v1 Sparring (`Exercise`) State Machine (`0` $\leftrightarrow$ `1`)

```mermaid
stateDiagram-v2
    [*] --> Inactive
    Inactive --> Sparring : StartExerciseSparZoneCommand / Arena Dialog\nCloses Active Enmity on Both First\ns2c 0x92 (btState=1) to Both
    Sparring --> Inactive : Partner Defeat (Zero Penalty!) / Third-Party Kill (+1 PK to Killer)\nOR FightMode==0 / Detach\ns2c 0x92 (btState=0) to Both
```

---

## 5. PvP `DeathPunish` Resolution Flow

```mermaid
flowchart TD
    A["Player Victim Life <= 0 (AttackerNpcId)"] --> B{"Attacker is Player\nAND Victim.FightMode != 0?"}
    B -- No --> C["PvE / Environmental Death\n CloseAllPkStates(Victim)"]
    B -- Yes --> D{"Victim.ExerciseAim == Killer?"}
    D -- Yes --> E["enumDEATH_MODE_PLAYER_NO_PUNISH\n0 XP / 0 Money / 0 Drop / 0 Abrade / +0 PK\nCloseAllPkStates(Victim)"]
    D -- No --> F["Compute Killer PkValue Delta (0, +1, or +2)\nClamp [0..10] & Emit s2c 0x93 if Changed"]
    F --> G["Deduct Victim XP (min(rawLoss, Cap), Floor = -80% ReqExp)\nEmit s2c 0x5C (Force Peace if XP <= -80%)"]
    G --> H["Deduct InventoryMoney (moneyDropPerMil / 1000)\nEmit s2c 0x68 & Spawn floor(moneyLost / 2) Ground Money (s2c 0x64)"]
    H --> I["Roll Bag Drops (bagItemDropPerMil / 1000)\n& Equip Drops (equipDropPerMil / 1000) -> Ground Objects (s2c 0x60 + 0x64)"]
    I --> J["Abrade Remaining Equipped Items by durabilityLoss%\nMark IsBroken if 0, Recalc Stats & Sync Items (s2c 0x5F)"]
    J --> K{"Victim.PkValue > 0\nAND XP > -50% ReqExp?"}
    K -- Yes --> L["Victim.PkValue -= 1\nEmit s2c 0x93"]
    K -- No --> M["CloseAllPkStates(Victim) & Broadcast NpcDeath (s2c 0x57)"]
    L --> M
```
