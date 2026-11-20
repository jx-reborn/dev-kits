# Ground-Truth Research: Core PK & Combat Rules (`014-core-pk-combat-rules`)

**Branch**: `014-core-pk-combat-rules` | **Date**: 2026-11-11 | **Constitution**: `v1.2.0` (Principle III — Zero-Hallucination Evidence & Ground-Truth Fidelity)

---

## 1. Authoritative Evidence Sources

Every protocol opcode, frame layout, state machine transition, hostility precedence rule, PK value increment, and death punishment formula in this specification aligns with legacy 6.0 wire contracts and configuration tables:

1. **Legacy 6.0 Wire Framing & Timers**:
   - Client-to-server opcodes `0x54` (`c2s_playerapplysetpk`), `0x76` (`c2s_pkapplychangenormalflag`), `0x77` (`c2s_pkapplyenmity`) and server-to-client opcodes `0x58` (`s2c_npcchangecurcamp`), `0x90` (`s2c_pksyncnormalflag`), `0x91` (`s2c_pksyncenmitystate`), `0x92` (`s2c_pksyncexercisestate`), `0x93` (`s2c_pksyncpkvalue`).
   - State-machine timers (`18` Hz): `EnmityWarmupFrames = 200` (`~11.11s`), `EnmityDurationFrames = 10800` (`10` minutes), `SwitchToPeaceCooldownFrames = 3240` (`3` minutes).
2. **Legacy 6.0 Configuration Tables (`$LEGACY_SERVER_DIR/server1/settings/npc/player/`)**:
   - `pkpunish.txt` — Authoritative 11-row (`PkValue 0..10`) per-mil penalty table, `NormalPKTimeLong = 3240`, `MaxExpLoss = 500000`, `HighLevelMaxExpLossByPkValue` (`Lv >= 130` caps `500,000..50,000,000`), and `AbradePercent` (`0..100`).
   - `pkrate.ini` — Authoritative PK rate parameters (`rate=20`, `FactionPKFaction=1`, `KillerPKFaction=1`, `EnmityPK=2`, `BeKilled=-1`, `ButcherPKExercise=1`, `NotSubPKExpPercent=-50`, `NotEnmityExpPercent=-50`, `NotFightExpPercent=-80`).
3. **Companion Protocol Documentation (`jx-swarm`)**:
   - `jx-swarm/docs/02-game-systems/movement-and-combat.md` & `chat-and-social.md` (`0x54`, `0x76`, `0x77`, `0x90`, `0x91`, `0x92`, `0x93`).
   - `jx-swarm/specs/011-module-pk-and-enmity-duels/research.md` & `contracts/jx-binary-protocol.md`.
4. **Current `JXReborn` Codebase (`jx-server`)**:
   - `data/players/progression.players.json:2450-2554` & `schemas/player-template.schema.json:220-244` — Canonical `pkRules` JSON data already authored and schema-defined!
   - `src/JXReborn.ZoneEngine/Combat/CombatDamageCalculator.cs:64-131` — Current `EvaluateRelation` implementation (contains the ordering bug where `Peace` is checked before `Slaughter` and lacks Party / Enmity / Exercise / `CurrentCamp` inputs).
   - `src/JXReborn.ZoneEngine/Combat/MissileSimulationTickSystem.cs:264-350` — Current projectile collision check (evaluates `EvaluateRelation` with cast-time fields only, missing live Enmity/Exercise/Party/CurrentCamp state).
   - `src/JXReborn.ZoneEngine/WorldZones/MapZoneWorker.cs:1638-1684, 2347-2365, 2685-2714` — Current `TryApplyPlayerDamageToPlayer`, `HandlePlayerDeath` (no PvP `DeathCalcPKValue` or `PkPunish`), and `HandleChangePkMode` (missing Peace-Zone cooldown bypass and `-80%` XP debt gate).
   - `src/JXReborn.Adapter.Legacy/Framing/ProtocolSizeTable.cs:39-75` — Currently validates `0x76` (`2B`), missing `0x54` (`c2s_playerapplysetpk` => `2B`) and `0x77` (`c2s_pkapplyenmity` => `5B`).
   - `src/JXReborn.Adapter.Legacy/GameServer/GameServerClientSession.WorldSink.cs:11-46` — `SendInitialWorldSyncBurst` does not yet emit `0x90` (`s2c_pksyncnormalflag`) and `0x93` (`s2c_pksyncpkvalue`) on initial world entry.

