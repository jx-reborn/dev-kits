# Feature Specification: Core PK & Combat Rules

**Feature Branch**: `014-core-pk-combat-rules`  
**Created**: 2026-11-11  
**Status**: Converged  
**Input**: Authoritative Player-versus-Player (PvP) combat rules, normal PK posture transitions, temporary PK camp flag toggling, targeted Enmity duels, penalty-free 1v1 Sparring, exhaustive PvP target hostility evaluation across direct and ranged attacks, PK murder value progression, and tiered PvP death punishments.

## Clarifications

### Session 2026-11-11

- Q: How is a 1v1 Sparring match initiated given that the legacy game client has no standalone sparring button or dedicated sparring request message? → A: Use an engine-first sparring trigger design combining an authoritative internal zone sparring command, a dialog scripting helper for interactive characters, and a reference Arena Sparring Master non-player character challenge flow, introducing zero invented client message types.
- Q: Does the 3-minute cooldown to return to Peace mode apply when a character is standing inside a non-combat Peace Zone? → A: Immediate Peace-mode switching is allowed inside non-combat Peace Zones, while the 3-minute cooldown is strictly enforced whenever the character is inside an active combat zone.
- Q: What happens when an active Enmity duel reaches its 10-minute maximum duration without either combatant dying or leaving? → A: The 10-minute Enmity timeout cleanly closes Enmity on both participating players and notifies both clients, rather than reproducing the legacy reference defect that mistakenly closed Sparring instead of Enmity.
- Q: Can a character in Slaughter mode attack a non-novice, non-party character who is in Peace mode inside a combat zone, and can the attacked Peace-mode character fight back? → A: Yes; Slaughter mode permits attacking non-novice, non-party Peace-mode characters in combat zones, and the attacked Peace-mode character is mutually hostile to that Slaughter-mode attacker so they can retaliate in self-defense without changing their own PK mode or incurring PK murder points if they slay the aggressor.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Normal PK Mode Transitions, Peace-Zone Cooldown Bypass & Camp Flag Toggling (Priority: P1)

A martial artist switches between the three primary combat stances—Peace (monster training), Fight (factional camp warfare), and Slaughter (indiscriminate PK)—and optionally toggles their temporary free-PK camp indicator. The world enforces map-level PK restrictions, severe experience-debt lockouts, and a 3-minute cooldown before returning to Peace inside combat zones while allowing instant Peace restoration inside safe sanctuaries. Upon entering the world, the character immediately receives their current normal PK mode and PK murder value so their interface accurately reflects their standing.

**Why this priority**: Normal PK stances and camp flags are the foundational player controls governing whether any PvP engagement can occur in the open world and must be synchronized accurately from initial login.

**Independent Test**: Can be independently tested by entering both safe sanctuaries and active combat zones, switching across Peace, Fight, and Slaughter modes under varying cooldown timers and experience debt levels, and toggling the temporary camp flag while observing nearby visibility updates.

**Acceptance Scenarios**:

1. **Given** a non-novice character with experience above the `-80%` debt threshold standing in a combat zone where PK switching is permitted, **When** the player switches from Peace to Fight or Slaughter and then requests Peace again within 3 minutes (`180` seconds), **Then** the switch to Fight or Slaughter succeeds immediately, the premature return to Peace is rejected while re-confirming the active mode to the player, and returning to Peace succeeds once 3 full minutes have elapsed.
2. **Given** a character currently in Fight or Slaughter mode with less than 3 minutes elapsed since their last mode change, **When** the character stands inside a non-combat Peace Zone and requests switching to Peace mode, **Then** the cooldown is bypassed and the character immediately transitions to Peace mode.
3. **Given** a character suffering severe experience debt at or below `-80%` of current-level progression (or standing on a map that forbids PK mode changes), **When** the player attempts to switch out of Peace mode into Fight or Slaughter, **Then** the request is denied and the character's current PK mode is re-synchronized unchanged.
4. **Given** a non-novice character in the world, **When** the player enables the temporary PK camp flag and later disables it, **Then** their effective combat camp switches temporarily to the Free camp and broadcasts the camp change to all observers in the surrounding area, and disabling it restores their permanent faction Base Camp and broadcasts the restored camp.
5. **Given** a character logging into a zone with persisted PK mode and PK murder points, **When** initial world entry synchronization completes, **Then** both their active normal PK mode and current PK murder value are delivered so their status display matches server state.

