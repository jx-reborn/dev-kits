# Domain Contracts: PvP Relation Matrix, Sparring API & Death Punishment (`014-core-pk-combat-rules`)

**Engine Tier**: `src/JXReborn.GameAssets`, `src/JXReborn.ZoneEngine`, `src/JXReborn.Scripts`  
**Compatibility Rules**: 11-tier PvP relation precedence, killer `PkValue` deltas, victim punishment calculations (`Lv >= 130` high-level XP loss caps), `pkpunish.txt`, and `pkrate.ini`.

---

## 1. Exhaustive Player-vs-Player Target Relation Truth Table

Evaluated identically for **immediate melee/AoE skills** (`MapZoneWorker.TryApplyPlayerDamageToPlayer`) and **in-flight projectiles** (`MissileSimulationTickSystem.StepTick`). Rules are evaluated top-to-bottom; the **first matching row** determines `EntityRelationKind`:

| Priority | Attacker (`A`) & Defender (`B`) Condition | Result | Notes |
| :---: | :--- | :---: | :--- |
| **1** | `A.RuntimeNpcId == B.RuntimeNpcId` | `Self` | Cannot deal offensive PvP damage to self |
| **2** | `A.FightMode == 0` **OR** `B.FightMode == 0` | `Friend` | Peace Zone sanctuary protection overrides all duel/PK states |
| **3** | `A.BaseCamp == Begin` **OR** `B.BaseCamp == Begin` | `Friend` | Novice (`camp_begin = 0`) absolute PvP protection |
| **4** | `A.PartyId != 0 && A.PartyId == B.PartyId` | `Friend` | Active party teammates cannot harm each other |
| **5** | `A.IsExercising && A.ExerciseAimCharacterId == B.CharacterId` | `Enemy` | Mutual 1v1 Sparring (`Tỷ thí`), even if both `Peace` or same camp |
| **6** | `A.EnmityState == Active && A.EnmityAimCharacterId == B.CharacterId` | `Enemy` | Active Enmity (`btState == 2`); **`Countdown` (`1`) does NOT match!** |
| **7** | `A.PkMode == Slaughter` **OR** `B.PkMode == Slaughter` | `Enemy` | `Slaughter` attacks `Peace`/`Fight`, **AND** `Peace`/`Fight` can retaliate against `Slaughter`! |
| **8** | `A.PkMode == Peace` **OR** `B.PkMode == Peace` | `Friend` | Protects `Peace` players against `Fight` (`1`) players outside duels |
| **9** | `A.PkMode == Fight && B.PkMode == Fight` with `A.CurrentCamp == Free || B.CurrentCamp == Free` | `Enemy` | Temporary Free-PK camp (`0x54`) makes `Fight` hostile to all camps |
| **10** | `A.PkMode == Fight && B.PkMode == Fight` with `A.CurrentCamp == B.CurrentCamp` | `Friend` | Same Justice / Evil / Balance camp allies |
| **11** | `A.PkMode == Fight && B.PkMode == Fight` with `A.CurrentCamp != B.CurrentCamp` | `Enemy` | Rival faction camps (`Justice` vs `Evil` vs `Balance`) |

### Selected Combinatorial Verification Matrix (`FightMode == 1`, `BaseCamp != Begin`, `PartyId == 0`)

| `A.PkMode` | `B.PkMode` | `A.CurrentCamp` | `B.CurrentCamp` | `EnmityState` | `Sparring?` | `Relation(A, B)` | `Relation(B, A)` |
| :---: | :---: | :---: | :---: | :---: | :---: | :---: | :---: |
| `Peace` | `Peace` | `Justice` | `Evil` | `Closed` | `false` | `Friend` | `Friend` |
| `Peace` | `Peace` | `Justice` | `Justice` | `Countdown (1)` | `false` | `Friend` | `Friend` |
| `Peace` | `Peace` | `Justice` | `Justice` | `Active (2)` | `false` | `Enemy` | `Enemy` |
| `Peace` | `Peace` | `Justice` | `Justice` | `Closed` | `true` | `Enemy` | `Enemy` |
| `Slaughter` | `Peace` | `Justice` | `Justice` | `Closed` | `false` | `Enemy` | `Enemy` *(Self-Defense)* |
| `Fight` | `Peace` | `Justice` | `Evil` | `Closed` | `false` | `Friend` | `Friend` |
| `Fight` | `Fight` | `Justice` | `Justice` | `Closed` | `false` | `Friend` | `Friend` |
| `Fight` | `Fight` | `Free (4)` | `Justice` | `Closed` | `false` | `Enemy` | `Enemy` |
| `Fight` | `Fight` | `Justice` | `Evil` | `Closed` | `false` | `Enemy` | `Enemy` |

---

## 2. Killer `DeathCalcPKValue` Contract (`0..10` Cap)

Evaluated in `PvpDeathPenaltyCalculator.EvaluateKillOutcome(PlayerZoneState killer, PlayerZoneState victim)` when `victim.FightMode != 0`:

| Order | Condition at Moment of Lethal Blow | `PunishmentMode` | `KillerPkDelta` | Clamped Killer `PkValue` |
| :---: | :--- | :---: | :---: | :--- |
| **1** | `victimPk.IsExercising && victimPk.ExerciseAimCharacterId == killer.CharacterId` | `NoPunish` | `0` | Unchanged |
| **2** | `victimPk.IsExercising && killer.PkState == PlayerPkMode.Slaughter` *(3rd-party interference)* | `PlayerPunish` | `+1` | `min(10, Killer.PkValue + 1)` |
| **3** | `victimPk.EnmityState == Active && victimPk.EnmityAimCharacterId == killer.CharacterId` | `PlayerPunish` | `killerPk.IsEnmityLauncher ? +2 : 0` | `min(10, Killer.PkValue + delta)` |
| **4** | `killer.PkState == Slaughter && victim.PkState == Peace` | `PlayerPunish` | `+1` | `min(10, Killer.PkValue + 1)` |
| **5** | `killer.PkState == Peace && victim.PkState == Slaughter` *(Peace self-defense)* | `PlayerPunish` | `0` | Unchanged |
| **6** | `victim.Level <= 50 && victim.PkState == Peace && (killer.Level * 2 >= victim.Level * 3)` | `PlayerPunish` | `+1` | `min(10, Killer.PkValue + 1)` |
| **7** | All other PvP kills (`Fight` vs `Fight`, `Slaughter` vs `Fight`, `Slaughter` vs `Slaughter`) | `PlayerPunish` | `0` | Unchanged |

---

## 3. Victim `PkPunish` (`PkValue 0..10`) Contract

Executed only when `PunishmentMode == PlayerPunish`:

| Victim `PkValue` | `ExpLossPerMil` (`/1000`) | `MaxExpLoss` (`Lv < 130`) | `HighLevelMaxExpLoss` (`Lv >= 130`) | `MoneyDropPerMil` (`/1000`) | Ground Money Spawned | `BagItemDropPerMil` (`/1000`) | `EquipDropPerMil` (`/1000`) | `DurabilityLossPercent` (`%`) |
| :---: | :---: | :---: | :---: | :---: | :---: | :---: | :---: | :---: |
| `0` | `10` | `500,000` | `500,000` | `50` | `floor(lost / 2)` (`2.5%`) | `0` | `0` | `0%` |
| `1` | `20` | `500,000` | `1,000,000` | `100` | `floor(lost / 2)` (`5.0%`) | `50` | `0` | `0%` |
| `2` | `30` | `500,000` | `2,000,000` | `300` | `floor(lost / 2)` (`15.0%`) | `100` | `0` | `0%` |
| `3` | `40` | `500,000` | `5,000,000` | `500` | `floor(lost / 2)` (`25.0%`) | `150` | `0` | `0%` |
| `4` | `50` | `500,000` | `8,000,000` | `500` | `floor(lost / 2)` (`25.0%`) | `200` | `0` | `5%` |
| `5` | `100` | `500,000` | `12,000,000` | `500` | `floor(lost / 2)` (`25.0%`) | `300` | `50` | `10%` |
| `6` | `150` | `500,000` | `16,000,000` | `600` | `floor(lost / 2)` (`30.0%`) | `400` | `100` | `20%` |
| `7` | `200` | `500,000` | `20,000,000` | `700` | `floor(lost / 2)` (`35.0%`) | `500` | `200` | `40%` |
| `8` | `250` | `500,000` | `30,000,000` | `800` | `floor(lost / 2)` (`40.0%`) | `600` | `400` | `60%` |
| `9` | `300` | `500,000` | `40,000,000` | `900` | `floor(lost / 2)` (`45.0%`) | `700` | `800` | `80%` |
| `10` | `350` | `500,000` | `50,000,000` | `1000` | `floor(lost / 2)` (`50.0%`) | `800` | `1000` | `100%` |

### Invariant Formulas
1. **Experience Floor**: `NewExp = Math.Max(-(RequiredLevelExp * 80L) / 100L, CurrentExp - Math.Min((RequiredLevelExp * ExpLossPerMil) / 1000L, EffectiveMaxExpLoss))`.
2. **Ground Money Ownership**: Spawned with `BelongCharacterId = Guid.Empty` (`LootProtectionExpireTick = 0`) so any player in range can pick up the dropped coins immediately.
3. **Drop Exclusions**: Items with `Genre == ItemGenreKind.Quest` or `BindState != 0` are never dropped on the ground during `BagItemDrop` or `EquipDrop` rolls.
4. **Victim PK Decay**: Executed **after** item/money/XP evaluation: if `victim.PkValue > 0` and `victim.Experience * 100L > -(RequiredLevelExp * 50L)`, `victim.PkValue--` and emit `0x93`.

---

## 4. Internal Zone Command & Scripting Contracts

```csharp
// src/JXReborn.ZoneEngine/Combat/SetTemporaryPkCampZoneCommand.cs
public sealed record SetTemporaryPkCampZoneCommand(
    Guid CharacterId,
    long LeaseEpoch,
    bool EnableFreeCamp) : IZoneCommand;

// src/JXReborn.ZoneEngine/Combat/ApplyEnmityZoneCommand.cs
public sealed record ApplyEnmityZoneCommand(
    Guid CharacterId,
    long LeaseEpoch,
    uint TargetNpcId) : IZoneCommand;

// src/JXReborn.ZoneEngine/Combat/StartExerciseSparZoneCommand.cs
public sealed record StartExerciseSparZoneCommand(
    Guid ChallengerCharacterId,
    long ChallengerLeaseEpoch,
    Guid TargetCharacterId) : IZoneCommand;

// src/JXReborn.ZoneEngine/Scripting/IScriptDialogContext.cs
public interface IScriptDialogContext
{
    bool StartExerciseSpar(Guid targetCharacterId);
}
```
