# Feature Specification: Core Equipment Durability, Golden Gear & Set Bonuses

**Feature Branch**: `013-core-equipment-durability-and-sets`  
**Created**: 2026-11-11  
**Status**: Ready for Implementation  
**Input**: Core equipment lifecycle covering combat and movement durability abrasion, broken equipment stat suspension and repair restoration, deterministic Golden equipment generation with fixed magic affixes, and multi-piece Classic and Extended equipment set bonuses.

---

## Clarifications

### Session 2026-11-11

- Q: What happens when an equipped item reaches `0` durability during active combat or movement, especially when the player's inventory bag is completely full? → A: When equipped gear reaches `0` durability, it remains in its equipment slot marked **Broken** (`0 / Maximum` durability) with 100% of its base attributes, explicit magic affixes, hidden Five-Elements adjacency links, and equipment set piece contributions immediately suspended until repaired. This avoids full-bag cursor-lock or accidental ground-drop edge cases mid-combat, while preventing broken items from being re-equipped if manually unequipped into the bag.
- Q: Which actions wear down which equipment slots, and at what probabilities? → A: Following authentic Version 6.0 gameplay rules, each abrasion check rolls independently to deduct `1` durability point: (1) casting an offensive skill while in combat mode rolls weapon abrasion at `1 / 256`; (2) suffering net health loss from incoming damage rolls abrasion independently at `1 / 320` on helm, body armor, belt, and cuffs (boots do not abrade on defense); (3) crossing a spatial region boundary rolls abrasion at `1 / 2560` on boots and `1 / 5120` on the horse mount (masks never abrade). Gear without a finite durability rating or bearing the indestructible property never abrades.
- Q: How do Golden equipment sets unlock hidden bonuses across Classic sets versus Extended sets? → A: Both Version 6.0 set architectures are supported simultaneously: (1) **Classic Golden Sets** track intact equipped pieces belonging to the same classic set; once the count of distinct intact pieces reaches the set's full piece requirement, all three hidden magic affix slots on every equipped piece of that set activate unconditionally without requiring Five-Elements adjacency; (2) **Extended Sets** track the count of distinct intact set piece indices worn by the character, activating each piece's first extended set bonus affix at the partial piece threshold and both the first and second extended set bonus affixes at the full piece threshold.

---

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Combat & Movement Equipment Durability Abrasion (Priority: P1)

As a player fighting monsters, dueling rivals, or traveling across regions, my equipped weapon, armor pieces, boots, and mount gradually wear down according to authentic Version 6.0 probabilities, and my client receives immediate durability updates whenever an item loses a point of durability so I can monitor wear before gear breaks.

**Why this priority**: Without real-time durability abrasion, equipment functions as permanent infinite-stat gear, removing a core economic gold sink and combat readiness loop.

**Independent Test**: Equip a character with finite-durability weapon, helm, body armor, belt, cuffs, boots, horse, mask, and an indestructible piece; execute combat attacks, incoming damage hits, and region crossings under deterministic random seeds; verify exact durability deductions per slot and verify immediate durability update notifications on every change.

**Acceptance Scenarios**:

1. **Given** a character in combat mode wielding an intact weapon with `50 / 50` durability, **When** the character casts offensive skills and an attack abrasion roll succeeds (`1 / 256` chance per skill cast), **Then** the weapon's current durability decreases to `49 / 50`, all non-weapon slots remain unchanged, and the player receives an immediate durability update notification.
2. **Given** a character wearing intact helm, body armor, belt, cuffs, and boots, **When** the character suffers net health loss from an incoming hit, **Then** helm, body armor, belt, and cuffs each roll abrasion independently at `1 / 320`, boots never lose durability from defense, and each slot whose roll succeeds decreases by `1` durability point with an immediate notification.
3. **Given** a character wearing intact boots, riding a horse mount, and wearing a mask, **When** the character moves across a spatial region boundary, **Then** boots roll abrasion at `1 / 2560`, the horse mount rolls abrasion at `1 / 5120`, and the mask never loses durability.
4. **Given** a character wearing an item that either has no finite durability rating or possesses the indestructible property, **When** any number of attacks, incoming hits, or region crossings occur, **Then** the item's durability never decreases and the client sees the item presented as permanently indestructible.