---

### User Story 2 - Exhaustive PvP Hostility & Target Relation Matrix (Priority: P1)

When two players encounter each other in the world and cast single-target, directional, area-of-effect, or ranged projectile skills, the world evaluates an authoritative target-relation matrix across Peace/Combat zones, Novice protection, Party membership, active Sparring, active Enmity, Slaughter stances, Fight stances, and effective combat camps—applying identical rules both at initial cast time and when in-flight projectiles collide ticks later.

**Why this priority**: Incorrect hostility ordering either allows griefing of protected players (novices, safe-zone bystanders, party comrades, warmup duelists) or breaks legitimate combat (preventing Slaughter attacks against Peace targets or blocking self-defense against Slaughter aggressors).

**Independent Test**: Can be independently tested by pairing two characters across every combination of zone fight state, camp, party status, PK stance, Enmity phase, and Sparring state, verifying both immediate melee attacks and delayed-impact ranged projectiles.

**Acceptance Scenarios**:

1. **Given** two players where either player is in a non-combat Peace Zone, either player belongs to the Novice (`Begin`) camp, or both players belong to the same Party, **When** either player casts a melee or ranged offensive skill at the other, **Then** their relationship evaluates as non-hostile (`Friend`) and zero PvP damage or status ailments are applied regardless of Slaughter, Fight, Enmity, or Sparring flags.
2. **Given** two non-novice, non-party players in a combat zone where Player A is in Slaughter mode and Player B is in Peace mode, **When** Player A attacks Player B and Player B counter-attacks Player A with both melee and ranged skills, **Then** both players evaluate as mutually hostile (`Enemy`) so Player A can strike Player B and Player B can retaliate in self-defense without leaving Peace mode.
3. **Given** two non-novice, non-party players in a combat zone who are both in Fight mode, **When** they belong to opposing camps (or either has toggled the temporary Free camp), **Then** they evaluate as mutually hostile (`Enemy`); **When** they share the exact same Justice, Evil, or Balance camp without active Enmity or Sparring, **Then** they evaluate as non-hostile (`Friend`).
4. **Given** a ranged projectile fired while two players are hostile (or non-hostile), **When** the projectile reaches the defender on a later simulation step after either player enters a Peace Zone or changes PK/duel state, **Then** collision resolution evaluates their live relationship using full party, Enmity, Sparring, PK mode, and effective camp rules rather than stale cast-time assumptions.

---

### User Story 3 - Targeted Enmity Warmup, Active Duel & Automatic Termination (Priority: P1)

A non-novice player in a combat zone declares targeted Enmity (*Cừu sát*) against another nearby non-novice player—even if one or both are in Peace mode or share the same faction camp. Both players receive an immediate 10-second (`200` simulation steps) warmup notification during which neither can yet harm the other via Enmity, allowing the target to prepare or escape to safety. Once the warmup finishes, both players enter a 10-minute (`10,800` simulation steps) active mutual duel that ends cleanly upon timeout, participant death, safe-zone entry, map departure, disconnection, or Sparring initiation.

**Why this priority**: Targeted Enmity is the core 1-on-1 grudge resolution mechanic in the martial arts world, requiring strict validation gates, fair warmup warning, launcher tracking for murder penalties, and leak-free termination.

**Independent Test**: Can be independently tested by declaring Enmity between two Peace-mode same-camp characters in a combat zone, verifying non-hostility during the warmup countdown, mutual hostility once active, and clean state closure on both characters across timeout, death, Peace-Zone entry, and logout.

