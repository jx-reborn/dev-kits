# Implementation Plan: Factions, Skill Trees & Point Washing

**Branch**: `015-core-factions-and-skills` | **Date**: 2026-11-11 | **Spec**: [spec.md](spec.md)  
**Input**: Feature specification from `/specs/015-core-factions-and-skills/spec.md`

## Summary

Implement end-to-end classic ten-sect martial arts progression (`0..9`), Five-Elements and gender-gated sect induction (`Level >= 10`) with persistent `BaseLevel = 0` skill tree grants, sect graduation (`Level >= 60`) with `Free` camp transitions and preserved `FirstFactionId`/`EffectiveFactionId`, level-gated skill point allocation (`0 -> 1` and `min(MaxLevel, Level - ReqLevel + 1)`), attribute and skill point washing (`Level >= 50`) with 100% scripture bonus point conservation, and all 28 Level 90 skill books, the Level 120 skill book, *Võ Lâm Mật Tịch*, and *Tẩy Tủy Kinh* (`+5` flat attribute points per volume). Fix discovered `PlayerDataExporter` faction `skillIds`/`titles` gaps, Control-Plane faction & `SkillExp` snapshot persistence gaps, `PlayerZoneState` `BaseLevel == 0` retention, missing `0x55`/`0x7B`/`0x59` wire packets, and the swapped field bug in `0x5E` `PlayerSkillLevelS2CPacket`.

## Technical Context