---

### User Story 2 - Broken Equipment Stat Suspension & Repair Restoration (Priority: P1)

As a player whose weapon or armor wears down to `0` durability mid-fight, the broken item stays safely in its equipment slot rather than vanishing or locking my cursor when my bag is full, immediately suspending all its stats, Five-Elements links, and set contributions until I repair it back to working condition.

**Why this priority**: Broken stat suspension and repair restoration complete the durability lifecycle; without them, `0`-durability gear either continues granting free stats or causes catastrophic item loss when bags are full.

**Independent Test**: Reduce an equipped life-boosting armor piece to `0` durability during combat with a full bag, verify the item stays in its equipment slot marked Broken while all its stats and elemental links suspend and player vitals clamp to the new maximums, then repair the item and verify gold deduction, durability restoration, and full stat reactivation.

**Acceptance Scenarios**:

1. **Given** a character with a completely full inventory bag wearing body armor at `1 / 85` durability that grants defense, maximum life, and a Five-Elements activation link to adjacent gear, **When** incoming damage reduces the body armor to `0 / 85` durability, **Then** the armor remains in the body equipment slot marked Broken, 100% of its base defense, magic affixes, and outgoing/incoming Five-Elements links are suspended, and the character's current life clamps down if it exceeds the recalculated maximum life.
2. **Given** a character who manually unequips a Broken (`0 / 85`) item from its equipment slot into the inventory bag, **When** the character attempts to equip that Broken item again before repairing it, **Then** the equip attempt is rejected and the item remains unequipped.
3. **Given** a character holding damaged or Broken standard/blue equipment and sufficient gold, **When** a standard repair operation executes, **Then** the calculated gold cost (scaled by base item price, missing durability ratio, and total magic affix tier levels) is deducted, current durability restores to maximum durability, the Broken state clears, and if equipped, all suspended stats immediately reactivate.
4. **Given** a character holding damaged or Broken Golden equipment, **When** repaired via **Costly Repair**, **Then** the character pays three times the simple Golden repair rate and current durability restores to full maximum durability without reducing maximum durability; **When** repaired via **Simple (Cheap) Repair**, **Then** the character pays the base Golden repair rate, maximum durability permanently decreases by one-tenth of the missing durability (minimum `1` point lost, never dropping below `1` maximum durability), and current durability restores to the new maximum durability.

---

### User Story 3 - Golden Equipment Generation & Fixed Affix Activation (Priority: P1)

As a player acquiring named Golden equipment (such as faction-specific legendary pieces), each generated Golden piece carries its canonical identity, base defense or weapon damage, durability, and up to six deterministic fixed magic affixes rolled within their authored value ranges and synchronized accurately to my client tooltip.

**Why this priority**: Named Golden equipment is the signature endgame progression tier in JX Reborn; accurate generation and client synchronization are mandatory prerequisites for Golden equipment sets and end-game progression.

**Independent Test**: Generate multiple named Golden items across fixed random seeds, verify rolled base attributes and all six fixed magic affix slots fall strictly within authored bounds, verify client synchronization displays the exact named Golden item and affix values, and verify persistence preserves Golden identity across logout and login.

**Acceptance Scenarios**:

1. **Given** a Golden equipment definition specifying base attributes and six fixed magic affix bindings (three explicit slots and three hidden slots), **When** an instance is generated with a specific seed, **Then** the item receives Golden quality, its canonical Golden catalog index, rolled durability, and six populated magic attributes whose values lie inclusively within each binding's authored minimum and maximum bounds.
2. **Given** a generated Golden item placed into a player's inventory or equipment slot, **When** synchronized to the game client, **Then** the client receives the Golden quality marker and zero-based Golden catalog index so the client renders the authentic orange/gold item name, sprite, set roster, and affix lines.
3. **Given** a single Golden item equipped without its remaining set pieces, **When** adjacent equipped items satisfy Five-Elements generation links, **Then** its explicit slots (`1st`, `3rd`, `5th` affix positions) apply unconditionally and its hidden slots (`2nd`, `4th`, `6th` affix positions) activate progressively via standard Five-Elements adjacency rules.
4. **Given** a character owning equipped or bagged Golden equipment, **When** the character logs out and re-enters the world, **Then** the Golden item's canonical catalog index, durability, broken state, and rolled magic affix values are preserved identically.