**Acceptance Scenarios**:

1. **Given** two non-novice players within local visibility range inside a combat zone, neither sparring, the initiator not already in Enmity, and the initiator's experience above `-50%` of current-level requirement, **When** the initiator declares Enmity on the target, **Then** both players enter the Enmity Countdown state (`200` simulation steps, approximately `11.1` seconds), receive opponent identity notifications, and cannot yet damage one another if otherwise non-hostile.
2. **Given** two players in the Enmity Countdown state who both remain alive in the combat zone, **When** `200` simulation steps elapse, **Then** both players transition into the Active Enmity state (`10,800` simulation steps = `10` minutes), receive active-duel notifications, and become mutually hostile (`Enemy`) even if both remain in Peace mode or share the same faction camp.
3. **Given** two players in Active Enmity where neither dies or leaves, **When** `10,800` simulation steps elapse, **Then** the server cleanly closes Enmity on both players and notifies both clients that Enmity has ended.
4. **Given** two players in Enmity Countdown or Active Enmity, **When** either player dies, steps into a Peace Zone, transfers maps, disconnects, or starts a Sparring match, **Then** Enmity is immediately closed for both players and synchronized to any remaining connected participant.
5. **Given** an Enmity request where either player is a Novice, either is in a Peace Zone, either is currently Sparring, the target is outside local visibility, the initiator is already in Enmity, or the initiator's experience debt is at or below `-50%`, **When** the initiator submits the Enmity declaration, **Then** the server rejects the request without starting a countdown and sends an explanatory notice to the initiator.

---

### User Story 4 - Penalty-Free 1v1 Sparring & Reference Arena Trigger (Priority: P2)

Two consenting non-novice players in a combat zone engage in friendly 1v1 Sparring (*Tỷ thí*) facilitated through an Arena Sparring Master challenge dialog (or direct zone sparring command). Any existing Enmity on either participant is automatically cleared, both players become mutually hostile for the match, and when one participant defeats the other, the match ends cleanly with zero experience loss, zero money loss, zero item or equipment drops, zero durability abrasion, and zero PK murder point gain.

**Why this priority**: Sparring enables martial artists of any camp or PK stance to test skills and builds in full-damage combat without risking hard-earned experience, wealth, equipment, or criminal PK status.

**Independent Test**: Can be independently tested by challenging a nearby partner via the Arena Sparring Master dialog (or direct zone sparring trigger), fighting until one participant's health reaches zero, and confirming that both exit Sparring with zero currency, experience, item, durability, or PK murder changes.

**Acceptance Scenarios**:

1. **Given** two non-novice players in local visibility inside a combat zone who are not already sparring with someone else, **When** a Sparring match is initiated via the Arena Sparring Master challenge flow (or zone sparring command), **Then** any active Enmity on either player is closed first, both players enter mutual Sparring state, and both receive sparring state notifications naming their opponent.
2. **Given** two players in an active Sparring match, **When** one sparring partner reduces the other's life to zero, **Then** Sparring closes cleanly on both players, the defeated player suffers zero experience loss, zero money loss, zero bag or equipped item drops, zero equipment durability loss, and zero PK value change, and the victor gains zero PK murder points.
3. **Given** two players in an active Sparring match, **When** either participant steps into a Peace Zone, changes maps, or disconnects, **Then** Sparring immediately terminates for both participants and clears the opponent's sparring indicator.

---

### User Story 5 - Slaughter PK Value Progression & Tiered PvP Death Penalties (Priority: P1)

