# Feature Specification: Factions, Skill Trees & Point Washing

**Feature Branch**: `015-core-factions-and-skills`  
**Created**: 2026-11-11  
**Status**: Ready for Implementation  
**Input**: Classic ten-sect martial arts progression, Five-Elements and gender induction gates, sect graduation and camp alignment transitions, level-gated skill point allocation, attribute and skill point washing, and advanced sect skill books and bonus point scriptures.

## Clarifications

### Session 2026-11-11

- Q: Which martial arts sects are active for player induction and progression in this release? → A: Scope is fixed to the **classic 10 martial arts sects (`0` through `9`, two sects per Five-Elements series)** while leaving any extra catalog entries (such as sect `10`) inactive for player induction.
- Q: How many bonus attribute points should reading a Marrow-Washing Scripture (*Tẩy Tủy Kinh*) grant? → A: Each Marrow-Washing Scripture grants a **flat `+5` unallocated attribute points starting at Level 80** up to 15 books (`+75` maximum bonus attribute points), eliminating the legacy level `80`–`87` permanent stat-point loss trap.
- Q: What happens to a disciple's sect skills and advanced book eligibility after graduating from their sect? → A: **Graduated disciples retain all learned sect skills** and remain eligible to learn their original sect's advanced Level 90 and Level 120 skill books based on their preserved historical sect identity.
- Q: Which skills are reset and refunded when a player washes their skill points? → A: **Skill reset refunds only point-allocated skills back to level `0`** while preserving basic melee/weapon starter skills, utility skills, and combat-proficiency skills at their current levels and accumulated training progress.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Five-Elements & Gender-Gated Sect Induction & Base Skill Tree Grant (Priority: P1)

A novice character who has reached at least Level 10 speaks with a sect master to join one of the ten classic martial arts sects. The server verifies that the character is currently unaffiliated, meets the minimum level requirement, matches the sect's Five-Elements affinity, and satisfies any sect-specific gender rule (male-only for Shaolin / *Thiếu Lâm*, female-only for Emei / *Nga My* and Cuiyan / *Thúy Yên*). Upon induction, the character's current sect, original sect (if joining for the first time), and sect join counter are updated, their camp alignment changes from Novice to the sect's alignment (Justice, Evil, or Balance), all base sect skills (`10x` through `60x` tiers plus weapon passives) are unlocked in their skill book at level `0`, and both the player and nearby observers see the updated sect and camp state immediately and across future logins.

**Why this priority**: Joining a martial arts sect is the foundational progression milestone that unlocks a character's identity, camp alignment, and entire martial arts skill tree. Without sect induction and persistent level-`0` skill grants, characters remain trapped with basic novice attacks.

**Independent Test**: Create Level 10 characters across all five elemental affinities and both genders, attempt induction into matching and non-matching sects, verify valid inductions update sect identity, camp alignment, and base skill trees at level `0`, verify invalid elemental/gender/level attempts are rejected without state changes, and confirm all sect and skill state survives logout and re-entry.

**Acceptance Scenarios**:

1. **Given** an unaffiliated Level 10 male Metal-series novice in Novice camp, **When** he requests induction into Shaolin (*Thiếu Lâm*, Metal, Male-only, Justice camp), **Then** his current and original sect become Shaolin, his sect join count becomes `1`, his base and current camp become Justice, all 13 Shaolin base sect skills are added to his skill list at level `0`, and nearby players observe his camp change to Justice.
2. **Given** an unaffiliated Level 10 female Metal-series novice, **When** she attempts to join Shaolin (*Thiếu Lâm*) or Emei (*Nga My*, Water), **Then** both induction attempts are rejected with clear explanations (gender mismatch for Shaolin; elemental mismatch for Emei) and her Novice camp and unaffiliated status remain unchanged.
3. **Given** an inducted disciple with level-`0` sect skills who logs out and re-enters the world, **When** world synchronization completes, **Then** the player's sect membership, camp alignment, join count, and complete list of level-`0` sect skills are restored identically.

---

### User Story 2 - Sect Graduation, Camp Transitions & Persistent Faction Identity (Priority: P1)