---

### User Story 4 - Multi-Piece Classic & Extended Equipment Set Bonuses (Priority: P2)

As a player collecting and equipping multiple pieces of a Classic Golden Set or an Extended Equipment Set, completing partial or full piece thresholds automatically unlocks hidden magic affixes or tiered set bonus affixes across my intact set pieces, and breaking or unequipping a piece immediately revokes thresholds no longer met.

**Why this priority**: Multi-piece set completion drives long-term character itemization, trading, and boss hunting, building directly upon Golden item generation and durability state tracking.

**Independent Test**: Equip pieces of both a Classic Golden Set (both early faction sets and high-numbered catalog sets) and an Extended Set progressively from 1 piece to full completion, assert exact stat deltas at partial and full thresholds, break one piece to `0` durability, and verify set bonuses immediately step down until repaired.

**Acceptance Scenarios**:

1. **Given** a Classic Golden Set requiring $N$ pieces (whether derived from the total piece count of classic faction sets `1..82` or defined explicitly in the equipment set catalog for sets `83+`), **When** a character equips fewer than $N$ intact pieces of that set, **Then** hidden affix slots on those pieces rely solely on standard Five-Elements adjacency links.
2. **Given** a character who equips all $N$ distinct intact pieces of a Classic Golden Set regardless of whether adjacent non-set slots satisfy Five-Elements generation, **When** equipment bonuses are evaluated, **Then** all three hidden affix slots on every equipped intact piece of that Classic Golden Set activate unconditionally.
3. **Given** an Extended Equipment Set defining a partial piece threshold (e.g., 3 distinct pieces) and a full piece threshold (e.g., 5 distinct pieces), **When** a character equips intact pieces matching the partial threshold of distinct set piece indices, **Then** the first extended bonus affix activates on each equipped piece of that Extended Set; **When** the character reaches the full threshold of distinct piece indices, **Then** both the first and second extended bonus affixes activate on each equipped piece of that Extended Set.
4. **Given** a character enjoying an active Classic full-set bonus or Extended set bonus, **When** one required set piece reaches `0` durability (becomes Broken) or is unequipped, **Then** the broken or removed piece ceases counting toward the set threshold and all bonuses requiring that piece count deactivate immediately on the same simulation frame.

---

### Edge Cases

- **Indestructible Equipment**: Equipment templates with no finite durability attribute (maximum durability `0`) or items carrying the indestructible magic affix must never roll abrasion, never enter the Broken state, reject repair attempts with zero cost, and present infinite/indestructible durability to the client.
- **Vital Clamping When Gear Breaks**: When an armor piece, amulet, or set piece granting flat or percentage Maximum Life, Maximum Mana, or Maximum Stamina reaches `0` durability mid-combat, the player's current Life, Mana, and Stamina must immediately clamp down to the newly reduced maximums (never dropping below `1` Life from stat loss alone if the player survived the hit) and synchronize updated vitals to the client.
- **Duplicate Pieces / Dual Rings in Set Counting**: Equipping two identical rings sharing the same Golden catalog identity (in Classic Sets) or sharing the same extended piece index (in Extended Sets) must count as only **1** distinct piece toward set completion thresholds, preventing players from bypassing missing armor slots by wearing duplicate rings.
- **Simple (Cheap) Golden Repair Floor**: Repeatedly using Simple (Cheap) Repair on low-durability Golden gear reduces maximum durability by at least `1` point per repair, but maximum durability must be floored at `1` so the item never becomes permanently `0 / 0` unrepairable.
- **Peace Mode Skill Casts**: Casting utility or movement skills while in non-combat peace mode must not abrade the equipped weapon.
- **Zero Net Damage Hits**: Attacks completely absorbed or dealing `0` net health loss must not trigger defensive armor abrasion rolls.