Unlawful player killings increment the killer's PK murder value (clamped from `0` to `10`) according to the combat context—Slaughter kills, Enmity initiator kills, over-leveled bullying of low-level peaceful characters, and third-party Slaughter interference against active Sparring participants—while mutual Fight kills and Enmity target self-defense kills add zero PK points. Conversely, when a player dies in non-sparring PvP combat, they suffer tiered penalties indexed by their current PK value (`0..10`): experience loss (capped by per-tier limits and high-level ceiling tables down to the `-80%` debt floor), inventory money loss (with half of lost coins dropped onto the ground as a lootable money pile), inventory bag item drops, equipped gear drops at high PK tiers (`5..10`), equipped item percentage durability abrasion (`4..10`), and a `-1` PK value decay when their experience is above `-50%`.

**Why this priority**: Tiered PK consequences enforce the social contract of JX Reborn—deterring indiscriminate murder with escalating red-name gear-drop risk while rewarding lawful factional combat and self-defense.

**Independent Test**: Can be independently tested by simulating lethal PvP blows across Sparring, Enmity (initiator vs target), Slaughter, over-leveled Fight, and mutual Fight scenarios, and testing victim deaths across PK values `0` through `10` at both standard (`< 130`) and high (`>= 130`) character levels.

**Acceptance Scenarios**:

1. **Given** a non-sparring PvP kill in a combat zone, **When** the killer is (a) the Enmity initiator slaying their Enmity target, (b) a Slaughter-mode aggressor slaying a Peace-mode victim, (c) a third-party Slaughter-mode player slaying someone currently Sparring with another player, or (d) an over-leveled attacker whose level is at least 1.5 times the level of a Peace-mode victim at or below level `50`, **Then** the killer's PK murder value increases by `+2` for case (a) and `+1` for cases (b), (c), and (d), clamped to a maximum of `10`, and the updated PK value is synchronized to the killer.
2. **Given** a non-sparring PvP kill in a combat zone, **When** the killer is the Enmity target slaying the Enmity initiator in self-defense, a Peace-mode defender slaying a Slaughter-mode attacker in self-defense, or a Fight-mode combatant slaying another Fight-mode enemy of a rival camp, **Then** the killer's PK murder value increases by `0`.
3. **Given** a player with PK value `k` (`0` through `10`) killed by another player outside of Sparring, **When** PvP death punishment resolves, **Then**:
   - Experience is deducted by the per-thousand rate for tier `k` of current-level required experience, capped by `500,000` for levels below `130` or the high-level PK cap table for levels `130` and above, and clamped so current experience never drops below `-80%` of current-level required experience;
   - Carried inventory money is reduced by the tier `k` per-thousand rate, and half of the lost money rounded down (when at least `1`) spawns at a nearby passable coordinate as a free-for-all ground money pile;
   - Drop-eligible bag items and equipped items roll independent drop checks per tier `k` rates and spawn on the ground if dropped, while remaining equipped items suffer tier `k` percentage durability abrasion;
   - If the victim's experience exceeds `-50%` of current-level required experience and their PK value is greater than `0`, the victim's PK murder value decreases by `1` and synchronizes to the client.
4. **Given** a notorious red-name outlaw at PK value `10` killed in non-sparring PvP, **When** death penalties resolve, **Then** `100%` of carried inventory money is lost (with `50%` dropped on the ground), eligible equipped items drop at `100%` rate, any remaining undropped equipment loses `100%` durability, experience drops up to the tier-10 cap, and PK murder value decays from `10` to `9` (provided experience exceeds `-50%`).

---

### Edge Cases

