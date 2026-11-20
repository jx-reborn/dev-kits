# Feature Specification: Gameplay Scripting Host & Interactive Dialog System

**Feature Branch**: `012-core-csharp-scripting-host`  
**Created**: 2026-11-11  
**Status**: Converged  
**Input**: Authoritative server-side gameplay scripting host supporting interactive non-player character conversations, multi-page narrative monologues, scripted item activation, persistent and temporary quest progression variables, custom map trap triggers, timed zone mission controllers, and live zero-downtime script reload with per-player fault containment.

---

## Clarifications

### Session 2026-11-11

- **Q: Where is the boundary between core world rules and gameplay scripts?** -> **A**: The server follows an Engine-First thin trigger boundary. Spatial grids, movement physics, combat formulas, inventory space checks, currency accounting, and shop/storage transaction integrity remain exclusively inside core server subsystems. Gameplay scripts express narrative flow, quest prerequisites, branching choices, reward orchestration, and thin triggers that open core user interfaces (such as merchant shops or personal storage boxes) without re-implementing underlying economy or combat rules.
- **Q: How does the system support nested multi-step conversation menus when a player picks an option?** -> **A**: Whenever a player submits a valid dialog choice or finishes a multi-page monologue, the server MUST clear the player's pending dialog state *before* executing the selected option's follow-up logic. This ensures that if the follow-up logic opens a nested sub-menu or secondary narrative sequence, the newly registered dialog state is preserved rather than overwritten on exit.
- **Q: How do scripted usable items indicate whether the item stack should be consumed or retained?** -> **A**: Every scripted item handler returns an explicit outcome indicating either "consume one unit" or "keep item" rather than ambiguous numeric return codes. Reusable quest tools and failure paths (such as unmet level requirements or insufficient bag space) retain the item cleanly, whereas single-use scrolls and reward chests deduct exactly one unit only when activation succeeds.
- **Q: How are active player conversations handled during a live gameplay script reload, and how is memory growth prevented?** -> **A**: Live reload atomically swaps the active script catalog so newly initiated NPC interactions, item activations, and trap triggers immediately use the updated scripts. Any player already mid-conversation finishes their current pending step safely without disconnection, and once all pre-reload callbacks complete, the retired script generation is completely released from server memory with zero residual retention on long-lived player state.

---

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Interactive NPC Dialogs & Multi-Step Option Menus (Priority: P1)

A player approaches a friendly non-player character (such as a Town Teleporter or Village Elder) within conversational range and initiates a conversation. The server validates that the player is alive, not currently trading, and standing close enough to the target character, then presents a prompt with selectable menu choices. When the player selects an option—such as choosing a destination town—the server executes the corresponding action (for example, verifying travel fare, deducting currency, and relocating the player) or opens a nested sub-menu of further choices without losing conversational context.

**Why this priority**: Interactive NPC conversations and branching option menus are the primary gateway to world travel, faction enrollment, merchant access, storage access, and quest progression across the entire game world.

**Independent Test**: Spawn a player near a scripted Town Teleporter NPC, initiate conversation, verify the initial destination menu appears, select a multi-step regional sub-menu, choose a final destination, and confirm currency deduction and character relocation succeed end-to-end.

**Acceptance Scenarios**:

1. **Given** an alive, non-trading player standing within conversational range of a scripted NPC, **When** the player clicks the NPC to talk, **Then** the server presents the NPC's greeting prompt and ordered list of selectable choices, while keeping option action bindings strictly on the server.
2. **Given** a player viewing an active NPC option menu where Option 1 opens a secondary confirmation sub-menu, **When** the player selects Option 1, **Then** the server clears the first menu's pending state prior to running Option 1's logic and leaves the secondary confirmation sub-menu active and selectable.
3. **Given** a player standing beyond the NPC's allowed conversational radius, **When** the player attempts to initiate dialog with that NPC, **Then** the server rejects the request without opening a menu or disconnecting the player.
4. **Given** a player viewing an active NPC option menu, **When** the player closes/cancels the dialog or submits an out-of-range choice index, **Then** the server clears the player's pending dialog state cleanly and takes no unintended action without disconnecting the player.

---

### User Story 2 - Multi-Page Narrative Monologues & Completion Callbacks (Priority: P1)

A player triggers a story sequence with an NPC or quest event that spans multiple sequential pages of narrative text. The player advances page-by-page through the story window; if the script registered a post-monologue continuation, completing the final page automatically triggers the follow-up reward, world state change, or branching selection menu.

**Why this priority**: Mainline martial arts storylines, faction initiation ceremonies, and tutorial introductions rely on multi-page sequential monologues that smoothly transition into either quest updates or interactive choices upon completion.