---

## 2. Normal PK Modes, Cooldowns, XP Debt Gate & Camp Flag Toggling

### 2.1 Normal PK Modes (`0x76` $\rightarrow$ `0x90`)
- **Legacy 6.0 3-State Enum (`PlayerPkMode`)**:
  - While early 2003 protocol specifications treated the normal PK flag as boolean (`0`/`1`), legacy 6.0 extended it to three states, modeled in `JXReborn.Primitives.Combat.PlayerPkMode`:
    - `0` = `Peace` (*Luyện công*): PvE monster grinding stance; cannot initiate attacks on non-Slaughter players.
    - `1` = `Fight` (*Chiến đấu*): Factional / camp warfare stance; hostile to rival camps and `Free` camp.
    - `2` = `Slaughter` (*Đồ sát*): Indiscriminate PK stance; hostile to all non-novice, non-party players in combat zones.
- **3-Minute Cooldown in Combat Zones vs. Immediate Switch in Peace Zones**:
  - `pkpunish.txt:2` and `progression.players.json:2451` specify `NormalPKTimeLong` / `switchToPeaceCooldownFrames = 3240` frames (`3240 / 18 = 180` seconds = `3` minutes).
  - Legacy 6.0 stance-switching rules:
    - When `player.FightMode == 0` (the character is standing inside a non-combat Peace Zone such as a town or village sanctuary), switching to `Peace` (`0`) bypasses the `3,240`-frame timer (`bForce = 1` / Peace-Zone immediate switch).
    - When `player.FightMode != 0` (active combat zone), switching from `Fight` (`1`) or `Slaughter` (`2`) back to `Peace` (`0`) requires `(currentTick - LastPkModeChangeTick) >= SwitchToPeaceCooldownFrames` (`3,240` frames). Premature requests are rejected and re-emit `0x90` (`s2c_pksyncnormalflag`) with the unchanged `PkState` so the client UI button reverts cleanly.
- **Novice (`Camp = Begin`) Server-Side Hardening & Experience Debt Gate (`NotFightExpPercent = -80`)**:
  - `pkrate.ini:10` defines `NotFightExpPercent = -80`.
  - Live `T=0` Differential Parity (`J05`) confirms the legacy server checks map PK rules, cooldown, and `-80%` XP debt without enforcing `Camp != Begin` on raw `0x76` packet receipt (relying on client UI locking while relation checks still block damage). `JXReborn` (`MapZoneWorker.HandleChangePkMode`) intentionally hardens `0x76` **server-side** by rejecting `Fight`/`Slaughter` transitions whenever `BaseCamp == EntityCamp.Begin` (documented intentional packet-injection hardening in `DifferentialParityPkAndSparringTests`).
  - Characters suffering severe negative experience debt where `Experience * 100 <= RequiredLevelExp * (-80)` (`NotFightExpPercent`) are forbidden from switching out of `Peace` (`0`) into `Fight` (`1`) or `Slaughter` (`2`).

### 2.2 Temporary PK Camp Flag (`0x54` $\rightarrow$ `0x58`)
- **Wire Protocol & Camp Separation**:
  - Client sends `c2s_playerapplysetpk` (`0x54`, 2 bytes: `[0]=0x54, [1]=PkCampFlag`).
  - Entities separate permanent faction camp `BaseCamp` from active combat camp `CurrentCamp`:
    - If `PkCampFlag != 0` (and `BaseCamp != EntityCamp.Begin`): sets `CurrentCamp = EntityCamp.Free` (`4`).
    - If `PkCampFlag == 0`: restores `CurrentCamp = BaseCamp`.
  - Whenever `CurrentCamp` changes, the server broadcasts `s2c_npcchangecurcamp` (`0x58`, 6 bytes: `[0]=0x58, [1..4]=uint32 ID, [5]=byte Camp`) to all players in the 3x3 spatial AOI.