- **Experience Debt Lockout Gates (`-50%` and `-80%`)**: A player whose experience has fallen into negative debt at or below `-80%` of their current level's required experience cannot switch out of Peace mode into Fight or Slaughter, and further PvP death experience deductions halt at the `-80%` floor. A player at or below `-50%` experience debt cannot initiate a targeted Enmity duel and does not receive the `-1` PK murder value reduction upon dying in PvP.
- **Third-Party Slaughter Interference During 1v1 Sparring**: If Player A and Player B are in an active 1v1 Sparring match in an open combat zone and a third-party Player C in Slaughter mode kills Player A, Player A's death is treated as a standard PvP homicide by Player C rather than a sparring defeat: Player C gains `+1` PK murder value, Player A suffers normal PvP death penalties for their current PK tier, and the Sparring state between Player A and Player B closes cleanly on both.
- **Peace-Zone Boundary Crossing, Map Transfer & Sudden Logout**: Whenever a player transitions from a combat zone into a Peace Zone, teleports to another map, dies, or disconnects abruptly, all active Enmity (countdown or active) and Sparring relationships involving that player close immediately on both participants so the partner is never left stranded with a dangling duel lock.
- **PK Value Ceiling (`10`) & Zero Floor (`0`)**: Repeated unlawful murders by a player already at maximum PK value `10` clamp cleanly at `10` without overflow, and `-1` PK decay on a player at PK value `0` clamps cleanly at `0`.
- **Target Leaves Visibility or Enters Peace Zone During Enmity Countdown**: If the Enmity target uses a Town Portal or walks into a Peace Zone during the 200-step warmup countdown, entering the Peace Zone (or departing the map) immediately cancels the countdown on both players before active hostility ever begins.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST support three normal player PK stances—`Peace` (`0`), `Fight` (`1`), and `Slaughter` (`2`)—and synchronize both the player's current normal PK stance and current PK murder value during initial world entry and upon every state change.
- **FR-002**: System MUST enforce map-level PK change permissions and server-side Novice (`Begin` camp) protection (even against raw/forged stance-change packets), and MUST forbid transitioning out of `Peace` mode into `Fight` or `Slaughter` when the player's current experience is at or below `-80%` of the experience required for their current level.
- **FR-003**: System MUST enforce a `3,240`-step (`180`-second at 18 steps per second) cooldown before a player in `Fight` or `Slaughter` mode can switch back to `Peace` mode while inside a combat zone, while allowing immediate cooldown-free switching to `Peace` mode when standing inside a non-combat Peace Zone.
- **FR-004**: System MUST maintain a character's permanent faction Base Camp separately from their effective Current Camp, allowing non-novice players to toggle a temporary PK camp flag that sets their Current Camp to `Free` when enabled, restores their Current Camp to their Base Camp when disabled, and broadcasts the effective camp change to all observers in the surrounding 3x3 area of interest.
- **FR-005**: System MUST evaluate Player-versus-Player combat relationships in strict priority order for both immediate skill hits and in-flight projectile collisions:
  1. Self (`Self`);
  2. Either player in a non-combat Peace Zone, either player in Novice (`Begin`) camp, or both players in the same active Party (`Friend`);
  3. Mutual active 1v1 Sparring targets (`Enemy`);
  4. Mutual Active Enmity targets (`Enemy`, only after the warmup countdown has completed);
  5. Either player in `Slaughter` mode (`Enemy`, enabling both Slaughter aggression against `Peace` or `Fight` targets and self-defense retaliation against `Slaughter` aggressors);
  6. Either player in `Peace` mode (`Friend`);
  7. Both players in `Fight` mode: camp relationship check using each player's effective Current Camp (`Enemy` when camps differ or either player has `Free` camp active; `Friend` when sharing the exact same non-Free camp).