**Independent Test**: Trigger a three-page NPC narrative monologue configured with a completion continuation, advance to the end of the monologue, and verify that the continuation executes exactly once and grants the expected quest state transition or follow-up menu.

**Acceptance Scenarios**:

1. **Given** a script initiating a multi-page monologue with no completion continuation, **When** the monologue is sent to the player, **Then** the player can read all pages and close the window without the server awaiting a follow-up response.
2. **Given** a script initiating a multi-page monologue with a completion continuation, **When** the player finishes the final page and notifies the server, **Then** the server clears the pending monologue state and executes the registered continuation exactly once.
3. **Given** a player who disconnects or leaves the map before finishing a pending monologue, **When** the session detaches from the map zone, **Then** any pending monologue callback is discarded cleanly without leaking session state.

---

### User Story 3 - Scripted Item Activation & Persistent Quest State Variables (Priority: P1)

A player right-clicks a scripted usable item in their inventory—such as a mysterious treasure scroll, a Beginner Gift Pack, or a reusable quest compass. The server runs the item's script, reads and updates the player's persistent quest variables (saved across logouts) or temporary session variables (cleared on logout), checks inventory capacity before awarding items, and either consumes one charge from the stack or retains the item based on the script's explicit outcome. If an item definition has no custom script assigned, the server falls back to triggering its configured skill effect if present.

**Why this priority**: Consumable reward packs, skill books, event tokens, quest items, and character progression tracking across conversations and item uses depend directly on scripted item activation and persistent/session task variables.

**Independent Test**: Grant a player a scripted gift pack item, attempt activation with a full bag (verifying the item is retained and a warning message is shown), free bag space, activate again (verifying one gift pack is consumed, rewards are granted, and a persistent quest flag is recorded and survives logout/login).

**Acceptance Scenarios**:

1. **Given** a player holding a single-use scripted gift item and sufficient free inventory space, **When** the player activates the item, **Then** the item script grants the configured rewards, updates the player's persistent quest variable, and consumes exactly one unit of the item.
2. **Given** a player holding a scripted item whose preconditions fail (such as insufficient character level or insufficient free bag space), **When** the player activates the item, **Then** the script sends an explanatory notice to the player and returns a keep-item outcome so the item count remains unchanged.
3. **Given** a player whose persistent quest variable is updated during an NPC dialog or item activation, **When** the player logs out and reconnects to the game world, **Then** the persistent quest variable retains its updated value while any temporary session-only quest variables start reset to zero.
4. **Given** a usable script-genre item that specifies no custom script identifier but configures a fallback skill effect, **When** the player activates the item (optionally targeting an entity), **Then** the server triggers that fallback skill effect and consumes one unit of the item.

---

### User Story 4 - Dynamic Map Trap Triggers & Zone Mission Lifecycle Hooks (Priority: P2)

When a player steps onto a map region cell bound to a custom scripted trap—such as a faction arena gate, a boss room barrier, or a battlefield readiness zone—the server invokes the assigned trap script, which can inspect player state, send notices, toggle combat posture, or relocate the player. In parallel, map zones hosting timed gameplay events (such as scheduled arenas or cooperative trials) run a mission lifecycle controller with deterministic initialization, periodic timer ticks, player join/leave hooks, and clean shutdown.

**Why this priority**: Enables custom entry rules for restricted areas (faction-only zones, boss lairs, tournament arenas) and provides the foundational lifecycle contract required by all scheduled battlegrounds and instanced challenges.

**Independent Test**: Step a player onto a custom-scripted map trap cell without the required quest flag (verifying bounce-back and notice), set the quest flag and step onto the cell again (verifying passage), and step a simulated map zone with an active mission controller through start, periodic timer intervals, and stop hooks.

**Acceptance Scenarios**:

1. **Given** a map trap cell bound to a custom trap script, **When** a player walks onto that cell from a non-matching trap cell, **Then** the server invokes the custom trap script once for that entry without re-firing repeatedly while the player remains on the same trap group.
2. **Given** an active mission controller started on a map zone, **When** the zone advances through simulation time, **Then** the mission's start hook runs on activation, its periodic timer hook fires at the configured interval without blocking normal zone responsiveness, and its stop hook cleans up mission participants upon completion.

---

### User Story 5 - Live Gameplay Script Hot-Reload & Per-Player Fault Containment (Priority: P2)

During live server operation or content authoring, an administrator or automated deployment updates compiled gameplay scripts without restarting the server or disconnecting online players. Invalid or broken script packages are rejected before activation so the currently running scripts remain intact. Furthermore, if any individual NPC dialog, item script, trap script, or mission callback encounters an unexpected runtime error, the server logs structured diagnostic context, safely resets only that affected interaction, and keeps the map zone and all other players running normally.