A sect disciple who has reached at least Level 60 speaks with their sect master to graduate (*Xuất Sư*) and venture independently into the martial world. Upon graduation, the character's current sect becomes unaffiliated while their original sect identity and join history are permanently preserved, their base and current camp transition to Free (*Tự Do*) alignment, and all previously learned sect skills remain intact and usable. Nearby players immediately observe the camp transition, and a graduated character may later re-induct into an eligible sect of the same elemental series if desired.

**Why this priority**: Sect graduation enables mid-to-endgame PvP, guild warfare, and cross-camp social play while preserving the character's martial arts heritage and skill tree.

**Independent Test**: Advance an inducted disciple to Level 59 and verify graduation is refused, then advance to Level 60, complete graduation, verify current sect clears to unaffiliated while original sect and join count persist, confirm camp becomes Free for both the player and nearby observers, and verify learned sect skills remain usable across logout and login.

**Acceptance Scenarios**:

1. **Given** a Level 60 Tangmen (*Đường Môn*, Wood, Balance camp) disciple with allocated sect skills, **When** the disciple graduates through the sect master, **Then** their current sect becomes unaffiliated, their original sect remains Tangmen, their base and current camp transition to Free, and all learned Tangmen skills remain at their existing levels.
2. **Given** a Level 59 disciple of any sect, **When** the disciple requests graduation, **Then** the request is denied and the disciple remains an active member of their sect and sect camp.
3. **Given** a graduated Metal-series martial artist in Free camp (originally Shaolin, join count `1`), **When** he requests induction into Tianwang (*Thiên Vương*, Metal, Balance camp), **Then** his current sect becomes Tianwang, his original sect remains Shaolin, his join count increments to `2`, and his base and current camp update to Balance.

---

### User Story 3 - Level-Gated Skill Point Allocation & Proficiency Skill Protection (Priority: P1)

As a disciple levels up and earns unallocated skill points, they invest those points directly into unlocked point-allocated sect skills—including raising newly granted sect skills from level `0` to level `1` once they meet the skill's minimum character level requirement. Each skill's maximum point-allocated level is strictly bounded by both the skill's own cap and `(character level - skill required level + 1)`. Meanwhile, basic melee starter skills, non-allocatable utility skills, and combat-proficiency skills (such as Level 90 and Level 120 mastery skills that advance only through combat training) reject manual skill point investment.

**Why this priority**: Point allocation from `0` to `1` and up to `20` is the primary mechanism by which players customize their build and unlock active combat abilities; enforcing level gates and proficiency-skill locks prevents progression exploits.

**Independent Test**: Grant a disciple unallocated skill points at Levels 10, 15, and 30, allocate points into tier-10 (`0 -> 1 -> 6`), tier-20, and tier-30 sect skills, verify attempts to exceed `(character level - skill required level + 1)` or invest points into starter melee or proficiency skills are rejected without consuming points, and confirm skill levels and remaining points synchronize accurately.

**Acceptance Scenarios**:

1. **Given** a Level 10 disciple with `5` unallocated skill points and a granted Level-10 sect skill at base level `0` (maximum level `20`), **When** the player allocates `1` skill point to that skill, **Then** the skill rises from level `0` to level `1` and unallocated skill points decrease to `4`.
2. **Given** the same Level 10 disciple with the Level-10 sect skill now at level `1`, **When** the player attempts to allocate a second point into that skill while still at Level 10, **Then** the allocation is rejected because `10 - 10 + 1 = 1`, leaving the skill at level `1` and unallocated skill points at `4`.
3. **Given** a Level 85 character with unallocated skill points and a Level 90 combat-proficiency sect skill at level `1` (`45%` training progress), **When** the player attempts to spend a skill point on that proficiency skill or on a starter melee skill, **Then** the attempt is rejected and both unallocated skill points and proficiency progress remain unchanged.

---

### User Story 4 - Attribute & Skill Point Reset with Bonus Point Conservation (Priority: P1)

A character of at least Level 50 visits the Marrow-Washing Monk (*Tẩy Tủy Đại Sư*) to reset either their allocated base attributes (Strength, Dexterity, Vitality, Energy), their allocated skill points, or both so they can respec their build. Attribute washing resets all four base attributes to the exact Five-Elements starter baseline (plus any permanent quest-granted single-stat bonuses), refunds every invested point into unallocated attribute points—including all bonus points earned from Marrow-Washing Scriptures—and recalculates derived maximum Life, Mana, Stamina, and equipment activation while clamping current vitals safely. Skill washing resets only point-allocated skills back to level `0`, refunds the exact sum of invested skill points—preserving all bonus points earned from Martial Arts Manuals—deactivates any active aura whose level drops to `0`, and leaves starter melee skills, protected utility skills, and combat-proficiency skills completely untouched at their current levels and training progress.