- **Design Decision in `JXReborn`**:
  - Refactor `PlayerZoneState` to store `BaseCamp` (initialized from faction/character DTO, replacing single-field `Camp` ambiguity while keeping `CurrentCamp` initialized to `BaseCamp`). All AOI full/compact sync packets (`0x4C` / `0x4D`) and `EvaluateRelation` read `CurrentCamp`, while `0x54` mutates `CurrentCamp` without corrupting persisted `BaseCamp`.

### 2.3 Initial World Entry Synchronization (`0x90` & `0x93`)
- Upon entering a zone (`SendInitialWorldSyncBurst` in `GameServerClientSession.WorldSink.cs` / `MapZoneWorker` attach), the server MUST include `PkSyncNormalFlagS2CPacket` (`0x90`, 2B) and `PkSyncPkValueS2CPacket` (`0x93`, 5B) so the client's PK mode orb and PK value readout immediately reflect `playerState.PkState` and `playerState.PkValue`.

---

## 3. Targeted Enmity (`Cừu Sát`) Lifecycle & Bug Fix

### 3.1 Request Packet & 11 Validation Gates
- **Wire Command**: `c2s_pkapplyenmity` (`0x77`, 5 bytes: `[0]=0x77`, `[1..4]=uint32 TargetNpcId` LE).
- **11 Validation Gates**:
  1. Both initiator and target are valid, alive, sync-completed `Player` entities (`CurrentLife > 0`, `Doing != Death`).
  2. `initiator.CharacterId != target.CharacterId` (cannot declare Enmity on self).
  3. Target is located within the initiator's 3x3 spatial region AOI.
  4. Initiator is in a combat zone (`initiator.FightMode != 0`).
  5. Target is in a combat zone (`target.FightMode != 0`).
  6. Initiator is not a Novice (`initiator.BaseCamp != EntityCamp.Begin`).
  7. Target is not a Novice (`target.BaseCamp != EntityCamp.Begin`).
  8. Initiator is not currently Sparring (`!initiatorPk.IsExercising`).
  9. Target is not currently Sparring (`!targetPk.IsExercising`).
  10. Initiator is not already in Enmity (`initiatorPk.EnmityState == PkEnmityState.Closed`). *(Note: if the target had a prior Enmity with someone else, their prior Enmity is closed before binding the new Enmity.)*
  11. Initiator's experience is strictly above `-50%` of current-level required experience (`NotEnmityExpPercent = -50` in `pkrate.ini:9`: `Experience * 100 > RequiredLevelExp * (-50)`).

### 3.2 State Machine (`0` $\rightarrow$ `1` $\rightarrow$ `2` $\rightarrow$ `0`) & Legacy Bug Fix
- **Timer Constants**:
  - `PK_ANMITY_TIME = 200` frames (`~11.11s` at 18 Hz) — `PkEnmityState.Countdown = 1` (`enumPK_ENMITY_STATE_TIME`).
  - `PK_ANMITY_CLOSE_TIME = 10800` frames (`600s` = `10` minutes at 18 Hz) — `PkEnmityState.Active = 2` (`enumPK_ENMITY_STATE_PKING`).
- **Warmup Phase (`Countdown = 1`, `200` frames)**:
  - `initiator` records `(State = Countdown, AimCharacterId = target, RemainingFrames = 200, IsLauncher = true)`.
  - `target` records `(State = Countdown, AimCharacterId = initiator, RemainingFrames = 200, IsLauncher = false)`.
  - Both receive `PK_ENMITY_STATE_SYNC` (`0x91`, `btState = 1`, `dwNpcID = opponent.RuntimeNpcId`, `szName = TCVN3(opponent.RoleNameUnicode)`).
  - Crucially, relation evaluation ONLY grants `Enemy` when `EnmityState == PkEnmityState.Active` (`2`). During the 200-frame warmup, if both are `Peace` or same-camp `Fight`, they CANNOT damage each other yet.