---

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST evaluate weapon durability abrasion once per offensive skill cast when the caster is in active combat mode, deducting `1` durability point from an intact destructible weapon with probability `1 / 256`.
- **FR-002**: The system MUST evaluate defensive armor durability abrasion whenever a player suffers net health loss from incoming damage, rolling independently at `1 / 320` for equipped helm, body armor, belt, and cuffs (excluding boots, jewelry, horse, and mask) to deduct `1` durability point per successful roll.
- **FR-003**: The system MUST evaluate movement durability abrasion whenever a player crosses from one spatial region into a different spatial region, rolling independently at `1 / 2560` for equipped boots and `1 / 5120` for an equipped horse mount while never abrading equipped masks.
- **FR-004**: The system MUST treat any item lacking a positive base durability definition or carrying the indestructible magic property as permanently indestructible, exempting it from all abrasion rolls and presenting indestructible durability state to the client.
- **FR-005**: Whenever an item's current or maximum durability changes due to abrasion or repair, the system MUST immediately dispatch a real-time item durability synchronization message containing the item's runtime identifier, maximum durability, and updated current durability to the owning player.
- **FR-006**: When an equipped destructible item reaches `0` current durability, the system MUST keep the item in its current equipment slot, mark it **Broken**, immediately recalculate character attributes with 100% of that item's base stats, magic affixes, Five-Elements links, and set piece counts suspended, clamp current Life/Mana/Stamina to updated maximums, and synchronize updated character attributes to the client.
- **FR-007**: The system MUST reject any attempt to equip an item that is currently marked **Broken** (`0` current durability on a destructible item) until it is repaired to at least `1` current durability.
- **FR-008**: The system MUST compute standard/blue equipment repair prices using the authentic Version 6.0 formula scaling base gold price by missing durability ratio, global item price scale (`50%`), and total magic affix tier levels (`(16 + TotalMagicLevels) / 16`), returning `0` when current durability already equals maximum durability or the item is indestructible.
- **FR-009**: The system MUST compute Golden equipment repair prices and outcomes for two modes: **Simple (Cheap) Repair** costing `10 * (ItemLevel^2) * MissingDurability` gold while reducing maximum durability by `max(1, floor(MissingDurability / 10))` (clamped to a minimum maximum durability of `1`), and **Costly Repair** costing `30 * (ItemLevel^2) * MissingDurability` gold while preserving maximum durability unchanged.
- **FR-010**: Executing a valid equipment repair operation MUST atomically verify and deduct the required gold from the player's inventory currency, update current (and if applicable maximum) durability, clear the **Broken** state when current durability becomes greater than `0`, emit currency and durability synchronization messages, and immediately reactivate stats and set bonuses if the item is currently equipped.
- **FR-011**: The static game data catalog and compiled snapshot pipeline MUST load Golden item fixed magic affix bindings, item set membership bindings (both classic set identifiers and extended set piece bindings), and equipment set definitions (both classic sets and extended sets), automatically deriving piece thresholds for classic Golden sets `1..82` from catalog membership counts when not explicitly listed in the set table.
- **FR-012**: The item generator MUST deterministically generate named Golden equipment from its 1-based Golden catalog row identifier and random seed, populating base durability and up to six fixed magic attributes (`3` explicit slots and `3` hidden slots) rolled within each binding's authored parameter bounds using the Golden fixed affix pool.
- **FR-013**: Client item synchronization for Golden equipment MUST encode Golden quality alongside the zero-based Golden catalog index (one less than the 1-based Golden catalog row identifier), and character persistence MUST store and restore the Golden catalog identifier, current/maximum durability, and Broken flag without loss across zone transfers and logins.
- **FR-014**: Equipment stat evaluation MUST support **Classic Golden Set Bonuses**: when the number of distinct intact equipped Golden items sharing a classic set identifier is greater than or equal to that set's required piece count, all three hidden magic affix slots on every intact equipped piece of that set MUST activate unconditionally regardless of Five-Elements adjacency.
- **FR-015**: Equipment stat evaluation MUST support **Extended Set Bonuses**: for each active extended set identifier, counting distinct extended piece index values among intact equipped items MUST activate each piece's first extended hidden affix when meeting or exceeding the partial piece threshold, and activate both the first and second extended hidden affixes when meeting or exceeding the full piece threshold.