**Why this priority**: Rapid iteration on NPC dialogs, quests, and events without server downtime—combined with strict immunity against a buggy quest script crashing an entire map shard—is essential for live service stability and developer productivity.

**Independent Test**: Start a zone with Version 1 of an NPC dialog script, deploy Version 2 while players remain online, verify new NPC clicks immediately show Version 2 text while retired Version 1 script memory becomes reclaimable, and trigger a deliberately faulting script for Player A while verifying Player B on the same map continues moving and talking uninterrupted.

**Acceptance Scenarios**:

1. **Given** a running server with online players and Version 1 gameplay scripts active, **When** a valid Version 2 script bundle is deployed for live reload, **Then** all subsequent NPC dialogs, item activations, and trap triggers execute Version 2 with zero player disconnections, and Version 1 resources are fully released once any in-flight Version 1 callbacks finish.
2. **Given** a running server with valid Version 1 scripts active, **When** a corrupted or incompatible script bundle is submitted for reload, **Then** the server logs a structured reload failure alert and continues serving Version 1 uninterrupted.
3. **Given** two players active in the same map zone, **When** Player A triggers a defective script that throws an unexpected runtime error, **Then** the server logs the fault, clears Player A's broken dialog state, retains Player A's unconsumed item if triggered via item use, and processes Player B's actions on the exact same tick without interruption.

---

### Edge Cases

- **Duplicate or Rapid-Fire Dialog Clicks**: If a player rapidly clicks two different NPCs or double-clicks the same NPC while an earlier dialog is already open, the newer valid conversation replaces the prior pending dialog state cleanly so stale option indices from the first menu cannot execute against the second menu.
- **Player Moves Away, Starts Trading, or Dies Mid-Dialog**: If a player walks far away from the originating NPC, enters a trade session, or dies before submitting a menu choice, the server rejects or cancels the stale selection safely without executing privileged actions.
- **Oversized Dialog Prompts or Excessive Options**: If a script authors more selectable options than the client interface supports (maximum 50 options) or text exceeding a single wire message's payload budget, the server enforces safe option limits and switches to extended description delivery or truncates cleanly rather than corrupting client memory or dropping the connection.
- **Cross-Map Teleport Inside Dialog or Item Callback**: When an option callback or scripted item teleports the player to another map zone, the player's local dialog state on the departing zone is detached cleanly through the standard zone leave lifecycle with zero dangling references.
- **Task Variable Boundary Values**: Setting a persistent or temporary quest variable to zero removes redundant default entries from storage snapshots while preserving exact signed integer values across the full supported range (`0` through `65535` variable identifiers).

---

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST provide a central gameplay scripting host registered by unique human-readable script identifiers covering four script categories: NPC conversations, usable item activations, custom map cell traps, and zone mission controllers.
- **FR-002**: The system MUST validate incoming NPC conversation requests against active session lease ownership, non-dead character state, non-trading state, target NPC presence on the same map, and conversational proximity before invoking the NPC's bound conversation script.
- **FR-003**: The system MUST provide a fluent conversation authoring interface enabling scripts to send interactive option menus (up to 50 choices), extended rich-header option descriptions, multi-page sequential monologues, system notices, and thin triggers to open core UI panels (such as merchant shops and personal storage boxes).
- **FR-004**: The system MUST keep all option action callbacks and monologue completion continuations strictly on the server, transmitting only display text and option counts to the client and binding client responses by zero-based option index.
- **FR-005**: When processing a player's dialog selection or monologue completion, the system MUST validate that the player currently has a matching pending dialog state, clear that pending state *prior* to invoking the registered callback so nested dialogs remain intact, and treat negative or out-of-bounds indices as safe dialog cancellations without disconnecting the client.
- **FR-006**: The system MUST clear any pending dialog state and temporary session state deterministically whenever a player logs out, loses connection, times out, transfers maps, or has their session lease superseded.
- **FR-007**: The system MUST route usable script-genre inventory items (including optional target entity identifiers supplied by the client) to their configured item usage script, consuming one stack unit if and only if the script returns an explicit consume-one outcome and retaining the item when the script returns a keep-item outcome or faults.
- **FR-008**: When a usable script-genre item template specifies no custom script identifier (or its fallback behavior is configured) and defines a castable skill identifier, the system MUST trigger that skill effect and consume one unit upon valid activation.
- **FR-009**: The system MUST expose both persistent character quest variables (keyed `0` through `65535` with signed 32-bit integer values, persisted across sessions) and temporary session quest variables (keyed `0` through `65535` with signed 32-bit integer values, automatically discarded when the session ends) to all script contexts.
- **FR-010**: The system MUST expose safe Engine-First player helper operations to script contexts—including reading character attributes (level, faction, element series, gender, map coordinates, currency, free bag space), adjusting currency and experience within validated bounds, granting or removing items by template identifier, casting skills, and teleporting within or across map zones—without allowing scripts to bypass core inventory collision or session lease invariants.
- **FR-011**: The system MUST invoke bound custom trap scripts when a player transitions onto a map cell configured with a custom script trap action, preventing duplicate re-triggers while the player remains continuously on cells sharing the same trap identifier.
- **FR-012**: The system MUST support per-zone active mission lifecycles with deterministic initialization, periodic frame-counted timer ticks on the map simulation loop, player join/leave notifications, and clean teardown without blocking normal map tick execution.
- **FR-013**: The system MUST support live hot-reloading of compiled gameplay script bundles from memory without locking script files on disk, atomically swapping the active script registry for new interactions while allowing retired script generations to be completely reclaimed once in-flight callbacks finish.
- **FR-014**: The system MUST contain any unhandled runtime error thrown by an NPC conversation, item activation, custom trap, or mission callback to that single execution—logging structured error details, resetting any corrupted pending dialog state for the player, preserving unconsumed items on item-script failure, and continuing zone simulation uninterrupted.
- **FR-015**: The system MUST include at least one complete, production-ready reference NPC conversation script (a multi-destination Town Teleporter supporting novice protection blocking travel below Level `5` via an explanatory monologue, destination selection at Level `5` and above, nested confirmation or regional sub-menus, fare validation, currency deduction, and cross-town relocation) verifying the end-to-end scripting workflow.
- **FR-016**: For deterministic live parity and development testing, the system MAY include an opt-in, configuration-gated test utility item script capable of seeding character level, currency, and test fixture items over standard usable-item dialog choices, provided both automatic token grant and script execution remain strictly disabled (`false`) by default in production.