- **Active Duel Phase (`Active = 2`, `10,800` frames)**:
  - When `RemainingFrames` decrements to `0` in `Countdown`, both transition to `PkEnmityState.Active` (`2`) with `RemainingFrames = 10800` and receive `0x91` (`btState = 2`, `dwNpcID = opponent.RuntimeNpcId`, `szName = opponentName`).
- **10-Minute Expiry Fix**:
  - Some legacy 2003 implementations accidentally triggered sparring cleanup instead of enmity cleanup when the 10-minute timer expired, leaving active Enmity stuck forever!
  - Per Session `2026-11-11` clarification #3, `JXReborn` fixes this defect: when `RemainingFrames` reaches `0` in `PkEnmityState.Active`, the tick system invokes `CloseEnmity` on both participants, resetting `EnmityState = Closed` and sending `0x91` (`btState = 0, dwNpcID = 0`) to both clients.
- **Immediate Termination Triggers (`CloseAll`)**:
  - `CloseAll` closes both Sparring and Enmity on both the player and their opponent (`0x91` `btState = 0`, `0x92` `btState = 0`) whenever:
    1. Either player dies;
    2. Either player's `FightMode` transitions to `0` upon entering a Peace Zone;
    3. Either player detaches from the zone (logout, socket close, heartbeat timeout, lease superseded, or map transfer via `IPlayerFeatureSlot.OnPlayerDetached`);
    4. Either player begins a 1v1 Sparring match (closes any active Enmity on both participants).

---

## 4. 1v1 Sparring (`Exercise` / `Tỷ Thí`) & Engine-First Trigger Architecture

### 4.1 Ground Truth: Zero C2S Sparring Opcode
- The legacy 6.0 protocol has **no** standalone C2S opcode to initiate 1v1 Sparring (`Exercise`); instead it is initiated server-side by script/dialog actions.
- **Wire Synchronization (`0x92`)**:
  - Note on `ProtocolOpcodes.cs:155` comment: `ProtocolOpcodes.cs` previously carried a stale `// 146 (6B)` comment next to `s2c_pksyncexercisestate = 0x92`, whereas wire captures confirm `0x92` is **identical in layout to `0x91`**: variable-length `[0]=0x92, [1..2]=uint16 m_wLength (TotalSize - 1), [3]=byte m_btState (0 or 1), [4..7]=uint32 m_dwNpcID, [8..8+N-1]=char m_szName[N]` (`8` bytes when closing with empty name, `8 + N` bytes when opening with `N`-byte TCVN3 opponent name)!

### 4.2 Engine-First Sparring Design (Clarification #1)
1. **Zone Command**: `StartExerciseSparZoneCommand(Guid ChallengerCharacterId, long ChallengerLeaseEpoch, Guid TargetCharacterId)` handled on the deterministic 18-Hz `MapZoneWorker` loop.
   - Validates: distinct alive sync-completed players in the same zone & 3x3 AOI, both `FightMode != 0`, neither `BaseCamp == Begin`, neither already Sparring.
   - Calls `CloseEnmity` on both players, sets mutual `ExerciseAimCharacterId`, and emits `0x92` (`btState = 1`, opponent `RuntimeNpcId` & TCVN3 name) to both players.
2. **Scripting API Helper**: `IScriptDialogContext.StartExerciseSpar(Guid targetCharacterId)` (in `JXReborn.ZoneEngine/Scripting/IScriptDialogContext.cs`) allowing any C# 14 `INpcDialogScript` to trigger mutual 1v1 Sparring cleanly without touching wire opcodes.
3. **Reference NPC Script**: `ArenaSparringMasterDialogScript` (`src/JXReborn.Scripts/Pvp/ArenaSparringMasterDialogScript.cs`) providing a concrete challenge/accept flow between two nearby combat-zone players.
4. **Death & Third-Party Interference Rules**:
   - Killed by `ExerciseAim`: resolves as `NoPunish` with **0** XP/money/bag/equip/durability/`PkValue` loss, and `CloseAll()` closes Sparring on both (`0x92` `btState = 0`).
   - Killed by a third-party `Slaughter` player while Sparring: per `pkrate.ini:7` (`ButcherPKExercise = 1`), adds `+1` `PkValue` to the third-party `Slaughter` killer, applies standard `PlayerPunish` penalties to the victim, and `CloseAll()` terminates the victim's Sparring session with their original partner.