---

### Key Entities

- **Equipment Durability State**: Represents the wear lifecycle of an equipment instance (`Indestructible`, `Intact`, `Damaged`, or `Broken`), tracking current durability, current maximum durability, original template maximum durability, and whether stats are active or suspended.
- **Golden Equipment Definition**: Canonical template for a named legendary equipment piece, identified by its 1-based Golden catalog row identifier, base attributes, requirements, up to six fixed magic affix value-range bindings, and optional Classic or Extended set membership binding.
- **Fixed Magic Affix Binding**: Authoritative link between one of the six magic slots (`0..5`) of a Golden equipment definition and a Golden fixed-pool magic attribute, defining minimum/maximum roll bounds and whether the slot is explicit (`0, 2, 4`) or hidden (`1, 3, 5`).
- **Equipment Set Definition**: Catalog rule governing multi-piece set completion for either a **Classic Golden Set** (unlocking all 3 hidden slots on each piece at full piece count) or an **Extended Set** (unlocking 1 or 2 additional set bonus affixes per piece at partial and full distinct piece counts).
- **Equipment Repair Quote & Mode**: Encapsulates the repair mode (`Standard`, `GoldenSimple`, or `GoldenCostly`), gold cost, resulting current durability, and resulting maximum durability for a damaged or broken equipment piece.

---

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Over `100,000` deterministic simulated combat attacks, incoming damage hits, and region crossings, observed durability loss per equipment slot matches the Version 6.0 abrasion rates (`1/256` weapon attack, `1/320` helm/body/belt/cuff defense, `1/2560` boots move, `1/5120` horse move, `0` mask/indestructible) within statistical tolerance (`±2%`), and `100%` of durability changes emit an immediate client update on the same simulation tick.
- **SC-002**: `100%` of equipped items reaching `0` durability mid-combat with a completely full `6x10` inventory bag remain safely in their equipment slot marked Broken with `0` item loss, `0` cursor-lock states, and `100%` of their base stats, magic affixes, Five-Elements links, and set piece contributions suspended on the exact frame durability reaches `0`.
- **SC-003**: `100%` of repaired Broken items immediately clear their Broken flag, deduct the exact formula-calculated gold amount, restore durability so current durability equals maximum durability, and—when equipped—restore `100%` of suspended character stats and set bonuses within `1` simulation tick.
- **SC-004**: `100%` of generated Golden equipment instances across all catalog entries produce rolled affix values strictly within authored `[Minimum, Maximum]` parameter bounds, render the authentic Golden item identity on client synchronization (`0`-based catalog index), and round-trip across persistence with zero attribute or identity drift.
- **SC-005**: Equipping, breaking, repairing, or unequipping pieces of both Classic Golden Sets (both classic sets `1..82` and catalog sets `83+`) and Extended Sets produces `100%` exact character stat transitions across partial and full piece thresholds, with duplicate rings/piece indices counted at most once.
- **SC-006**: Per-hit defensive abrasion, per-cast offensive abrasion, and equipment stat recalculation execute with zero dynamic memory spikes on the real-time simulation loop, maintaining `18`-ticks-per-second frame budgets (`< 55.5 ms` per frame) under high-density combat.

---

## Assumptions

- Interactive merchant dialog trees and client shop repair requests belong to Phase 2 **Merchant & NPC Shops** (`017-module-npc-shops`). Feature `013-core-equipment-durability-and-sets` delivers the authoritative zone repair operation, standard and Golden repair pricing calculators, durability mutation invariants, currency deduction, outbound durability/currency/attribute synchronization, and full stat/set reactivation so `017-module-npc-shops` and blacksmith dialog scripts invoke a single verified repair operation.
- Crystal and Repair Oil item restoration of permanently lost Golden maximum durability (the gap between original template maximum durability and reduced current maximum durability) is a merchant/item-usage workflow layered on top of the core durability primitives delivered here and is out of scope for `013`.
- Purple socketed and Platina (`+6` and above) crafting and specialized repair workflows belong to their dedicated crafting expansion features, while `013` preserves their quality tiers and durability fields non-destructively.