**Why this priority**: Build respec (*Tẩy Tủy*) is essential to classic martial arts progression because early-level leveling builds differ fundamentally from endgame builds, and losing permanent scripture bonus points or proficiency progress during a reset would permanently brick high-level characters.

**Independent Test**: Take a Level 85 character who has consumed bonus attribute and skill scriptures, invested points across all four attributes and multiple sect skills, trained a Level 90 proficiency skill to level `5` (`60%` progress), and activated a sect aura; perform attribute and skill washes and verify 100% mathematical conservation of total attribute and skill points, preservation of proficiency/starter/utility skills, aura deactivation, and safe vital clamping.

**Acceptance Scenarios**:

1. **Given** a Level 80 Metal-series character (starter base `35/25/25/15`) who gained `395` level-up attribute points and `+15` bonus points from `3` Marrow-Washing Scriptures (`410` total distributable points) and invested `300` points across Strength and Vitality (`110` unallocated), **When** the character washes attribute points, **Then** base attributes return to `35/25/25/15`, unallocated attribute points become `410`, maximum Life/Mana/Stamina recalculate to base values, and current Life/Mana/Stamina clamp down so they never exceed the new maximums.
2. **Given** a Level 85 disciple who gained `75` level-up skill points and `+5` bonus points from `5` Martial Arts Manuals (`80` total distributable skill points), invested `60` points across sect skills (including `10` points in an active aura skill), holds `20` unallocated skill points, knows basic melee skills at level `1`, Lightness skill (*Khinh Công*) at level `1`, and a Level 90 combat-proficiency skill at level `4` (`72%` training progress), **When** the character washes skill points, **Then** all point-allocated sect skills reset to level `0`, the active aura is cleared, unallocated skill points become `80`, and basic melee skills, Lightness skill, and the Level 90 proficiency skill (`level 4, 72%`) remain completely unchanged.
3. **Given** a Level 49 character, **When** the character asks the Marrow-Washing Monk to reset attributes or skills, **Then** the request is refused because the character has not reached Level 50.

---

### User Story 5 - Advanced Sect Skill Books & Permanent Stat/Skill Scriptures (Priority: P2)

High-level characters consume rare martial arts books from their inventory to unlock endgame sect abilities and permanent bonus points:
- **Level 90 Sect Skill Books** (28 distinct manuals across the 10 sects, usable from Level 80+): Verify the reader's effective sect (current sect, or original sect if graduated) matches the manual's sect and the skill is not yet known; consume one manual and grant the skill at level `1` for combat-proficiency manuals (23 books) or level `0` for point-allocated manuals (5 support/curse books).
- **Level 120 Sect Skill Book** (usable at Level 120+ once per character): Verify the reader belongs to or graduated from one of the 10 sects, has reached Level 120, and has not previously learned a Level 120 skill; consume one manual, grant the corresponding sect's Level 120 combat-proficiency skill at level `1`, and record completion permanently.
- **Martial Arts Manual (*Võ Lâm Mật Tịch*) & Marrow-Washing Scripture (*Tẩy Tủy Kinh*)** (usable from Level 80+, up to 15 uses each): Consume one volume per valid use, permanently granting `+1` unallocated skill point (*Võ Lâm Mật Tịch*) or a flat `+5` unallocated attribute points (*Tẩy Tủy Kinh*), up to `+15` bonus skill points and `+75` bonus attribute points per character.

**Why this priority**: Advanced skill books and scriptures drive the entire Level 80–120 item hunt, boss economy, and endgame character differentiation while relying on the sect identity and skill infrastructure established in P1.

**Independent Test**: Attempt to use each category of book below required level, with a wrong sect, as a duplicate, and past the 15-book cap to verify item preservation on rejection; then consume valid books as both active disciples and graduated Free-camp martial artists and verify exact skill grants, point awards, item consumption, and persistence across relog.