---

## 5. Exhaustive PvP Relation Precedence Order

### 5.1 Defects in Current `CombatDamageCalculator.EvaluateRelation`
In `src/JXReborn.ZoneEngine/Combat/CombatDamageCalculator.cs:106-131`:
1. Line `117` checks `if (attackerPkMode == PlayerPkMode.Peace || defenderPkMode == PlayerPkMode.Peace) return Friend;` **BEFORE** line `122` (`Slaughter`)! That made `Peace` players completely immune to `Slaughter` (`Đồ sát`) attackers and unable to fight back!
2. It has no awareness of Party membership, active Sparring (`Exercise`), active Enmity (`PkEnmityState.Active`), or `BaseCamp` vs `CurrentCamp`.
3. `MissileSimulationTickSystem.cs:310-355` only stored `LauncherCamp` and `LauncherPkMode` at spawn time, ignoring live Enmity, Sparring, Party, `FightMode`, and `CurrentCamp` at impact time.

### 5.2 Verified 9-Step PvP Relation Precedence Order
For two `Player` entities `A` (attacker) and `B` (defender):
1. **Identity**: If `A.RuntimeNpcId == B.RuntimeNpcId` $\rightarrow$ `EntityRelationKind.Self`.
2. **Peace Zone Protection**: If `A.FightMode == 0 || B.FightMode == 0` $\rightarrow$ `EntityRelationKind.Friend`.
3. **Novice Protection**: If `A.BaseCamp == EntityCamp.Begin || B.BaseCamp == EntityCamp.Begin` $\rightarrow$ `EntityRelationKind.Friend`.
4. **Same Party Immunity**: If `A.PartyId != 0 && A.PartyId == B.PartyId` $\rightarrow$ `EntityRelationKind.Friend`.
5. **Mutual 1v1 Sparring (`Exercise`)**: If `A` is actively Sparring with `B` (`A.ExerciseAim == B.CharacterId && B.ExerciseAim == A.CharacterId`) $\rightarrow$ `EntityRelationKind.Enemy`.
6. **Mutual Active Enmity (`PkEnmityState.Active`)**: If `A.EnmityState == Active && A.EnmityAim == B.CharacterId` (note: `Countdown` state `1` does **NOT** trigger this rule!) $\rightarrow$ `EntityRelationKind.Enemy`.
7. **Slaughter Stance & Self-Defense**: If `A.PkState == PlayerPkMode.Slaughter || B.PkState == PlayerPkMode.Slaughter` $\rightarrow$ `EntityRelationKind.Enemy` (both allows `Slaughter` attacker to hit `Peace`/`Fight` target AND allows `Peace`/`Fight` target to retaliate in self-defense!).
8. **Peace Stance Protection**: If `A.PkState == PlayerPkMode.Peace || B.PkState == PlayerPkMode.Peace` $\rightarrow$ `EntityRelationKind.Friend`.
9. **Mutual Fight Stance (`Fight` vs `Fight`)**: Evaluate `CampRelationTable[A.CurrentCamp, B.CurrentCamp]`, with the Free-camp symmetry rule: if `A.CurrentCamp == EntityCamp.Free || B.CurrentCamp == EntityCamp.Free` $\rightarrow$ `EntityRelationKind.Enemy`; else if `A.CurrentCamp == B.CurrentCamp` $\rightarrow$ `EntityRelationKind.Friend`; else $\rightarrow$ `EntityRelationKind.Enemy`.