- **FR-006**: System MUST validate targeted Enmity declarations against all 11 ground-truth gates: alive synchronized players, distinct non-self target, target present in the initiator's 3x3 area of interest, initiator in a combat zone, target in a combat zone, initiator not in Novice camp, target not in Novice camp, initiator not Sparring, target not Sparring, initiator not already in Enmity (Countdown or Active), and initiator experience strictly above `-50%` of current-level required experience.
- **FR-007**: Upon accepting a targeted Enmity declaration, System MUST close any pre-existing Enmity on the target, place both players into the Enmity `Countdown` state for `200` simulation steps with the initiator marked as the Enmity launcher, and notify both players with their opponent's runtime identifier and character name.
- **FR-008**: System MUST decrement active Enmity timers once per simulation step, transitioning both participants from `Countdown` to `Active` Enmity for `10,800` simulation steps (`10` minutes) when the `200`-step warmup reaches zero, and cleanly closing Enmity on both players with client notifications when the `10,800`-step active duel timer expires.
- **FR-009**: System MUST provide an engine-level 1v1 Sparring initiation command and an interactive NPC dialog scripting helper—demonstrated by a reference Arena Sparring Master challenge flow—that validates two distinct, alive, non-novice players in local visibility inside a combat zone who are not already sparring, closes any active Enmity on either participant, places both into mutual Sparring state, and synchronizes opponent details to both clients.
- **FR-010**: System MUST immediately close all active Enmity and Sparring states on both participating players—and notify connected clients—whenever either participant dies, enters a non-combat Peace Zone, transfers maps, or disconnects from the zone.
- **FR-011**: When a player is killed by their active Sparring partner, System MUST classify the death as a zero-penalty sparring defeat, applying zero experience loss, zero money loss, zero bag or equipment drops, zero durability abrasion, and zero PK murder value change to either player.
- **FR-012**: When a player is killed by another player outside of mutual Sparring in a combat zone, System MUST update the killer's PK murder value (clamped to `0..10` and synchronized to the killer) as follows:
  - `+2` if the parties were in Active Enmity and the killer was the Enmity launcher (`0` if the killer was the Enmity target);
  - `+1` if the victim was actively Sparring with a third party and the killer was in `Slaughter` mode;
  - `+1` if the killer was in `Slaughter` mode and the victim was in `Peace` mode (`0` if a `Peace`-mode defender killed a `Slaughter`-mode attacker);
  - `+1` if the victim was at or below level `50` in `Peace` mode and twice the killer's level is greater than or equal to three times the victim's level;
  - `0` for mutual `Fight`-mode combat between rival camps.
- **FR-013**: System MUST load declarative PK rules (Peace-mode switch cooldown duration, maximum PK murder value, and the 11-row `0..10` PK punishment table specifying per-thousand experience loss, experience loss cap, per-thousand money drop, per-thousand bag item drop, per-thousand equipped item drop, and percentage equipment durability loss) from the canonical player progression catalog.
- **FR-014**: Upon a punishable PvP death, System MUST deduct victim experience equal to the lesser of `(RequiredLevelExperience * PerThousandExpLoss / 1000)` and the effective maximum experience loss cap—using `500,000` for levels below `130` and the authoritative 11-tier high-level cap table (`500,000` up to `50,000,000`) for levels `130` and above—while clamping final experience so it never falls below `-80%` of required current-level experience.
- **FR-015**: Upon a punishable PvP death, System MUST deduct `(CarriedInventoryMoney * PerThousandMoneyDrop / 1000)` from the victim's carried inventory money, synchronize the updated money balance, and whenever half of the deducted money is at least `1`, spawn a ground money pile containing half of the deducted amount at a passable nearby coordinate with no pickup ownership lock so any nearby player can collect it.
- **FR-016**: Upon a punishable PvP death, System MUST evaluate bag item drop probabilities (`PerThousandBagDrop / 1000`) across drop-eligible inventory bag items and equipped item drop probabilities (`PerThousandEquipDrop / 1000`) across drop-eligible equipped gear—spawning dropped items onto the ground, removing them from the victim's inventory, recalculating character stats, and abrading remaining equipped items by the tier's durability loss percentage.
- **FR-017**: Upon a punishable PvP death, if the victim's current PK murder value is greater than `0` and their experience exceeds `-50%` of required current-level experience, System MUST decrement the victim's PK murder value by `1` (clamped to `0` minimum), synchronize the updated PK value, and persist both normal PK stance and PK murder value across character saves.

### Key Entities