**Acceptance Scenarios**:

1. **Given** a Level 80 graduated martial artist whose original sect is Tangmen (*Đường Môn*), **When** they use both a Tangmen combat-proficiency Level 90 manual (*Bạo Vũ Lê Hoa*) and Tangmen's point-allocated Level 90 manual (*Loạn Hoàn Kích*), **Then** each manual is consumed from inventory, the proficiency skill is granted at level `1`, and the point-allocated skill is granted at level `0` ready for skill point investment.
2. **Given** a Level 120 Shaolin (*Thiếu Lâm*) disciple who has never learned a Level 120 skill, **When** they use a Level 120 Skill Book, **Then** the book is consumed, Shaolin's Level 120 proficiency skill (*Đại Thừa Như Lai Chú*) is granted at level `1`, and any subsequent attempt to use another Level 120 Skill Book is rejected without consuming the item.
3. **Given** a Level 80 character who has used `14` Marrow-Washing Scriptures and `15` Martial Arts Manuals, **When** they use one Marrow-Washing Scripture and one Martial Arts Manual, **Then** the 15th Marrow-Washing Scripture is consumed and grants `+5` unallocated attribute points (reaching `+75` total), while the 16th Martial Arts Manual is rejected with a cap message and remains in their inventory.

---

### Edge Cases