By encapsulating PK/party state in a lightweight `readonly record struct PkRelationContext` and resolving live `PlayerZoneState` for both `LauncherNpcId` and `DefenderNpcId` inside `MissileSimulationTickSystem.StepTick` (with fallback to spawn snapshot if the launcher detached mid-flight), both direct skills and delayed projectiles share 100% identical PvP relation semantics with zero heap allocations.

---

## 6. Killer `PkValue` & Victim `PkPunish` Tiered PvP Death Penalties

### 6.1 Killer `PkValue` Calculation (`pkrate.ini`)
When player `Victim` is killed by player `Killer` in a combat zone (`Victim.FightMode != 0`), clamped to `[0, MaxPkValue = 10]`:

| Scenario | Killer `PkValue` Delta | Victim Death Mode | Config / Rule Citation |
| :--- | :---: | :---: | :--- |
| Mutual 1v1 Sparring (`Victim.ExerciseAim == Killer`) | `+0` | `NoPunish` | Sparring zero-penalty invariant |
| Third-party `Slaughter` kills Sparring `Victim` (`Victim.IsExercising && Killer.PkState == Slaughter`) | `+1` (`ButcherPKExercise`) | `PlayerPunish` | `pkrate.ini:7` |
| Active Enmity & `Killer.IsEnmityLauncher == true` | `+2` (`EnmityPK`) | `PlayerPunish` | `pkrate.ini:5` |
| Active Enmity & `Killer.IsEnmityLauncher == false` (target self-defense) | `+0` | `PlayerPunish` | Enmity self-defense rule |
| `Killer.PkState == Slaughter` kills `Victim.PkState == Peace` | `+1` | `PlayerPunish` | Aggressor murder rule |
| `Killer.PkState == Peace` kills `Victim.PkState == Slaughter` (self-defense) | `+0` | `PlayerPunish` | Lawful self-defense rule |
| `Victim.Level <= 50`, `Victim.PkState == Peace`, `Killer.Level * 2 >= Victim.Level * 3` | `+1` (`KillerPKFaction` / `FactionPKFaction`) | `PlayerPunish` | `pkrate.ini:3-4` |
| Mutual `Fight` (or `Fight` vs `Slaughter` rival) | `+0` | `PlayerPunish` | Lawful faction combat |

Whenever `Killer.PkValue` changes, emit `PkSyncPkValueS2CPacket` (`0x93`, 5B) to `Killer`.

### 6.2 Victim `PkPunish` (`pkpunish.txt` & High-Level Caps)

`data/players/progression.players.json:2450-2554` already contains the exact 11-row penalty table (`pkpunish.txt`, where rates are **per-mil `/ 1000`**, except `durabilityLoss` which is **percent `/ 100`**):

| `PkValue` | `expLossPerMil` | `maxExpLoss` (`Lv < 130`) | `HighLevelMaxExpLoss` (`Lv >= 130`) | `moneyDropPerMil` | `bagItemDropPerMil` | `equipDropPerMil` | `durabilityLoss` (`%`) |
| :---: | :---: | :---: | :---: | :---: | :---: | :---: | :---: |
| `0` | `10` (1.0%) | `500,000` | `500,000` | `50` (5%) | `0` (0%) | `0` (0%) | `0%` |
| `1` | `20` (2.0%) | `500,000` | `1,000,000` | `100` (10%) | `50` (5%) | `0` (0%) | `0%` |
| `2` | `30` (3.0%) | `500,000` | `2,000,000` | `300` (30%) | `100` (10%) | `0` (0%) | `0%` |
| `3` | `40` (4.0%) | `500,000` | `5,000,000` | `500` (50%) | `150` (15%) | `0` (0%) | `0%` |
| `4` | `50` (5.0%) | `500,000` | `8,000,000` | `500` (50%) | `200` (20%) | `0` (0%) | `5%` |
| `5` | `100` (10.0%) | `500,000` | `12,000,000` | `500` (50%) | `300` (30%) | `50` (5%) | `10%` |
| `6` | `150` (15.0%) | `500,000` | `16,000,000` | `600` (60%) | `400` (40%) | `100` (10%) | `20%` |
| `7` | `200` (20.0%) | `500,000` | `20,000,000` | `700` (70%) | `500` (50%) | `200` (20%) | `40%` |
| `8` | `250` (25.0%) | `500,000` | `30,000,000` | `800` (80%) | `600` (60%) | `400` (40%) | `60%` |
| `9` | `300` (30.0%) | `500,000` | `40,000,000` | `900` (90%) | `700` (70%) | `800` (80%) | `80%` |
| `10` | `350` (35.0%) | `500,000` | `50,000,000` | `1000` (100%) | `800` (80%) | `1000` (100%) | `100%` |

