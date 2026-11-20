# Quickstart & Verification Guide: Factions, Skill Trees & Point Washing (`015-core-factions-and-skills`)

This guide walks through building, re-compiling the `JX Reborn Data Standard v1` player snapshot, and executing the 3-tier automated verification suite for **Feature `015-core-factions-and-skills`**.

---

## 1. Build & Recompile `players.pack` Snapshot

From the repository root:

```powershell
# 1. Build the entire solution with zero warnings under TreatWarningsAsErrors
dotnet build JXReborn.slnx -c Release

# 2. Validate data/players/progression.players.json against schemas/player-template.schema.json
#    and recompile MemoryPack runtime snapshots (players.pack + manifest.json SHA-256)
dotnet run --project tools/JXReborn.DataExporter -- compile-snapshots --data-dir data --schema-dir schemas --output-dir artifacts/snapshots
```

Expected check:
- `data/players/progression.players.json` validates cleanly with all 10 classic sects (`0..9`) carrying canonical `skillIds` and aligned `factiontitle.txt` `titles`.
- `JXReborn.slnx` compiles with `0 Warning(s)` and `0 Error(s)`.

---

## 2. Tier 1 — Wire Protocol & Schema Symmetry Gate (`JXReborn.Protocol.Tests`)

Validates byte-exact framing, `ProtocolSizeTable`, and raw hex test vectors for `0x55` (`1` B), `0x7B` (`8` B), `0x59` (`6` B), `0x58` (`6` B), `0x45` (`483` B), `0x5D` (`14` B), and the aligned `0x5E` (`15` B) packet:

```powershell
dotnet test tests/JXReborn.Protocol.Tests --filter "FullyQualifiedName~FactionAndSkillPacketTests"
```

Key assertions verified:
- `ProtocolSizeTable` resolves `0x55` (`c2s_playerapplyfactiondata`) to fixed size `1`.
- `PlayerFactionDataS2CPacket` (`0x7B`) matches `7B 00 FF FF 00 00 00 00` (Novice), `7B 01 00 00 01 00 00 00` (Shaolin), and `7B 04 FF 02 01 00 00 00` (Graduated Tangmen).
- `PlayerSkillLevelS2CPacket` (`0x5E`) writes `[9..12] int32 UnallocatedSkillPoints` and `[13..14] ushort SkillExpPercent` matching `TagPlayerSkillLevelSync.cs` (`jx-swarm`).

---

## 3. Tier 2 — Deterministic 18-Hz `ZoneSimulationHarness` Gate (`JXReborn.ZoneEngine.Tests`)

Runs all sect induction/graduation matrices, `0 -> 1` skill allocation caps, 28 Level 90 books, Level 120 book, scriptures, and 100-cycle `WashPoints` conservation invariants in-memory without network or database overhead:

```powershell
dotnet test tests/JXReborn.ZoneEngine.Tests --filter "FullyQualifiedName~FactionJoinGraduateAndWashTests|FullyQualifiedName~SkillBooksAndScripturesTests"
```

Key scenarios verified:
1. **5-Series × 2-Gender × 10-Sect Matrix (`100` combinations)**:
   - Enforces `Level >= 10`, `CurrentFactionId == 0xFF`, `Series` match, Male-only Shaolin (`0`), Female-only Emei (`4`) & Cuiyan (`5`), and inactive sect rejection (`>= 10`).
   - Grants all canonical sect skills (`13..18` skills per sect) at `BaseLevel = 0` and broadcasts `0x59` + `0x58` to nearby AOI observers in the same tick (`<= 55.5 ms`).
2. **Graduation & Re-Induction**:
   - Rejects graduation at Level `59`; graduates at Level `60` (`CurrentFactionId = 0xFF`, `FirstFactionId` preserved, `BaseCamp = CurrentCamp = Free [4]`); allows re-induction into a sibling sect of the same series (`FactionJoinCount == 2`, `FirstFactionId` still preserved).
3. **`0 -> 1` Skill Point Allocation & Proficiency Locks**:
   - Allocates `0 -> 1` on newly granted sect skills and the 5 non-exp 90x books (`332, 351, 390, 391, 394`).
   - Enforces `MaxAllowedLevel = Math.Min(tpl.MaxLevel, player.Level - tpl.ReqLevel + 1)`.
   - Blocks manual point allocation into starter melee (`1, 2, 53, 229..232`), utility (`210, 400`), and `IsExpSkill == true` skills (`302..380`, `708..717`).
4. **Books, Scriptures & `WashPoints` Conservation**:
   - Verifies all 28 Level 90 books (`23` at `BaseLevel = 1`, `5` at `BaseLevel = 0`) and all 10 Level 120 mappings (`0 -> 709`, `1 -> 708`, `2..9 -> 710..717`) for both active disciples and graduated Free-camp martial artists via `EffectiveFactionId`.
   - Consumes up to `15` *Võ Lâm Mật Tịch* (`+15` skill points, `Task[80] & 0xF`) and `15` *Tẩy Tủy Kinh* (`+75` flat attribute points at Level `80`, `Task[81] & 0xF`), rejecting the 16th volume without item loss and preserving upper task bits.
   - Runs repeated `WashAttributes` and `WashSkills` cycles, proving zero point loss/duplication, aura deactivation when washed to `0`, vital clamping (`CurrentLife <= MaxLife`), and untouched `IsExpSkill` levels/experience.

---

## 4. Tier 3 — Snapshot Persistence & Headless TCP Client Integration Gate (`JXReborn.Integration.Tests`)

Verifies full Control-Plane EF Core persistence (`UpdateCharacterWorldSnapshotDto`) and live encrypted Legacy 6.0 TCP framing:

```powershell
dotnet test tests/JXReborn.Integration.Tests --filter "FullyQualifiedName~FactionProgressionAndWashIntegrationTests"
```

Key end-to-end flows verified:
- **Logout / Re-Attach Persistence Round-Trip**: A disciple joins a sect (granting `BaseLevel = 0` skills), allocates points into subset skills, trains a Level 90 proficiency skill to `SkillExp > 0`, graduates at Level 60, detaches from `MapZoneWorker` (persisting `UpdateCharacterWorldSnapshotDto`), and re-attaches in a fresh session—verifying `CurrentFactionId`, `FirstFactionId`, `FactionJoinCount`, `Free` camp, all `BaseLevel == 0` slots, and exact `uint SkillExp` are restored identically.
- **Live Encrypted TCP Bot Sync**: Two headless clients connect over TCP; Bot A sends `0x55` (`c2s_playerapplyfactiondata`), joins a sect, allocates `0 -> 1` (`0x52`), washes points, and graduates while Bot B in the same 3x3 region receives `0x59` (`s2c_npcchgcamp`) and `0x58` (`s2c_npcchangecurcamp`) frames on wire.

---

## 5. Tier 4 — Live Zero-Sim Containerized Differential Parity Gate (`JXReborn.ClientDriver.Tests` — Journey `J06`)

Verifies Emei series-mismatch rejection, symmetric `Lv10` GM Token seeding, Wudang (`Series=4`) induction (`Map 81`, `Camp=1`, `12` sect skills), `+1 Str/Vit/Eng` Earth-series vital deltas, and `0 -> 1` skill allocation + `ReqLevel` cap against `jx-runtime-standalone`:

```powershell
.\tools\run-parity.ps1 -Profile minimal -Filter "DifferentialParityFactionAndSkillTests"
```