- **Cross-series or wrong-gender sect induction**: Attempting to join a sect outside the character's Five-Elements affinity, a sect whose gender rule excludes the character (female attempting Shaolin; male attempting Emei or Cuiyan), an inactive sect outside `0`–`9`, or attempting to join while already an active member of a sect MUST reject the request cleanly with zero changes to sect, camp, join count, or skills.
- **First skill point allocation (`0` → `1`) on granted skills**: Sect induction and the 5 point-allocated Level 90 books grant skills at base level `0`; allocating the first skill point MUST smoothly raise the skill from `0` to `1` whenever the character meets the skill's required level, whereas ungranted skills (not in the character's skill list) MUST reject point allocation even if they belong to the same sect.
- **Active aura skill reset to level `0`**: If a character's currently active right-hand or aura skill is a point-allocated sect skill that gets reset to level `0` during a skill wash, the active aura MUST be cleared immediately so the character cannot retain passive aura benefits from a level-`0` skill.
- **Vital clamping and equipment requirement re-evaluation on attribute wash**: Resetting Vitality or Energy lowers maximum Life or Mana, and resetting Strength or Dexterity can deactivate worn equipment that previously added bonus Life/Mana or skill levels; current Life, Mana, and Stamina MUST clamp to `[1, maximum Life]`, `[0, maximum Mana]`, and `[0, maximum Stamina]` without killing the player or leaving overflowed vitals.
- **Duplicate, wrong-sect, or unaffiliated novice book usage**: Using a Level 90 or Level 120 skill book as a pure novice who has never joined any sect, using another sect's Level 90 book, or re-reading a book for a skill already known MUST fail safely and leave the book item intact in inventory.
- **Exceeding the 15-volume scripture cap or preserving shared task progress**: Reading a 16th *Võ Lâm Mật Tịch* or 16th *Tẩy Tủy Kinh* MUST reject consumption without deducting the item, and updating the per-character usage counter (stored in the low 4 bits of the character's quest counter) MUST preserve any unrelated upper bits in that quest variable.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST define and enforce the **classic 10 martial arts sects (`0` through `9`)**, paired strictly two per Five-Elements series (`0` Metal: Shaolin, Tianwang; `1` Wood: Tangmen, Wudu; `2` Water: Emei, Cuiyan; `3` Fire: Gaibang, Tianren; `4` Earth: Wudang, Kunlun), including each sect's alignment camp, gender eligibility, rank titles, and complete base sect skill catalog, while keeping any extra sect entries inactive.
- **FR-002**: System MUST enforce sect induction gates requiring the character to be at least Level 10, currently unaffiliated with any active sect, matched to the target sect's Five-Elements series, and matched to the target sect's gender restrictions (`0` Shaolin: male only; `4` Emei and `5` Cuiyan: female only; all other 7 classic sects: both genders).
- **FR-003**: Upon valid sect induction, system MUST atomically set the character's current sect, record their original sect if joining for the first time (preserving original sect on any subsequent re-join), increment their total sect join count, set both base camp and current camp to the sect's alignment (Justice, Evil, or Balance), and grant all base sect skills not yet in the character's skill book at base level `0`.
- **FR-004**: System MUST enforce sect graduation (*Xuất Sư*) gates requiring the character to have an active sect membership and be at least Level 60; upon graduation, system MUST clear active sect membership to unaffiliated, preserve original sect identity and join count, transition both base camp and current camp to Free alignment, and retain all learned skills and their levels/training progress.
- **FR-005**: System MUST derive a character's camp alignment deterministically on world entry and state transitions: active sect disciples receive their sect's camp (Justice, Evil, or Balance); unaffiliated characters who have previously joined a sect (`join count > 0`) receive Free camp; pure novices who have never joined a sect (`join count == 0`) receive Novice camp.
- **FR-006**: System MUST synchronize full faction membership state (base camp, current sect, original sect, and join count) to the owning player upon world entry, upon explicit client faction-state refresh request, and immediately after sect induction or graduation, while broadcasting base camp and current camp updates to nearby players in visual range.
- **FR-007**: System MUST retain learned skills at base level `0` alongside each skill's accumulated combat-proficiency experience across active gameplay, zone transfers, periodic snapshots, and logout/login persistence cycles without stripping level-`0` skill entries.
- **FR-008**: System MUST persist current sect, original sect, sect join count, unallocated attribute and skill points, per-character scripture/book counters, and every learned skill's base level and raw training experience across character saves and session re-attachments.
- **FR-009**: System MUST allow allocating unallocated skill points into any skill already present in the character's learned skill list—including advancing base level `0` to `1`—if and only if the skill is point-allocatable (not a combat-proficiency skill and not a fixed starter/utility skill), the character has at least `1` unallocated skill point, the character's level is at least the skill's required level, and the resulting base level does not exceed `min(skill maximum level, character level - skill required level + 1)`.
- **FR-010**: System MUST synchronize updated skill level, remaining unallocated skill points, and skill training percentage (`0`–`100%`) accurately to the owning client whenever a skill is granted, allocated, trained, or reset.
- **FR-011**: System MUST support attribute point washing (*Tẩy Tủy Tiềm Năng*) for characters at Level 50 or above, resetting Strength, Dexterity, Vitality, and Energy to the character's Five-Elements starter baseline (plus any permanent single-stat quest bonuses), refunding the exact total points removed into unallocated attribute points, re-evaluating equipment activation and derived maximum Life/Mana/Stamina, clamping current vitals to the new maximums, and synchronizing all four attributes and vitals to the client.
- **FR-012**: System MUST support skill point washing (*Tẩy Tủy Kỹ Năng*) for characters at Level 50 or above, resetting every point-allocatable learned skill with base level `> 0` back to base level `0`, refunding the exact sum of reset base levels into unallocated skill points, deactivating any active aura skill whose level becomes `0`, preserving starter melee/weapon skills, protected utility skills (*Khinh Công*, *Kiếp Phú Tế Bần*), and all combat-proficiency skills at their existing base levels and training experience, and synchronizing the full skill book to the client.
- **FR-013**: Attribute and skill washing MUST conserve 100% of all bonus attribute points earned from Marrow-Washing Scriptures and all bonus skill points earned from Martial Arts Manuals across unlimited repeated washes.
- **FR-014**: System MUST define the character's **effective sect** as their active sect when affiliated, or their preserved original sect when graduated, and MUST use this effective sect to validate sect-specific Level 90 and Level 120 skill books.
- **FR-015**: System MUST support all **28 classic Level 90 sect skill books** for characters at Level 80 or above whose effective sect matches the book's required sect and who have not yet learned the target skill, consuming `1` book item and granting the skill at base level `1` for the 23 combat-proficiency skills or base level `0` for the 5 point-allocated support/curse skills.
- **FR-016**: System MUST support the **Level 120 sect skill book** for characters at Level 120 or above with a valid effective sect (`0`–`9`) who have not previously completed Level 120 skill learning, consuming `1` book item, granting the corresponding sect's Level 120 combat-proficiency skill at base level `1`, and permanently marking the character's Level 120 book tracker to prevent duplicate consumption.
- **FR-017**: System MUST support **Martial Arts Manual (*Võ Lâm Mật Tịch*)** consumption for characters at Level 80 or above with fewer than `15` recorded uses, consuming `1` manual, adding `+1` unallocated skill point, and incrementing the 4-bit usage counter without altering unrelated quest bits.
- **FR-018**: System MUST support **Marrow-Washing Scripture (*Tẩy Tủy Kinh*)** consumption for characters at Level 80 or above with fewer than `15` recorded uses, consuming `1` scripture, adding a **flat `+5` unallocated attribute points**, and incrementing the 4-bit usage counter without altering unrelated quest bits.

### Key Entities

- **Martial Arts Sect Catalog Entry**: Static definition for each of the 10 classic sects (`0`–`9`), specifying its Five-Elements series, alignment camp (Justice, Evil, or Balance), allowed character genders, rank title progression, and ordered list of base sect skills granted upon induction.
- **Character Faction Identity**: Persistent and live character state tracking current sect affiliation (or unaffiliated), original first-joined sect, cumulative sect join count, base camp, and current camp.
- **Learned Skill Slot**: Entry in a character's skill book pairing a skill identifier with its current base level (`0` or higher, distinct from temporary equipment bonuses) and accumulated raw combat-proficiency experience.
- **Scripture & Mastery Progress Trackers**: Persistent per-character counters recording how many Martial Arts Manuals (`0`–`15`) and Marrow-Washing Scriptures (`0`–`15`) the character has consumed, any permanent single-attribute quest bonuses, and whether the character has already learned their Level 120 sect skill.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% of the 100 combinations of Five-Elements affinities (`5` series), character genders (`2` genders), and classic martial arts sects (`10` sects) enforce exact induction acceptance and rejection rules, granting 100% of each sect's canonical base skill list at level `0` on valid induction.
- **SC-002**: 100% of inducted disciples, graduated Free-camp martial artists, level-`0` granted skills, allocated skill levels, and combat-proficiency experience values survive zone detach, persistence save, and subsequent world re-entry with zero lost skills or reverted camp alignments.
- **SC-003**: 100% of skill point allocation attempts strictly uphold `0 → 1` unlocking, level-gated caps (`min(skill maximum level, character level - skill required level + 1)`), and proficiency/starter skill locks with zero point loss or client/server skill-point desynchronization.
- **SC-004**: Across 100 consecutive cycles of allocating points and executing attribute and skill washes on characters holding `1`–`15` consumed Martial Arts Manuals and Marrow-Washing Scriptures, total distributable attribute points (`starter baseline + level-up attribute points + 5 * scriptures + quest attribute bonuses`) and total distributable skill points (`level-up skill points + manuals`) remain 100% invariant (`0` points lost or duplicated), while combat-proficiency skills retain 100% of their level and training progress.
- **SC-005**: All 28 Level 90 sect skill books, all 10 sect mappings of the Level 120 skill book, *Võ Lâm Mật Tịch*, and *Tẩy Tủy Kinh* enforce 100% accurate level, effective-sect, duplicate-skill, and 15-use cap checks—never consuming an inventory item when a requirement fails.
- **SC-006**: Nearby players within visual range observe camp alignment transitions (Novice → Sect Camp on induction; Sect Camp → Free Camp on graduation) within a single world simulation tick (`<= 56 ms`).

## Assumptions

- Interactive NPC conversations (sect master induction/graduation dialogs and Marrow-Washing Monk respec dialogs) and inventory book consumption hooks build upon the gameplay scripting infrastructure defined in Feature `012`.
- Separation between a character's persistent base camp and runtime current camp, along with nearby runtime camp change notifications, builds upon the camp and combat state foundations established in Feature `014`.
- Combat-proficiency experience gain per skill cast during live combat continues to use existing combat-proficiency skill classifications and training curves; this feature ensures those skills are granted at the proper initial level, synchronized with accurate progress percentages, persisted with raw training experience intact, blocked from manual point allocation, and preserved untouched during skill point washing.
- In accordance with the resolved session clarification, NPC dialog prompts for sect induction, graduation, and point washing enforce level, sect, and state prerequisites cleanly without charging silver fees by default, while allowing server scripts to layer optional campaign conditions in future content packs without changing core faction or wash mechanics.