Step-by-step execution in `PvpDeathPenaltyCalculator` / `MapZoneWorker.HandlePlayerDeath`:
1. **Experience Deduction & `-80%` Debt Floor**:
   - Let `reqExp = GetRequiredExpForNextLevel(victim.Level)`.
   - `rawLoss = (reqExp * rule.ExpLossPerMil) / 1000L`.
   - `effectiveCap = victim.Level >= 130 ? TablePunishExp[pkVal] : rule.MaxExpLoss`.
   - `cappedLoss = Math.Min(rawLoss, effectiveCap)`.
   - Let `minAllowedExp = -(reqExp * 80L) / 100L` (`NotFightExpPercent = -80`).
   - `victim.Experience = Math.Max(minAllowedExp, victim.Experience - cappedLoss)`, followed by `SendPlayerExpSync`.
   - If `victim.Experience <= minAllowedExp` and `victim.PkState != PlayerPkMode.Peace`, force `victim.PkState = PlayerPkMode.Peace` and emit `0x90`.
2. **Inventory Money Loss & `moneyLost / 2` Ground Spawn**:
   - `moneyLost = (int)((long)victim.InventoryMoney * rule.MoneyDropPerMil / 1000L)`.
   - Deduct `moneyLost` from `victim.InventoryMoney` and send `SendMoneySync`.
   - `groundMoney = moneyLost / 2`; if `groundMoney > 0`, spawn a `GroundObjectKind.Money` ground object near `victim`'s position with `BelongCharacterId = Guid.Empty` (free-for-all ownership) and broadcast `0x64` (`s2c_objadd`) to 3x3 AOI.
3. **Bag Item Drop Roll (`bagItemDropPerMil / 1000`)**:
   - For each drop-eligible item in `ItemContainerPlace.EquipRoom` (excluding `ItemGenreKind.Quest`, `ItemGenreKind.TownPortal` if bound, or non-zero `BindState`), roll `random.Next(1000) < rule.BagItemDropPerMil`. On hit, remove from `victim.Inventory` (`SendItemRemove`), spawn on the ground via `GroundObjectTickSystem` (`BelongCharacterId = Guid.Empty`), and broadcast `0x64`.
4. **Equipped Item Drop Roll (`equipDropPerMil / 1000`) & `AbradePercent` Durability Loss (`durabilityLoss %`)**:
   - If `rule.EquipDropPerMil > 0`, evaluate drop-eligible items in `ItemContainerPlace.Equip` (`random.Next(1000) < rule.EquipDropPerMil`), removing dropped equipment (`SendItemRemove`), recalculating aggregated equipment stats on `victim`, and spawning ground item objects (`0x64`).
   - If `rule.DurabilityLossPercent > 0`, for every remaining item in `ItemContainerPlace.Equip` with `MaxDurability > 0`:
     - `loss = Math.Max(1, (item.MaxDurability * rule.DurabilityLossPercent) / 100)` (or full `CurrentDurability` when `100%`).
     - `item.CurrentDurability = (short)Math.Max(0, item.CurrentDurability - loss)`; if `0`, set `item.IsBroken = true`, recalculate stats, and sync updated item state.
5. **Victim `-1` `PkValue` Decay (`BeKilled = -1`, `NotSubPKExpPercent = -50`)**:
   - Per `pkrate.ini:6,8`, if `victim.PkValue > 0` and `victim.Experience * 100L > -(reqExp * 50L)`, decrement `victim.PkValue` by `1` and emit `0x93` (`s2c_pksyncpkvalue`).
