# Ground-Truth Research: Core C# 14 Scripting Host & Dialog System (`012`)

**Feature Branch**: `012-core-csharp-scripting-host`  
**Date**: 2026-11-11  
**Spec**: [spec.md](spec.md)  
**Compliance**: JXReborn Constitution v1.4.0 (Principle III — Zero-Hallucination Evidence & Ground-Truth Fidelity; Principle VI — C# 14 Fluent Scripting API)

---

## 1. Ground-Truth Evidence Ledger

All wire layouts, framing lengths, anti-cheat checksum keys, spatial distance gates, and state transitions in `012-core-csharp-scripting-host` are aligned across legacy 6.0 protocol contracts, companion test swarms, and the current codebase:

| Subsystem | Early 2003 Wire Notes | Legacy 6.0 Wire Contract | `jx-swarm` (Verified Bot Suite) | `jx-server` (Current Codebase) |
| :--- | :--- | :--- | :--- | :--- |
| **`c2s_dialognpc` (`0x6E`)** | Fixed 9B header | Validates check key `0x506C` and Euclidean radius prior to dialog trigger | `C2S_DialogNpcPacket.cs` (9B), `npc-dialog-inventory-and-economy.md` §1.1 | `ProtocolOpcodes.cs:58`, `ProtocolSizeTable.cs:63` (`9`), `MovementAntiCheatConsts.cs:13` (`0x506C`) |
| **`s2c_scriptaction` (`0x63`)** | 300B `\|`-delimited buffer (`MaxAnswers = 50`) | Expanded `900`B payload buffer with null-terminated TCVN3 options (`Say`/`Describe`) | `TagPlayerScriptActionSync.cs:24-120`, `ScriptActionPacketHandler.cs`, `npc-dialog-inventory-and-economy.md` §1.2 | `ProtocolOpcodes.cs:136` (`s2c_scriptaction = 0x63`) |
| **`c2s_playerselui` (`0x5F`)** | 5B legacy payload (`byte + int`) | Extended **17B** `<Biiii` layout (`SelectIndex`, `SelectType`, `Param1`, `Param2`) | `C2S_PlayerSelUiPacket.cs` (17B `<Biiii`), `npc-dialog-inventory-and-economy.md` §1.3 | `ProtocolOpcodes.cs:44` (`0x5F`), **missing in `ProtocolSizeTable.cs`** |
| **`c2s_playereatitem` (`0x59`)** | Consumable / script usage | Dispatches consumables or scriptable items (`MagicScript = 6`) | `npc-dialog-inventory-and-economy.md` §2 | `PlayerEatItemC2SPacket.cs:10-16` (16B with `TargetId`), `PlayerEatItemPacketHandler.cs:40-49`, `MapZoneWorker.cs:1923-1962` |
| **Thin Shop / Box UI (`0x84`, `0x88`)** | Shop & storage openers | Triggers merchant shop UI (`0x84`, 13B) and repository UI (`0x88`, 3B) | `npc-dialog-inventory-and-economy.md` §3.1 | `ProtocolOpcodes.cs:145` (`0x84` 13B), `ProtocolOpcodes.cs:149` (`0x88` 3B) |

---

## 2. Research Decisions & Protocol Verification

### 2.1 `c2s_dialognpc` (`0x6E`, 9 Bytes) & Conversational Proximity Gate

- **Decision**: Parse `c2s_dialognpc` (`0x6E`) as a fixed **9-byte** little-endian payload (`<BII`: `byte ProtocolType = 0x6E`, `uint TargetNpcId`, `uint CheckValue`), validate `CheckValue` via `MovementAntiCheatConsts.IsValidCheckValue(checkValue, token1, token2, MovementAntiCheatConsts.DialogNpcCheckKey)` (`0x506C`), and enforce the exact Linux 6.0 Euclidean conversational distance gate inside `ScriptingCommandHandler`:
  $$\text{distanceMps} \le (\text{DialogRadiusCells} \times 32) \times 2$$
  where `DialogRadiusCells` defaults to `4` cells (`4 * 32 = 128` Mps base radius $\Rightarrow$ `256` Mps maximum interaction distance).
- **Rationale & Evidence**:
  - In the legacy 6.0 protocol, dialog requests validate the anti-cheat token with key `0x506C` (`20588` decimal, verified in `MovementAntiCheatConsts.cs:13` and `jx-swarm` `C2S_DialogNpcPacket.cs:16-28`).
  - Proximity gating computes integer Euclidean distance between the player's current `(CurrentMpsX, CurrentMpsY)` and the target NPC's `(MpsX, MpsY)` (`distance <= Npc.DialogRadiusMps * 2`). Because `1 cell = 32 Mps` (`WorldCoordinateConsts`), an NPC with default `DialogRadiusCells = 4` (`128` Mps) permits dialog up to `256` Mps (`distanceSquared <= 256L * 256L = 65536` using overflow-safe `long` math).
  - Pre-dispatch session gates MUST also verify:
    1. Active session lease (`player.LeaseEpoch == cmd.LeaseEpoch`) and `player.IsSyncCompleted`.
    2. Player is alive (`player.CurrentLife > 0 && player.Doing != EntityDoingPosture.Death`).
    3. Player is not currently locked in an active player-to-player trade session.
    4. Target NPC exists in `context.StaticNpcs`, resides on the same `MapId`, is alive (`npc.CurLife > 0 && npc.Doing != EntityDoingPosture.Death`), lies within the 3x3 AOI neighborhood (`Math.Abs(player.RegionX - npc.RegionX) <= 1 && Math.Abs(player.RegionY - npc.RegionY) <= 1`), and has a non-empty `ScriptCode` (or mapped `ScriptPath`).
- **Alternatives Considered**: Manhattan/Chebyshev cell distance was rejected because legacy 6.0 proximity checking uses integer Euclidean distance (`dx*dx + dy*dy <= maxDist*maxDist`).

---

### 2.2 `s2c_scriptaction` (`0x63`, Variable `16 + m_nBufferLen` Bytes) — Linux 6.0 Wire Fidelity

- **Decision**: Implement `ScriptActionS2CPacket` (`0x63`) matching the exact 16-byte `tagPLAYER_SCRIPTACTION_SYNC` header (`<BHBBBBBii`) plus `m_pContent` (`0..900` bytes, `MAX_ANSWERNUM = 50`), encoding UTF-8 strings to `TCVN3` strictly at the `JXReborn.Adapter.Legacy` boundary:
  - **Header Invariants**:
    - `+0x00` (`1B`): `ProtocolType = 0x63` (`s2c_scriptaction`)
    - `+0x01` (`2B` `ushort` LE): `m_wProtocolLong = (ushort)(15 + m_nBufferLen)` (`TotalPayloadSize - 1`, verified in legacy 6.0 protocol captures)
    - `+0x03` (`1B`): `m_nOperateType` (`0` = `Say` / `Talk` UI show, `2` = `Describe` / `CreateNewSayEx`)
    - `+0x04` (`1B`): `m_bUIId` (`0` = `UI_SELECTDIALOG` for `Say`/`Describe`, `2` = `UI_TALKDIALOG` for `Talk`)
    - `+0x05` (`1B`): `m_bOptionNum` (`0..50`, clamped to `MaxDialogOptions = 50`)
    - `+0x06` (`1B`): `m_bParam1 = 0` (inline string content rather than client string-table ID)
    - `+0x07` (`1B`): `m_bParam2 = 1` (server-authoritative interactive UI flag)
    - `+0x08` (`4B` `int` LE): `m_nParam` (`-1` / `0xFFFFFFFF` for `Say` and final-chunk `Describe`; for `Talk`, `1` when an `onComplete` continuation is registered on the server, else `0`)
    - `+0x0C` (`4B` `int` LE): `m_nBufferLen` (`0..900` bytes)
    - `+0x10` (`m_nBufferLen` bytes): `m_pContent`
  - **Payload Encoding Variants (Early vs. 6.0 Wire Reconciliation)**:
    1. **`Say` (`m_nOperateType = 0`, `m_bUIId = 0`, `m_nParam = -1`) & `Describe` (`m_nOperateType = 2`, `m_bUIId = 0`, `m_nParam = -1`)**:
       While early 2003 builds used a `300`-byte `'|'`-delimited buffer, Version 6.0 (verified in `jx-swarm` `TagPlayerScriptActionSync.cs:47-68` and `npc-dialog-inventory-and-economy.md` §1.2) expands buffer capacity to **`900` bytes** and packs `m_pContent` with **null-terminated TCVN3 strings**:
       `[Prompt TCVN3][0x00][Opt0 TCVN3][0x00]...[Opt(N-1) TCVN3][0x00]`.
       Crucially, option callback delegates (`Func<IScriptDialogContext, ValueTask>`) remain **100% server-side** inside `PlayerDialogFeatureSlot`—unlike legacy Lua `"Option/funcName"` strings, no function/method names ever touch the wire or client memory.
    2. **`Talk` (`m_nOperateType = 0`, `m_bUIId = 2`, `m_nParam = hasCallback ? 1 : 0`)**:
       Multi-page narrative monologues set `m_bOptionNum = (byte)pageCount` and concatenate TCVN3 page strings separated by the 3-byte 6.0 delimiter `"| |"` (`0x7C, 0x20, 0x7C`) after each page (verified against `TagPlayerScriptActionSync.cs:105-112`). When `m_nParam == 1`, the client sends `c2s_playerselui` (`0x5F`, `SelectIndex = 0`) after the player clicks through the final page; when `m_nParam == 0`, no `0x5F` packet is sent.
    3. **Thin Core UI Triggers (`0x84` & `0x88`)**:
       When an NPC dialog option opens a merchant shop (`Sale(shopId)`) or personal storage box (`OpenBox()`), legacy scripts emit `s2c_opensalebox` (`0x84`, 13 bytes `<Biii`: `0x84`, `int32 ShopId`, `int32 ScalePercent = 100`, `int32 CurrencyType = 0`) or `s2c_openstorebox` (`0x88`, 3 bytes, `ProtocolOpcodes.cs:149`). In `012`, `IScriptDialogContext` exposes `OpenShop(int shopId, byte currencyType = 0, int scalePercent = 100)` and `OpenStoreBox()` routed via `IDialogOutboundSink` as thin Engine-First triggers ready for `017` (NPC Shops) and `018` (Repository Box).

---

### 2.3 `c2s_playerselui` (`0x5F`, 17 Bytes) — Missing `ProtocolSizeTable` Entry & Nested Menu Ordering

- **Decision**:
  1. Add `ProtocolOpcodes.c2s_playerselui => payload.Length == 17` to `ProtocolSizeTable.IsValidGameServerC2SPayload` (`src/JXReborn.Adapter.Legacy/Framing/ProtocolSizeTable.cs:62`).
  2. Implement `PlayerSelectUiC2SPacket` (`0x5F`, **17 bytes** `<Biiii`: `byte ProtocolType = 0x5F`, `int SelectIndex`, `int SelectType`, `int Param1`, `int Param2`).
  3. **Mandatory Pre-Callback State Clearing Rule**: Inside `ScriptingCommandHandler.HandleAsync(SelectDialogUiZoneCommand)`, snapshot and **clear** `PlayerDialogFeatureSlot`'s pending callback state *before* invoking the selected callback delegate.
- **Rationale & Evidence**:
  - **2003 vs. 6.0 Struct Size Trap**: Early 2003 protocol specifications defined the selection reply packet as 5 bytes (`byte + int`), whereas Version 6.0 (verified in `jx-swarm` `C2S_PlayerSelUiPacket.cs:9-24` and `ProtocolOpcodes.cs:44`) uses **17 bytes**, adding `SelectType`, `Param1`, and `Param2`. Currently `ProtocolSizeTable.cs` omits `c2s_playerselui`, which causes the framing layer to reject every dialog click from a 6.0 client or `jx-swarm` bot!
  - **Pre-Callback Reset Invariant**: In the legacy dialog state machine, pending feedback flags clear *before* invoking the option handler. If a server clears pending state in a `finally` block *after* `await callback(dialogCtx)`, any nested `ctx.Say(...)` or `ctx.Talk(...)` invoked inside `callback` has its newly registered options immediately wiped out! Clearing pending state *prior* to invoking `callback` guarantees arbitrarily deep multi-step menus (`Xa Phu` -> Region -> Town -> Confirmation) work deterministically.
  - **Safe Cancellation**: If the player has no active pending dialog, or `SelectType != 0`, or `SelectIndex < 0` (client Escape / Close button), or `SelectIndex >= OptionCount`, the server clears pending state (when negative/canceled) and returns cleanly without disconnecting the session.

---

### 2.4 `c2s_playereatitem` (`0x59`, 16 Bytes), `ItemGenreKind` Alignment & Explicit `ItemUsageResult`

- **Decision**:
  1. Align `ItemGenreKind` (`src/JXReborn.Primitives/Items/ItemGenreKind.cs`) with `schemas/item-catalog.schema.json` and legacy 6.0 `item_genre` values (`Equipment = 0`, `Medicine = 1`, `QuestKey = 4`, `TownPortal = 5` alongside legacy `2`/`3` compatibility aliases if needed, and `MagicScript = 6`), ensuring `ItemGenreKind.MagicScript = 6` (`item_magicscript`) routes to `IZoneScriptHost`.
  2. Forward `packet.TargetId` (`uint`, offset `+12..+15` in `PlayerEatItemC2SPacket.cs:16,34`) from `PlayerEatItemPacketHandler.cs:40-49` into an optional `uint TargetNpcId = 0` parameter on `UseInventoryItemZoneCommand` (`src/JXReborn.ZoneEngine/Items/UseInventoryItemZoneCommand.cs`).
  3. Add optional `ItemScriptUsageTemplate? ScriptUsage = null` to `ItemTemplate` (`src/JXReborn.GameAssets/Items/ItemTemplate.cs`) carrying `(string? ScriptCode, int CastSkillId, ushort CastSkillLevel, ushort MinRequiredLevel, bool ConsumeOnSkillFallback)`.
  4. Replace legacy Lua's error-prone inverted integer return convention with a self-documenting C# enum `ItemUsageResult { KeepItem = 0, ConsumeOne = 1 }`.
- **Rationale & Evidence**:
  - In legacy script handlers, returning `0` consumed the item while returning `1` prevented deletion—a notorious source of accidental item loss or infinite-use bugs across thousands of legacy Lua scripts. An explicit `ItemUsageResult` enum (`ConsumeOne` vs. `KeepItem`) makes script intent unmistakable at compile time and defaults safely to `KeepItem` if an exception is caught (`FR-007`, `FR-014`).
  - `PlayerEatItemC2SPacket` (`0x59`, 16 bytes) already parses `TargetId` at bytes `[12..15]`, used by targeted magic-script items (e.g., capture nets, targeted quest talismans, fireworks). Forwarding `TargetId` onto `UseInventoryItemZoneCommand` exposes it cleanly on `IScriptItemContext.TargetNpcId`.
  - When `item.Genre == ItemGenreKind.MagicScript`:
    - If `ScriptUsage.ScriptCode` is non-empty, `MapZoneWorker.HandleUseInventoryItemAsync` resolves `IItemUsageScript` from `IZoneScriptHost` and invokes `OnUseItemAsync(IScriptItemContext, CancellationToken)`. Only when the result is `ItemUsageResult.ConsumeOne` does the worker call `ConsumeOneStackFromItem(player, item)`.
    - Otherwise, if `ScriptUsage?.CastSkillId > 0`, the worker triggers the configured skill (`CastSkillId`, `CastSkillLevel`) and consumes one unit when `ConsumeOnSkillFallback` is `true` (`FR-008`).

---

### 2.5 Collectible `AssemblyLoadContext` Hot-Reload & Non-Pinning Interface Boundary

- **Decision**:
  1. Place all scripting contracts and value types (`IZoneScriptHost`, `INpcDialogScript`, `IItemUsageScript`, `ITrapScript`, `IMissionController`, `IScriptDialogContext`, `IScriptItemContext`, `IScriptTrapContext`, `IMissionContext`, `DialogOption`, `ItemUsageResult`) inside the **default non-collectible** `JXReborn.ZoneEngine` assembly (`src/JXReborn.ZoneEngine/Scripting/`).
  2. Implement the runtime host (`ZoneScriptHost`), immutable `ScriptRegistrySnapshot` (`FrozenDictionary<string, INpcDialogScript>`, `FrozenDictionary<string, IItemUsageScript>`, `FrozenDictionary<string, ITrapScript>`, `FrozenDictionary<string, Func<IMissionController>>`), fluent dialog builder, and built-in scripts inside `src/JXReborn.Scripts/`.
  3. For dynamic hot-reload (`CollectibleScriptAssemblyLoader`), load compiled `.dll` (and optional `.pdb`) bytes strictly via `File.ReadAllBytes` -> `MemoryStream` -> `AssemblyLoadContext.LoadFromStream(peStream, pdbStream)` inside a custom `AssemblyLoadContext(name, isCollectible: true)`—**never** calling `LoadFromAssemblyPath`, which locks `.dll` files on Windows NTFS.
  4. Swap active script registries atomically via `Interlocked.Exchange(ref _currentSnapshot, nextSnapshot)` and read via `Volatile.Read(ref _currentSnapshot)` on the 18-Hz zone thread (`0` locks, `0` allocations per lookup).
- **Rationale & Evidence**:
  - For .NET's `AssemblyLoadContext(isCollectible: true)` to unload cleanly (`WeakReference.IsAlive == false` after `Unload()` + `GC.Collect()`), no long-lived object in the default `AssemblyLoadContext` (such as `PlayerZoneState` or `MapZoneWorker`) may retain strong references to `Type`s, instances, or lambda closures allocated inside the collectible ALC.
  - Because `PlayerDialogFeatureSlot` clears its `Func<IScriptDialogContext, ValueTask>[]` array immediately before callback execution, on dialog cancel, and inside `IPlayerFeatureSlot.OnPlayerDetached(...)`, any closure capturing a collectible script instance lives only for the brief window of an open dialog. Once pending dialogs conclude and `_currentSnapshot` swaps to the new generation, zero roots reference the retired `AssemblyLoadContext`, guaranteeing deterministic reclamation (`SC-005`).
  - Validating all discovered script types (duplicate `ScriptCode` check, parameterless constructor instantiation, interface compliance) *before* `Interlocked.Exchange` ensures a broken reload package leaves the previous `ScriptRegistrySnapshot` untouched (`FR-013`).

---

### 2.6 Modular Zone Spine Integration (`ScriptingZoneModule : IZoneModule`)

- **Decision**: Register dialog commands (`DialogNpcZoneCommand`, `SelectDialogUiZoneCommand`), the player dialog state slot (`PlayerDialogFeatureSlot`), and the periodic mission tick system (`MissionZoneTickSystem` at `(ZoneTickPhase)550`, between `MovementAndPosture = 500` and `SessionHeartbeats = 600`) via `ScriptingZoneModule : IZoneModule`, while injecting `IZoneScriptHost? ScriptHost` onto `ZoneAssetServices` for `MapZoneWorker` item-usage (`HandleUseInventoryItemAsync`) and custom-trap (`EvaluatePlayerTrapAsync` when `ActionKind == PortalTrapActionKind.CustomScript`) hooks.
- **Rationale**: Follows the exact Open/Closed `IZoneModule` + `IPlayerFeatureSlot` + domain-segregated `IDialogOutboundSink` pattern established in `016-module-chat-channels` (`ChatZoneModule` / `IChatOutboundSink`), keeping wire packets in `JXReborn.Adapter.Legacy`, simulation interfaces in `JXReborn.ZoneEngine`, and concrete gameplay scripts in `JXReborn.Scripts`.