### Key Entities

- **Script Registry Snapshot**: Immutable point-in-time catalog mapping human-readable script codes to active NPC conversation, item usage, custom map trap, and mission controller definitions; swapped atomically during live reloads.
- **Pending Player Dialog State**: Per-player session state recording whether the player is currently awaiting an option menu selection or a monologue completion, the originating NPC (if any), and the server-side callbacks bound to each valid choice index.
- **Persistent Quest Variable**: Durable key-value progression state (`0..65535` -> signed whole number) saved with the character across logins to track quest stages, faction trials, and one-time rewards.
- **Temporary Session Variable**: Volatile key-value state (`0..65535` -> signed whole number) scoped strictly to the player's current online session for transient multi-step interactions and reset upon disconnect.
- **Item Script Usage Binding**: Static item catalog metadata associating a usable item with its target script identifier, optional fallback castable skill, minimum level, and consumption policy.
- **Active Zone Mission Instance**: Runtime state of an ongoing timed event or arena within a specific map zone, tracking mission phase, participant roster, and next scheduled timer tick.

---

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Players within valid conversational distance receive NPC dialog prompts and selectable choices within 1 simulation tick (under 60 milliseconds) of clicking an NPC, and 100% of multi-step nested sub-menus (`depth >= 3`) preserve active option callbacks without state overwrite.
- **SC-002**: 100% of out-of-range NPC clicks, negative cancel selections, out-of-bounds option indices, and stale dialog responses after movement/death/disconnect are handled safely with zero client disconnections and zero unauthorized reward grants.
- **SC-003**: 100% of scripted item activations accurately honor explicit consume-one versus keep-item outcomes, with zero item loss when preconditions fail (such as full inventory) or when an unexpected script error occurs.
- **SC-004**: Persistent quest variables modified during NPC conversations or item activations survive character logout and re-entry with 100% fidelity, while 100% of temporary session variables reset upon session termination.
- **SC-005**: Live reloading an updated gameplay script bundle completes with zero dropped player sessions, and 100% of retired script generations release all retained memory immediately once all pre-reload callbacks conclude.
- **SC-006**: When a deliberately faulting script is triggered repeatedly under concurrent zone load, 0 zone simulation loops halt and 100% of sibling players on the same map continue moving, fighting, and interacting normally.

---

## Assumptions

- Static catalog definitions for NPCs, items, and map portal/trap bindings already support human-readable string identifiers authored under the project's canonical data pipeline.
- Core merchant shop buying/selling and personal storage repository operations are delivered in their dedicated economy feature specifications; in this feature, the scripting context provides the thin user-interface trigger hooks and state slots so NPC scripts (such as Shopkeepers and Storage Keepers) can open them cleanly.
- Client-supplied text and numeric input prompts and item submission windows build atop the same pending callback slot pattern introduced here and can be layered cleanly in downstream quest and crafting modules without altering the core scripting host architecture.