**Language/Version**: C# 14 / .NET 10 (`net10.0`, `<Nullable>enable</Nullable>`, `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>`)  
**Primary Dependencies**: `MemoryPack` (Tier-2 static game asset `.pack` snapshots), `System.Collections.Frozen` (`FrozenDictionary` catalogs), `EF Core 10` (Control-Plane character persistence), `JXReborn.Scripts` (C# 14 Fluent Scripting API for NPC dialogs and book item usage)  
**Storage**: Dual-Tier `JX Reborn Data Standard v1` (`data/players/progression.players.json` + `players.pack`) for static sect templates; EF Core 10 (`SQL Server` / `SQLite` / `InMemory`) via asynchronous write-behind `UpdateCharacterWorldSnapshotDto` for `CurrentFactionId`, `FirstFactionId`, `FactionJoinCount`, `TaskValues` (`80`, `81`, `88`, `2463`), and `LearnedSkills` (`BaseLevel >= 0` + `uint SkillExp`)  
**Testing**: `xUnit` across 3 verification tiers: `JXReborn.Protocol.Tests` (packet framing & test vectors), `JXReborn.ZoneEngine.Tests` (`ZoneSimulationHarness` deterministic 18-Hz tick verification), and `JXReborn.Integration.Tests` (snapshot round-trip + headless TCP client sessions)  
**Target Platform**: Cross-platform .NET 10 Server Host (Modular Monolith & Sharded Multi-Node Cluster)  
**Project Type**: Real-time 18-Hz MMORPG Server Engine + Control Plane + Legacy 6.0 Protocol Adapter  
**Performance Goals**: Zero heap allocations on hot packet serialization (`Span<byte>` / `stackalloc`), deterministic `< 1 ms` `JoinFaction` / `GraduateFaction` / `WashPoints` execution inside the single-threaded 18-Hz (`55.5 ms`) map tick, immediate same-tick 3x3 AOI camp broadcast (`0x59` + `0x58`)  
**Constraints**: Strict 1-type-per-file rule, `sealed` concrete classes, pluralized feature directories, zero ORM/EF Core leakage into `JXReborn.ZoneEngine`, zero Lua VM (all dialogs and books in C# 14 `JXReborn.Scripts`), boundary-only TCVN3 transcoding in `JXReborn.Adapter.Legacy`  
**Scale/Scope**: 10 active classic sects (`0..9`), 142 base sect skill mappings, 28 Level 90 skill books, 10 Level 120 skill mappings, 2 permanent bonus scriptures (`15`-use nibble counters), 4 wire packets added/aligned (`0x55`, `0x7B`, `0x59`, `0x5E`)

## Constitution Check (v1.4.0)

*GATE: Verified against `.specify/memory/constitution.md` (`v1.4.0`) before Phase 0 and re-verified after Phase 1 design.*

- [x] **Principle I (Hybrid Control Plane & Sharded 18-Hz Zone Architecture)**: All sect induction, graduation, skill allocation, and point washing execute lock-free inside the owning `MapZoneWorker` tick and persist asynchronously via `UpdateCharacterWorldSnapshotDto` without blocking the 18-Hz loop.
- [x] **Principle II (Multi-Protocol Transport Abstraction)**: `JXReborn.ZoneEngine` emits canonical sink calls (`SendPlayerFactionDataSync`, `SendNpcChangeBaseCampBroadcast`, `SendPlayerSkillLevelSync`, `SendPlayerSkillListSync`) and processes internal `*ZoneCommand` records; wire framing (`0x55`, `0x7B`, `0x59`, `0x5E`) lives exclusively in `JXReborn.Adapter.Legacy`.
- [x] **Principle III (Zero-Hallucination Evidence & Ground-Truth Fidelity)**: Every sect rule, skill ID list, book mapping, bit-packed task counter, and packet layout is verified against legacy 6.0 protocol contracts, data tables (`skills_table.lua`, `skillbook.lua`, `lv120skillbook.lua`, `wulin-miji.lua`, `xisui-jing.lua`, `路人_叛僧.lua`), and `jx-swarm` (documented in [research.md](research.md)).
- [x] **Principle IV (Zero-Dupe Economy & Lease-Fenced Session Integrity)**: Book/scripture consumption validates all level, effective-sect, duplicate-skill, and 15-use cap gates *before* removing `1` item from `PlayerZoneState` inventory inside the single-threaded zone tick, preventing lost items on failure or double-consumption races.
- [x] **Principle V (UTF-8 Domain Core & Dual-Tier `JX Reborn Data Standard v1`)**: `data/players/progression.players.json` remains UTF-8 JSON validated against `schemas/player-template.schema.json` and compiles into `players.pack` (`SHA-256` verified) with `FactionTemplate` (`id` + `code`).
- [x] **Principle VI & Structural Naming Rules**: Strictly enforces **1 type per `.cs` file**, **`sealed` concrete classes**, **pluralized feature folders** (`Characters/`, `PlayerTemplates/`, `Factions/`, `PlayerSessions/`, `Items/`, `Packets/`, `GameServer/`), and **C# 14 Fluent Scripting API (`JXReborn.Scripts`)** for sect masters, Marrow-Washing Monk, and skill/bonus books.

## Project Structure

### Documentation (this feature)

```text
specs/015-core-factions-and-skills/
├── spec.md                                           # Technology-agnostic feature specification
├── plan.md                                           # Technical implementation plan (this file)
├── research.md                                       # Ground-truth evidence & bug audit
├── data-model.md                                     # Entities, state machine & WashPoints math
├── quickstart.md                                     # Build & 3-tier verification guide
├── checklists/
│   └── requirements.md                               # 16/16 SpecKit quality validation
├── contracts/
│   ├── faction-and-skill-wire-protocol.md            # Byte-exact 0x55, 0x7B, 0x59, 0x58, 0x45, 0x5D, 0x5E specs
│   └── factions-washpoints-and-books-contracts.md    # 10-sect table, 28 90x books, 120x book, scriptures & scripts
└── tasks.md                                          # Dependency-ordered implementation checklist
```

### Source Code (repository root `the repository root`)

```text
data/
└── players/
    └── progression.players.json                      # [MODIFY] Populate 10-sect skillIds + fix factiontitle.txt titles

tools/
└── JXReborn.DataExporter/
    ├── Exporters/
    │   └── PlayerDataExporter.cs                     # [MODIFY] Export canonical sect skillIds & fix FACTION title mapping
    └── Compilation/
        └── MemoryPackSnapshotCompiler.cs             # [MODIFY] Include FactionTemplate list when compiling players.pack

src/
├── JXReborn.Primitives/
│   └── Characters/
│       └── CharacterConsts.cs                        # [MODIFY] ClassicFactionCount=10, level gates, book caps & Task IDs
│
├── JXReborn.Contracts/
│   └── Characters/
│       ├── CharacterSkillDto.cs                      # [MODIFY] Add uint SkillExp = 0
│       ├── CharacterSkillSnapshotDto.cs              # [NEW] Write-behind skill snapshot DTO (BaseLevel >= 0 + SkillExp)
│       └── UpdateCharacterWorldSnapshotDto.cs        # [MODIFY] Add CurrentFactionId, FirstFactionId, FactionJoinCount, LearnedSkills
│
├── JXReborn.Domain/
│   └── Characters/
│       ├── CharacterSkillValue.cs                    # [MODIFY] Allow BaseLevel >= 0 and store uint SkillExp
│       └── Character.cs                              # [MODIFY] Persist faction identity & BaseLevel == 0 skills in UpdateWorldSnapshot
│
├── JXReborn.Application/
│   └── Characters/
│       └── CharacterAppService.cs                    # [MODIFY] Map faction fields & SkillExp in ApplyWorldSnapshot & DTO projection
│
├── JXReborn.GameAssets/
│   ├── PlayerTemplates/
│   │   ├── FactionRankTitleTemplate.cs               # [NEW] MemoryPackable rank title record (Rank, MaleTitle, FemaleTitle)
│   │   ├── FactionTemplate.cs                        # [NEW] MemoryPackable sect definition (Id, Code, Series, Camp, Genders, SkillIds, Titles)
│   │   ├── IPlayerTemplateCatalog.cs                 # [MODIFY] Expose AllFactions, TryGetFactionById/Code, TryGetStarterTemplate
│   │   └── PlayerTemplateCatalog.cs                  # [MODIFY] FrozenDictionary<byte, FactionTemplate> & code lookup
│   └── Snapshots/
│       ├── GameAssetsSnapshotBundle.cs               # [MODIFY] Add IReadOnlyList<FactionTemplate>? Factions = null
│       └── GameAssetsSnapshotLoader.cs               # [MODIFY] Hydrate FactionTemplate list from JSON & players.pack
│
├── JXReborn.ZoneEngine/
│   ├── Factions/
│   │   ├── WashPointsMode.cs                         # [NEW] Enum: AttributesOnly = 1, SkillsOnly = 2, Both = 3
│   │   ├── JoinFactionZoneCommand.cs                 # [NEW] Zone command to induct player into sect 0..9
│   │   ├── GraduateFactionZoneCommand.cs             # [NEW] Zone command to graduate disciple (Xuất Sư -> Free camp)
│   │   ├── RequestFactionDataZoneCommand.cs          # [NEW] Zone command triggered by 0x55 c2s_playerapplyfactiondata
│   │   ├── WashPlayerPointsZoneCommand.cs            # [NEW] Zone command for Marrow-Washing attribute/skill reset
│   │   ├── FactionProgressionResult.cs               # [NEW] Strongly-typed result enum/record for faction/wash/book operations
│   │   ├── FactionProgressionManager.cs              # [NEW] Pure engine rules: induction, graduation, camp derivation, 0->1 cap, WashPoints, book grants
│   │   └── FactionProgressionCommandHandler.cs       # [NEW] Orchestrates FactionProgressionManager + outbound 0x7B/0x59/0x58/0x45/0x5D/0x5E syncs
│   ├── PlayerSessions/
│   │   ├── LearnedSkillSlot.cs                       # [NEW] readonly record struct (SkillId, BaseLevel, SkillExp) allowing BaseLevel == 0
│   │   ├── PlayerZoneState.cs                        # [MODIFY] FirstFactionId, FactionJoinCount, EffectiveFactionId, LearnedSkillSlot dictionary
│   │   ├── IWorldOutboundSink.cs                     # [MODIFY] Add SendPlayerFactionDataSync & SendNpcChangeBaseCampBroadcast
│   │   ├── NoOpWorldOutboundSink.cs                  # [MODIFY] Implement new faction/camp outbound sink methods
│   │   └── SelectivePlayerOutboundSink.cs            # [MODIFY] Forward new faction/camp outbound sink methods
│   ├── Scripting/
│   │   ├── IScriptDialogContext.cs                   # [MODIFY] Thin engine-first helpers: CanJoinFaction, TryJoinFaction, TryGraduateFaction, TryWashPoints
│   │   └── IScriptItemContext.cs                     # [MODIFY] Thin engine-first helpers: EffectiveFactionId, HasLearnedSkill, TryGrantSkill, AddBonusPoints
│   └── WorldZones/
│       ├── MapZoneWorker.cs                          # [MODIFY] Camp derivation on attach, initial 0x7B sync, 0->1 AllocateSkillPoint fix, snapshot BaseLevel >= 0
│       └── CoreZoneModule.cs                         # [MODIFY] Register FactionProgressionManager & FactionProgressionCommandHandler
│
├── JXReborn.Scripts/
│   ├── Factions/
│   │   ├── SectMasterDialogScript.cs                 # [NEW] C# 14 Fluent NPC dialog script for all 10 sect masters (induction + graduation)
│   │   └── MarrowWashingMonkDialogScript.cs          # [NEW] C# 14 Fluent NPC dialog script for Tẩy Tủy Đại Sư (WashAttributes / WashSkills / Both)
│   └── Items/
│       ├── SkillBook90ItemScript.cs                  # [NEW] C# 14 Fluent item script for all 28 Level 90 sect skill books (Particular 27..59)
│       ├── SkillBook120ItemScript.cs                 # [NEW] C# 14 Fluent item script for Level 120 sect skill book (Particular 1125)
│       ├── WuLinMiJiItemScript.cs                    # [NEW] C# 14 Fluent item script for Võ Lâm Mật Tịch (6, 1, 26 -> +1 skill point, cap 15)
│       └── XiSuiJingItemScript.cs                    # [NEW] C# 14 Fluent item script for Tẩy Tủy Kinh (6, 1, 22 -> flat +5 attr points, cap 15)
│
└── JXReborn.Adapter.Legacy/
    ├── Framing/
    │   └── ProtocolSizeTable.cs                      # [MODIFY] Register c2s_playerapplyfactiondata (0x55) fixed size = 1 byte
    ├── Packets/
    │   ├── ProtocolOpcodes.cs                        # [MODIFY] Add c2s_playerapplyfactiondata (0x55), s2c_playerfactiondata (0x7B), fix s2c_npcchgcamp (0x59)
    │   ├── PlayerApplyFactionDataC2SPacket.cs        # [NEW] 1-byte C2S packet (0x55)
    │   ├── PlayerFactionDataS2CPacket.cs             # [NEW] 8-byte S2C packet (0x7B: BaseCamp, CurFaction, FirstFaction, AddTimes)
    │   ├── NpcChangeCampS2CPacket.cs                 # [NEW] 6-byte S2C packet (0x59: NpcId, BaseCamp)
    │   └── PlayerSkillLevelS2CPacket.cs              # [MODIFY] Fix swapped bytes [9..12] int32 UnallocatedSkillPoints & [13..14] ushort SkillExpPercent
    └── GameServer/
        ├── PlayerApplyFactionDataPacketHandler.cs    # [NEW] Dispatches 0x55 -> RequestFactionDataZoneCommand
        ├── GameServerClientSession.WorldSink.cs      # [MODIFY] Serialize & encrypt 0x7B PlayerFactionDataS2CPacket & 0x59 NpcChangeCampS2CPacket
        └── GameServerClientSession.CombatSink.cs     # [MODIFY] Update SendPlayerSkillLevelSync call site to aligned PlayerSkillLevelS2CPacket

tests/
├── JXReborn.Protocol.Tests/
│   └── FactionAndSkillPacketTests.cs                 # [NEW] Symmetric round-trip & hex test vectors for 0x55, 0x7B, 0x59, 0x58, 0x45, 0x5D, 0x5E
├── JXReborn.ZoneEngine.Tests/
│   └── Factions/
│       ├── FactionJoinGraduateAndWashTests.cs        # [NEW] 5x2x10 induction matrix, graduation, re-join, 0->1 allocation, WashPoints conservation
│       └── SkillBooksAndScripturesTests.cs           # [NEW] All 28 90x books, 10 120x mappings, Võ Lâm Mật Tịch & Tẩy Tủy Kinh nibble caps
└── JXReborn.Integration.Tests/
    └── FactionProgressionAndWashIntegrationTests.cs  # [NEW] Snapshot persistence round-trip (BaseLevel == 0, SkillExp, Factions) + live TCP bot sync
```

**Structure Decision**: Multi-project modular architecture strictly aligned with `JXReborn` repository conventions and Constitution `v1.4.0` (Primitives → GameAssets / ZoneEngine / Scripts → Contracts / Domain / Application → Legacy Adapter).

## Architectural Design Details

### 1. Engine-First Faction & Skill Progression (`FactionProgressionManager`)
To keep interactive scripts in `JXReborn.Scripts` declarative, concise, and exploit-proof, core state transitions live in `sealed class FactionProgressionManager` inside `JXReborn.ZoneEngine/Factions/`:
- **`TryJoinFaction(PlayerZoneState player, byte factionId)`**: Validates `Level >= 10`, `CurrentFactionId == 0xFF`, `factionId < 10`, `faction.Series == player.Series`, and `faction.AllowedGenders.Contains(player.Gender)`. Updates `CurrentFactionId`, `FirstFactionId` (if `FactionJoinCount == 0`), `FactionJoinCount++`, `BaseCamp = CurrentCamp = faction.Camp`, and inserts any missing `faction.SkillIds` as `new LearnedSkillSlot(skillId, BaseLevel: 0, SkillExp: 0)`.
- **`TryGraduateFaction(PlayerZoneState player)`**: Validates `CurrentFactionId < 10` and `Level >= 60`. Sets `CurrentFactionId = 0xFF` and `BaseCamp = CurrentCamp = (byte)EntityCamp.Free` (`4`), preserving `FirstFactionId`, `FactionJoinCount`, and `LearnedSkills`.
- **`TryAllocateSkillPoint(PlayerZoneState player, int skillId)`**: Validates `UnallocatedSkillPoints >= 1`, `LearnedSkills` membership, `!tpl.IsExpSkill`, `!IsProtectedNonAllocatableSkill(skillId)`, `player.Level >= tpl.ReqLevel`, and `slot.BaseLevel < Math.Min(tpl.MaxLevel, player.Level - tpl.ReqLevel + 1)`. Supports `0 -> 1`.
- **`TryWashPoints(PlayerZoneState player, WashPointsMode mode)`**: Validates `player.Level >= 50`. Executes exact starter-base + `Task[88]` attribute refund (re-evaluating equipment activation, derived max vitals, and clamping current vitals) and/or non-protected `!IsExpSkill` skill reset to `BaseLevel = 0` (clearing `ActiveAuraSkillId` if reset).

### 2. Synchronization Choreography (`FactionProgressionCommandHandler`)
- **On World Entry (`MapZoneWorker` Attach)**: Derive `BaseCamp`/`CurrentCamp` from `(CurrentFactionId, FactionJoinCount)` → send `0x45` (`SyncCurPlayerSkillS2CPacket`, including `BaseLevel == 0` slots) → send `0x7B` (`PlayerFactionDataS2CPacket`).
- **On `0x55` (`PlayerApplyFactionDataC2SPacket`)**: Reply unicast `0x7B` (`PlayerFactionDataS2CPacket`).
- **On Sect Join**: Unicast `0x7B` + unicast `0x45` (full updated skill book with newly granted level-`0` sect skills) + 3x3 AOI broadcast `0x59` (`NpcChangeCampS2CPacket`) + 3x3 AOI broadcast `0x58` (`NpcChangeCurCampS2CPacket`).
- **On Sect Graduation**: Unicast `0x7B` + 3x3 AOI broadcast `0x59` (`Free = 4`) + 3x3 AOI broadcast `0x58` (`Free = 4`).
- **On `WashPoints`**:
  - `AttributesOnly` / `Both`: Unicast `4x` `0x5D` (`PlayerAttributeS2CPacket` for Str, Dex, Vit, Eng with updated `UnallocatedAttributePoints`) + vitals refresh (`0x46` / `0x44`).
  - `SkillsOnly` / `Both`: Unicast `0x45` (`SyncCurPlayerSkillS2CPacket` with washed `BaseLevel = 0` slots) + unicast `0x5E` (`PlayerSkillLevelS2CPacket` carrying updated `UnallocatedSkillPoints`).

## Complexity Tracking

No Constitution violations or architectural exceptions required (`0` violations).