- **Player PK Session State**: Per-player runtime PvP state tracking normal PK stance (`Peace`, `Fight`, `Slaughter`), last PK stance change step, persisted PK murder value (`0..10`), Enmity state (`Closed`, `Countdown`, `Active`), Enmity opponent identifier, Enmity remaining step counter, Enmity initiator flag, and active Sparring opponent identifier.
- **Character Camp Pair (Base Camp & Current Camp)**: Distinguishes a character's permanent sect/alignment camp (`Begin`, `Justice`, `Evil`, `Balance`) from their temporary effective combat camp (`Free` when the PK camp flag is active, or equal to Base Camp otherwise).
- **PK Punishment Rule Tier**: Declarative rule entry indexed by PK murder value (`0..10`) defining per-thousand experience loss, experience loss cap, per-thousand carried money loss, per-thousand bag item drop probability, per-thousand equipped gear drop probability, and equipped durability loss percentage.
- **Arena Sparring Challenge**: Transient two-player consent state managed through an Arena Sparring Master dialog allowing a challenger to propose a penalty-free 1v1 match and an opponent in local visibility to accept it.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: `100%` of valid and invalid PK stance transitions, Peace-Zone immediate cooldown bypasses, `180`-second combat-zone cooldown blocks, `-80%` experience-debt lockouts, and temporary Free-camp broadcasts behave deterministically with zero desynchronized client indicators.
- **SC-002**: `100%` of cells in the multi-factor Player-versus-Player hostility truth table (`Peace Zone × Novice Camp × Party × Sparring × Enmity Phase × Slaughter × Fight × Camp`) resolve identically across both immediate melee/direct skills and delayed-impact projectiles.
- **SC-003**: Targeted Enmity duels enforce `100%` of the 11 validation gates, transition from non-hostile warmup to active mutual combat at exactly `200` simulation steps, and terminate cleanly on both players at `10,800` simulation steps or immediately upon death, Peace-Zone entry, map transfer, or disconnect with zero orphaned duel states.
- **SC-004**: `100%` of 1v1 Sparring defeats result in exact zero loss of experience, carried money, inventory items, equipped gear, and durability, and zero PK murder point gain, whereas third-party `Slaughter` kills on sparring players apply `+1` PK murder value and standard PvP death punishment.
- **SC-005**: Across all 11 PK murder tiers (`0..10`) and character levels (`1..200`), PvP death punishment calculations match reference ground-truth formulas with `100%` mathematical parity—including `-80%` experience debt floors, `50%` ground money object spawning, item/equipment drop exclusions for bound or quest items, durability percentage abrasion, and `-1` PK value decay above `-50%` experience.
- **SC-006**: Sustaining `100` concurrent player duel and projectile engagements within a single map zone adds less than `1` millisecond to the 99th-percentile simulation step duration with zero unhandled exceptions on sudden participant disconnects.

## Assumptions

- **Party Membership Integration**: When evaluating party immunity in PvP target relations (`FR-005`), two players sharing the same non-zero active party/team identifier are treated as `Friend`; if neither player belongs to a party (or before party creation is invoked in a test scenario), party immunity is inactive and evaluation proceeds to Sparring, Enmity, PK stances, and camps.
- **Cross-Spec Dependency on `012` (NPC Dialog & Scripting Runtime)**: The reference Arena Sparring Master challenge flow builds upon the strongly-typed NPC dialog scripting context (`012`), while the core zone sparring command (`FR-009`) also executes standalone in headless and integration harnesses without requiring dialog UI clicks.
- **Cross-Spec Dependency on `013` (Equipment Durability & Repair)**: Percentage durability abrasion on PvP death (`FR-016`) reduces current durability proportionally to maximum durability on equipped items and marks gear reaching zero durability as broken consistent with the item durability rules established in `013`; non-droppable quest, locked, or permanently bound items are excluded from ground drop rolls.
- **Town Portal Escape During Warmup**: Standard Town Portal usage during the 200-step Enmity warmup remains intentionally permitted, matching classic martial arts PK rules where an ambushed player can escape before active Enmity locks in.
