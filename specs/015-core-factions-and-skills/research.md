# Ground-Truth Research: Factions, Skill Trees & Point Washing (`015-core-factions-and-skills`)

**Date**: 2026-11-11  
**Status**: Complete — All mechanics, wire packets, formulas, and persistence requirements verified against legacy 6.0 compatibility vectors, `jx-swarm`, and `jx-server`.

---

## 1. Five-Elements Starter Base Attributes & Classic 10 Sects (`0..9`)

### Ground-Truth Sources
- Legacy 6.0 faction state machine rules (`CurrentFactionId`, `FirstFactionId`, `FactionJoinCount`, camp alignment transitions)
- `legacy-server/server1/settings/faction/门派设定.ini` (lines 6–74)
- `legacy-server/server1/settings/factiontitle.txt` (lines 1–82)
- `legacy-server/server1/script/global/skills_table.lua` (lines 1408–1548)
- `legacy-server/server1/script/global/特殊用地/梦境/npc/路人_叛僧.lua` (lines 135–145)
- `jx-server/data/players/progression.players.json` (lines 1–287) & `tools/JXReborn.DataExporter/Exporters/PlayerDataExporter.cs` (lines 341–464)

### Findings & Decisions
1. **Five-Elements Starter Base Attributes (Sum = `100` across all 5 series)**:
   Verified identically across `newplayerini00..09.ini`, `skills_table.lua:1408-1411`, `路人_叛僧.lua:137-140`, and `progression.players.json:6-98`:
   - `0` **Metal (*Kim*)**: `Strength = 35`, `Dexterity = 25`, `Vitality = 25`, `Energy = 15` (`InitialLifeMax = 204`, `InitialManaMax = 16`, `InitialStaminaMax = 100`)
   - `1` **Wood (*Mộc*)**: `Strength = 20`, `Dexterity = 35`, `Vitality = 20`, `Energy = 25`
   - `2` **Water (*Thủy*)**: `Strength = 25`, `Dexterity = 25`, `Vitality = 25`, `Energy = 25`
   - `3` **Fire (*Hỏa*)**: `Strength = 30`, `Dexterity = 20`, `Vitality = 30`, `Energy = 20`
   - `4` **Earth (*Thổ*)**: `Strength = 20`, `Dexterity = 15`, `Vitality = 25`, `Energy = 40`

2. **Classic 10 Martial Arts Sects (`0..9`) & `NoFaction = 0xFF` (`255`)**:
   - Defaults: `CurrentFactionId = -1` (`0xFF` unsigned byte on the wire), `FirstFactionId = -1` (`0xFF`), `FactionJoinCount = 0` (`CharacterConsts.NoFactionId = 0xFF`).
   - Series-to-faction gating enforces the exact 2-sect-per-series pairing `nFactionID in [nSeries * 2, nSeries * 2 + 1]` for series `0..4`, yielding the **10 classic sects `0..9`**.
   - `progression.players.json` also contains an 11th entry (`id: 10, code: "hoa-son-phai"`). Per Session 2026-11-11 clarification, `CharacterConsts.ClassicFactionCount = 10` gates active induction and 120x book lookup strictly to `0..9`, keeping `10` inactive in catalog data without breaking schema validation.
   - **Induction & Graduation Gates**:
     - **Induction**: `player.Level >= 10` (`MinFactionJoinLevel = 10`), `player.CurrentFactionId == 0xFF`, `factionId < 10`, `faction.Series == player.Series`, and `faction.AllowedGenders.Contains(player.Gender)` (`0` Thiếu Lâm: `[0]` Male-only; `4` Nga My & `5` Thúy Yên: `[1]` Female-only; all other 7 sects: `[0, 1]`).
     - **On Induction**: `CurrentFactionId = factionId`; if `FactionJoinCount == 0` (or `FirstFactionId == 0xFF`), set `FirstFactionId = factionId`; `FactionJoinCount++`; `BaseCamp = CurrentCamp = (byte)faction.Camp`; grant every skill in `faction.SkillIds` not yet in `player.LearnedSkills` at `BaseLevel = 0, SkillExp = 0`.
     - **Graduation (`LeaveFaction`)**: `player.CurrentFactionId < 10` and `player.Level >= 60` (`MinFactionGraduateLevel = 60`). Sets `CurrentFactionId = 0xFF`, preserves `FirstFactionId` and `FactionJoinCount`, and sets `BaseCamp = CurrentCamp = (byte)EntityCamp.Free` (`4`), matching the canonical graduation camp rule (returns `EntityCamp.Free` when `CurrentFactionId == 0xFF && FactionJoinCount > 0`, and `EntityCamp.Begin` when `FactionJoinCount == 0`).
     - **Effective Faction (`GetLastFactionNumber()`)**: `EffectiveFactionId = CurrentFactionId != 0xFF ? CurrentFactionId : FirstFactionId`. Used by Level 90 and Level 120 skill books so graduated Free-camp disciples retain full access to their original sect's endgame manuals.

3. **Complete 10-Sect Base Skill Tree Table (`skills_table.lua:1432-1533`, Tiers `[1..7]`: Lv10–Lv60 + Weapon Passives)**:
   All 142 sect skill IDs below were cross-checked against `jx-server/data/skills/base-and-factions.skills.json` (`100%` present with accurate `reqLevel`, `maxLevel`, `isExpSkill = false`, and `isAura`):

   | ID | Code | Legacy Key | Vietnamese Name | Series | Camp (`EntityCamp`) | Genders | Base Sect `SkillIds` (Granted at `BaseLevel = 0`) |
   | :-: | :--- | :--- | :--- | :-: | :-: | :-: | :--- |
   | `0` | `thieu-lam-phai` | `shaolin` | Thiếu Lâm phái | `0` (Kim) | `1` (`Justice`) | `[0]` | `10, 14, 4, 6, 8, 15, 16, 20, 11, 19, 271, 21, 273` (13) |
   | `1` | `thien-vuong-bang` | `tianwang` | Thiên Vương Bang | `0` (Kim) | `3` (`Balance`) | `[0, 1]` | `29, 30, 34, 23, 24, 26, 33, 31, 35, 37, 40, 42, 32, 36, 41, 324` (16) |
   | `2` | `duong-mon` | `tangmen` | Đường Môn | `1` (Mộc) | `3` (`Balance`) | `[0, 1]` | `45, 43, 347, 303, 47, 50, 54, 343, 345, 349, 48, 58, 249, 341` (14) |
   | `3` | `ngu-doc-giao` | `wudu` | Ngũ Độc Giáo | `1` (Mộc) | `2` (`Evil`) | `[0, 1]` | `63, 65, 60, 62, 67, 66, 70, 64, 68, 69, 384, 73, 356, 72, 71, 74, 75` (17) |
   | `4` | `nga-my-phai` | `emei` | Nga My phái | `2` (Thủy) | `1` (`Justice`) | `[1]` | `80, 85, 77, 79, 93, 82, 89, 385, 86, 92, 88, 91, 252, 282` (14) |
   | `5` | `thuy-yen-mon` | `cuiyan` | Thúy Yên môn | `2` (Thủy) | `3` (`Balance`) | `[1]` | `99, 102, 95, 97, 269, 105, 113, 100, 109, 108, 111, 114` (12) |
   | `6` | `cai-bang` | `gaibang` | Cái Bang | `3` (Hỏa) | `1` (`Justice`) | `[0, 1]` | `119, 122, 115, 116, 129, 124, 274, 277, 125, 128, 130, 360` (12) |
   | `7` | `thien-nhan-giao` | `tianren` | Thiên Nhẫn Giáo | `3` (Hỏa) | `2` (`Evil`) | `[0, 1]` | `135, 145, 131, 132, 136, 137, 138, 140, 141, 364, 143, 142, 148, 150` (14) |
   | `8` | `vo-dang-phai` | `wudang` | Võ Đang phái | `4` (Thổ) | `1` (`Justice`) | `[0, 1]` | `153, 155, 151, 152, 159, 158, 164, 160, 157, 165, 166, 267` (12) |
   | `9` | `con-lon-phai` | `kunlun` | Côn Lôn phái | `4` (Thổ) | `3` (`Balance`) | `[0, 1]` | `169, 179, 167, 168, 171, 392, 174, 172, 173, 178, 393, 175, 181, 90, 176, 182, 275, 630` (18) |

4. **Two Bugs Discovered in `PlayerDataExporter.cs` & `data/players/progression.players.json`**:
   - **Bug 1 — Missing `skillIds`**: `schemas/player-template.schema.json:170-173` already defines `"skillIds": { "type": "array", "items": { "type": "integer", "minimum": 0 } }` on `FactionDefinition`, which grants each sect's base skill list at level `0`, yet `PlayerDataExporter.ExportFactions` (`PlayerDataExporter.cs:341-464`) omitted `skillIds` completely, and `JXReborn.GameAssets` has no `FactionTemplate` in `PlayerTemplateCatalog` or `GameAssetsSnapshotBundle`.
   - **Bug 2 — Misaligned `titles` from `factiontitle.txt`**: `PlayerDataExporter.cs:361` mapped `int factionId = factionOneBased - 1;`. Inspection of `legacy-server/server1/settings/factiontitle.txt` (81 rows) proves its 1-based `FACTION` column uses historical creation order rather than `0..9` series order:
     - `1 -> 0` (Thiếu Lâm: *Tăng nhân* .. *Phương Trượng*)
     - `2 -> 8` (Võ Đang: *Đạo đồng* .. *Chưởng Giáo*)
     - `3 -> 4` (Nga My: *Ni cô* .. *Chưởng Môn*)
     - `4 -> 9` (Côn Lôn: *Đạo sĩ* .. *Hộ Quốc Chân Nhân*)
     - `5 -> 2` (Đường Môn: *Ám khí học đồ* .. *Lão Tổ*)
     - `6 -> 5` (Thúy Yên: *Hoa Sứ* .. *Hoa Tiên*)
     - `7 -> 6` (Cái Bang: *Đệ tử không túi* .. *Bang Chủ*)
     - `8 -> 1` (Thiên Vương: *Thị vệ* .. *Thiên Vương*)
     - `9 -> 3` (Ngũ Độc: *Ngũ Độc đồng tử* .. *Giáo Chủ*)
     - `10 -> 7` (Thiên Nhẫn: *Thập Phu Trưởng* .. *Thánh Chủ*)
     Because of `factionOneBased - 1`, `progression.players.json` currently assigned Võ Đang's Daoist titles to Thiên Vương (`1`) and Nga My's nun titles to Đường Môn (`2`)! Both `PlayerDataExporter.cs` and `data/players/progression.players.json` (plus compiled `.pack` snapshots) MUST fix `skillIds` and `titles`.

---

## 2. Persistence, Runtime State & Skill Allocation Fixes in `jx-server`

### Ground-Truth Sources
- `jx-swarm/docs/02-game-systems/movement-and-combat.md` (Legacy 6.0 skill allocation and `BaseLevel = 0` slot semantics)
- `jx-server/src/JXReborn.Contracts/Characters/UpdateCharacterWorldSnapshotDto.cs` & `CharacterSkillDto.cs`
- `jx-server/src/JXReborn.Domain/Characters/Character.cs` (lines 344–423) & `CharacterSkillValue.cs`
- `jx-server/src/JXReborn.Application/Characters/CharacterAppService.cs` (lines 220–310)
- `jx-server/src/JXReborn.ZoneEngine/PlayerSessions/PlayerZoneState.cs` (lines 21, 90–145, 336–358)
- `jx-server/src/JXReborn.ZoneEngine/WorldZones/MapZoneWorker.cs` (lines 620–680, 2492–2555, 2950–2995)

### Findings & Critical Fixes Required
1. **Persistence Gap — Faction State & `SkillExp` Lost on Zone Detach**:
   - `Character` domain aggregate already stores `CurrentFactionId` (`byte`, default `0xFF`), `FirstFactionId` (`byte`, default `0xFF`), and `FactionJoinCount` (`ushort`, default `0`) and exposes `Character.JoinFaction(byte)` and `Character.LeaveFaction()`.
   - However, `PlayerZoneState` only had `CurrentFactionId` (missing `FirstFactionId` and `FactionJoinCount`), and `UpdateCharacterWorldSnapshotDto`, `Character.UpdateWorldSnapshot`, `CharacterAppService.ApplyWorldSnapshot`, and `MapZoneWorker.BuildWorldSnapshotDto` **never persisted faction state back to the Control Plane**!
   - Furthermore, `CharacterSkillValue` and `CharacterSkillDto` stored `byte ExpPercent` (`0..100`) without `uint SkillExp`, losing sub-percent combat proficiency progress across relogs.
   - **Fix**: Add `byte? CurrentFactionId`, `byte? FirstFactionId`, `ushort? FactionJoinCount` to `UpdateCharacterWorldSnapshotDto`, `Character.UpdateWorldSnapshot`, and `CharacterAppService.ApplyWorldSnapshot`; add `uint SkillExp = 0` to `CharacterSkillValue`, `CharacterSkillDto`, and `CharacterSkillSnapshotDto`.

2. **`BaseLevel == 0` Stripping Bug (`LearnedSkillSlot`)**:
   - `PlayerZoneState.SetSkillLevel(int skillId, ushort level)` (`PlayerZoneState.cs:336-342`) previously rejected `level == 0` (`if (skillId > 0 && level > 0)`), and `MapZoneWorker.BuildWorldSnapshotDto` (`MapZoneWorker.cs:2955`) filtered `if (kvp.Key > 0 && kvp.Value > 0)`.
   - In legacy 6.0 (`0x45` `s2c_synccurplayerskill`), sect induction and the 5 non-exp 90x books add skills into the skill list with `BaseLevel = 0`, which the client renders in the F5 Skill Tree so the player can click `+` to allocate `0 -> 1`. Stripping `BaseLevel == 0` makes sect induction and `WashSkills` forget the player's entire skill tree!
   - **Fix**: Introduce `readonly record struct LearnedSkillSlot(int SkillId, ushort BaseLevel, uint SkillExp)` in `JXReborn.ZoneEngine/PlayerSessions/LearnedSkillSlot.cs`, store `Dictionary<int, LearnedSkillSlot>` on `PlayerZoneState`, allow `BaseLevel >= 0` for any `skillId > 0`, and serialize all `LearnedSkills` slots (`BaseLevel >= 0`) in both `0x45` `SyncCurPlayerSkillS2CPacket` (up to 80 slots) and `UpdateCharacterWorldSnapshotDto`.

3. **Skill Point Allocation (`0 -> 1`, Level Gate & Proficiency/Starter Locks)**:
   - `MapZoneWorker.HandleAllocateSkillPoint` (`MapZoneWorker.cs:2492-2540`) previously blocked `currentLevel == 0` (`if (currentLevel == 0) return;`) and did not check `skillTpl.ReqLevel` or `skillTpl.IsExpSkill`.
   - Verified rule (`c2s_playeraddskillpoint` + `SkillTemplate`):
     1. `player.UnallocatedSkillPoints >= 1`
     2. `player.TryGetLearnedSkill(skillId, out LearnedSkillSlot slot)` (skill MUST already be granted in `LearnedSkills`)
     3. `_gameAssets.Skills.TryGetById(skillId, out SkillTemplate? tpl)`
     4. `!tpl.IsExpSkill` AND `!tpl.IsWeaponSkill` AND `skillId` is not a protected non-allocatable starter/utility skill (`1, 2, 53, 210, 229, 230, 231, 232, 400`)
     5. `player.Level >= tpl.ReqLevel`
     6. `int maxAllowedLevel = Math.Min(tpl.MaxLevel, player.Level - tpl.ReqLevel + 1);`
     7. `slot.BaseLevel < maxAllowedLevel`
   - On success: `slot = slot with { BaseLevel = (ushort)(slot.BaseLevel + 1) }`, `player.UnallocatedSkillPoints--`, recompute passive/equipment bonuses if passive, and send `0x5E` `s2c_playerskilllevel` (`EffectiveLevel = BaseLevel + BonusLevel`, `UnallocatedSkillPoints`, `ExpPercent`).

4. **Deterministic Camp Derivation on Zone Attach**:
   - On `AttachPlayerZoneCommand` (`MapZoneWorker`), compute `BaseCamp` from faction state + `PlayerTemplateCatalog`:
     - If `CurrentFactionId < CharacterConsts.ClassicFactionCount` and `Factions.TryGetById(CurrentFactionId, out var faction)` => `BaseCamp = CurrentCamp = faction.Camp` (`1` Justice, `2` Evil, `3` Balance).
     - Else if `FactionJoinCount > 0` (or `FirstFactionId < CharacterConsts.ClassicFactionCount`) => `BaseCamp = CurrentCamp = (byte)EntityCamp.Free` (`4`).
     - Else => `BaseCamp = CurrentCamp = (byte)EntityCamp.Begin` (`0`).

---

## 3. Exact Wire Packets & Legacy Protocol Alignment

### Ground-Truth Sources
- Legacy 6.0 wire packet specifications (`0x55`, `0x58`, `0x59`, `0x5E`, `0x7B`)
- `jx-swarm/src/JXSwarm.Protocol/Packets/ProtocolOpcodes.cs`, `C2S_PlayerApplyFactionDataPacket.cs`, `TagPlayerFactionDataSync.cs`, `TagPlayerSkillLevelSync.cs`, `TagNpcChangeCurCampSync.cs`
- `jx-server/src/JXReborn.Adapter.Legacy/Framing/ProtocolSizeTable.cs`, `Packets/ProtocolOpcodes.cs`, `Packets/PlayerSkillLevelS2CPacket.cs`

### Exact Wire Layouts & Discovered Adapter Bugs
1. **`c2s_playerapplyfactiondata` (`0x55`, Fixed `1` Byte)**:
   - Wire layout: `[0] byte ProtocolType = 0x55`.
   - Verified fixed size `1` byte in legacy 6.0 wire framing and `jx-swarm` (`C2S_PlayerApplyFactionDataPacket.cs`).
   - **Adapter Fix**: Currently missing from `ProtocolOpcodes.cs` and `ProtocolSizeTable.cs` (`0x55` defaulted to `-2` unknown opcode, disconnecting any client requesting faction sync!). Add `c2s_playerapplyfactiondata = 0x55` and `table[0x55] = 1`.

2. **`s2c_playerfactiondata` (`0x7B`, Fixed `8` Bytes)**:
   - Packed 8-byte layout (`TagPlayerFactionDataSync.cs`):
     - `[0]` `byte ProtocolType = 0x7B`
     - `[1]` `byte btCamp` (`BaseCamp`: `0..4`)
     - `[2]` `byte btCurFaction` (`CurrentFactionId`: `0..9`, or `0xFF` when unaffiliated/graduated)
     - `[3]` `byte btFirstFaction` (`FirstFactionId`: `0..9`, or `0xFF` when never joined)
     - `[4..7]` `int32 nAddTimes` (little-endian `FactionJoinCount`)
   - Sent to the owning client during initial world sync (right after attributes/skills), on `0x55` `c2s_playerapplyfactiondata`, and immediately on sect induction or graduation.

3. **`s2c_npcchgcamp` (`0x59`, Fixed `6` Bytes) & `s2c_npcchangecurcamp` (`0x58`, Fixed `6` Bytes)**:
   - Packed 6-byte camp sync (`0x59` updates permanent `BaseCamp` on client, `0x58` updates active `CurrentCamp` on client):
     - `[0]` `byte ProtocolType` (`0x59` for base camp, `0x58` for current camp)
     - `[1..4]` `uint32 Id` (player's world `NpcId`, little-endian)
     - `[5]` `byte Camp` (`EntityCamp` `0..4`)
   - **Adapter Naming Fix**: `ProtocolOpcodes.cs:126` in `jx-server` had a typo naming `0x59` `s2c_npcchgregion` instead of `s2c_npcchgcamp` (`0x58` = `s2c_npcchangecurcamp`, `0x59` = `s2c_npcchgcamp`, `0x5A` = `s2c_npcchgregion`). Rename/alias `0x59` to `s2c_npcchgcamp` and broadcast both `0x59` and `0x58` to the 3x3 AOI when a player joins or graduates from a sect.

4. **CRITICAL BUG FIX — `s2c_playerskilllevel` (`0x5E`, Fixed `15` Bytes)**:
   - Packed 15-byte layout (`TagPlayerSkillLevelSync.cs` in `jx-swarm`):
     - `[0]` `byte ProtocolType = 0x5E`
     - `[1..4]` `int32 nSkillID`
     - `[5..8]` `int32 nSkillLevel` (effective client level; `BaseLevel + BonusLevel`)
     - `[9..12]` `int32 nLeavePoint` (`UnallocatedSkillPoints`)
     - `[13..14]` `uint16 wSkillExpPercent` (`0..100`)
   - **Bug Found in `jx-server`**: `src/JXReborn.Adapter.Legacy/Packets/PlayerSkillLevelS2CPacket.cs` (lines 25–39) wrote `ushort ExpPercent` at `[9..10]` and `int UnallocatedSkillPoints` at `[11..14]`, swapping the last two fields! Fixing `PlayerSkillLevelS2CPacket.cs` to `[9..12] int32 UnallocatedSkillPoints` and `[13..14] ushort SkillExpPercent` restores 100% byte-exact compatibility with `game.exe` and `jx-swarm`.

---

## 4. Advanced Sect Books, Scriptures & `WashPoints` Rules

### Ground-Truth Sources
- `legacy-server/server1/script/item/skillbook.lua` (lines 18–48, 139–167)
- `legacy-server/server1/script/task/lv120skill/item/lv120skillbook.lua` (lines 1–29) & `script/task/lv120skill/head.lua` (lines 6, 19, 116–128)
- `legacy-server/server1/script/item/wulin-miji.lua` (lines 1–27)
- `legacy-server/server1/script/item/xisui-jing.lua` (lines 1–39)
- `legacy-server/server1/script/global/特殊用地/梦境/npc/路人_叛僧.lua` (lines 106–148)

### Verified Mechanics
1. **28 Level 90 Sect Skill Books (`Genre = 6, DetailType = 1, ParticularType in 27..59` excluding `29..32, 44`)**:
   - Prerequisites (`skillbook.lua:139-167`): `player.Level >= 80`, `player.EffectiveFactionId == book.NeedFaction`, and `!player.HasLearnedSkill(book.SkillId)`.
   - **23 Proficiency Books (`IsExpSkill == true`, granted at `BaseLevel = 1, SkillExp = 0`)**:
     - Thiếu Lâm (`0`): `56 -> 318` (*Đạt Ma Độ Giang*), `57 -> 319` (*Hoành Tảo Thiên Quân*), `58 -> 321` (*Vô Tướng Trảm*)
     - Thiên Vương (`1`): `37 -> 325` (*Truy Phong Quyết*), `38 -> 323` (*Truy Tinh Trục Nguyệt*), `39 -> 322` (*Phá Thiên Trảm*)
     - Đường Môn (`2`): `27 -> 302` (*Bạo Vũ Lê Hoa*), `45 -> 339` (*Nhiếp Hồn Nguyệt Ảnh*), `46 -> 342` (*Cửu Cung Phi Tinh*)
     - Ngũ Độc (`3`): `47 -> 353` (*Âm Phong Thực Cốt*), `48 -> 355` (*Huyền Âm Trảm*)
     - Nga My (`4`): `42 -> 328` (*Tam Nga Tề Tuyết*), `43 -> 380` (*Phong Sương Toái Ảnh*)
     - Thúy Yên (`5`): `40 -> 336` (*Băng Tung Vô Ảnh*), `41 -> 337` (*Băng Tâm Tiên Tử*)
     - Cái Bang (`6`): `54 -> 357` (*Phi Long Tại Thiên*), `55 -> 359` (*Thiên Hạ Vô Cẩu*)
     - Thiên Nhẫn (`7`): `35 -> 361` (*Vân Long Kích*), `36 -> 362` (*Thiên Ngoại Lưu Tinh*)
     - Võ Đang (`8`): `33 -> 365` (*Thiên Địa Vô Cực*), `34 -> 368` (*Nhân Kiếm Hợp Nhất*)
     - Côn Lôn (`9`): `50 -> 372` (*Ngạo Tuyết Tiêu Phong*), `51 -> 375` (*Lôi Động Cửu Thiên*)
   - **5 Point-Allocated Support/Curse Books (`IsExpSkill == false`, granted at `BaseLevel = 0, SkillExp = 0`)**:
     - `28 -> 351` (Đường Môn `2`: *Loạn Hoàn Kích*)
     - `49 -> 390` (Ngũ Độc `3`: *Đoạn Cân Hủ Cốt*)
     - `52 -> 394` (Côn Lôn `9`: *Túy Tiên Tá Cốt*)
     - `53 -> 391` (Thiên Nhẫn `7`: *Nhiếp Hồn Loạn Tâm*)
     - `59 -> 332` (Nga My `4`: *Phổ Độ Chúng Sinh*)

2. **Level 120 Sect Skill Book (`Genre = 6, DetailType = 1, ParticularType = 1125`)**:
   - Prerequisites (`lv120skillbook.lua` + `head.lua:116-128`): `EffectiveFactionId in 0..9`, `player.Level >= 120`, `player.GetTaskValue(2463) == 0`, `!player.HasLearnedSkill(skillId)`.
   - Notice the canonical ID order in `LV120_SKILL_ID` (`head.lua:19`): **Faction `0` (Thiếu Lâm) maps to `709` (*Đại Thừa Như Lai Chú*), while Faction `1` (Thiên Vương) maps to `708` (*Đảo Hư Thiên*)**; Factions `2..9` map sequentially to `710..717` (`2->710` *Mê Ảnh Tung*, `3->711` *Hấp Tinh Yểm*, `4->712` *Bế Nguyệt Phật Trần*, `5->713` *Ngự Tuyết Ẩn*, `6->714` *Hỗn Thiên Khí Công*, `7->715` *Ma Âm Phệ Phách*, `8->716` *Xuất Ứ Bất Nhiễm*, `9->717` *Lưỡng Nghi Chân Khí*).
   - Grants at `BaseLevel = 1, SkillExp = 0` (`IsExpSkill == true`) and sets `Task[2463] = skillId`.

3. ***Võ Lâm Mật Tịch* (`6, 1, 26`) & *Tẩy Tủy Kinh* (`6, 1, 22`)**:
   - **Bit-packed Task Counters (`wulin-miji.lua:10-24`, `xisui-jing.lua:11-36`)**: Both scripts pack the book usage count (`0..15`) into the **low nibble (`bits 0..3`, `& 0xF`)** of their respective task IDs (`Task[80]` for *Võ Lâm Mật Tịch*, `Task[81]` for *Tẩy Tủy Kinh*) while preserving upper bytes (`SetByte(val, 1, SetBit(...))` / `(taskVal & ~0xF) | (times + 1)`).
   - *Võ Lâm Mật Tịch* (`6, 1, 26`): Requires `Level >= 80` and `(Task[80] & 0xF) < 15`; consumes `1` item, adds `+1` `UnallocatedSkillPoints`, updates `Task[80]`.
   - *Tẩy Tủy Kinh* (`6, 1, 22`): Legacy `xisui-jing.lua:20-25` only granted `+5` attribute points when `GetLevel() >= 87`, causing characters at Levels `80..86` to consume the book and lose `+5` points forever! Per Session 2026-11-11 user-approved decision, `XiSuiJingItemScript` requires `Level >= 80` and `(Task[81] & 0xF) < 15`, consumes `1` item, and **unconditionally grants `+5` `UnallocatedAttributePoints`** (`XiSuiJingBonusPointsPerBook = 5`, max `+75`).

4. **Attribute & Skill Point Washing (`WashPoints`, `MinMarrowWashLevel = 50`, `路人_叛僧.lua:106-148`)**:
   - **Attribute Wash (`WashAttributes`)**:
     - Reads starter baseline `(baseStr, baseDex, baseVit, baseEng)` from `PlayerTemplateCatalog` (`Series`, `Gender`), plus optional quest single-stat bonus bytes packed in `Task[88]` (`byte 0 = Str`, `byte 1 = Dex`, `byte 2 = Vit`, `byte 3 = Eng`, matching `GetByte(GetTask(88), 1..4)` in `路人_叛僧.lua:142-145`).
     - For each attribute where `currentBase > minBase`, refunds `delta = currentBase - minBase` into `UnallocatedAttributePoints` and sets `currentBase = minBase`.
     - Because `UnallocatedAttributePoints` already holds any unspent level-up and *Tẩy Tủy Kinh* points and `currentBase - minBase` holds every spent point (whether sourced from level-ups or *Tẩy Tủy Kinh*), `newUnallocated = oldUnallocated + sum(deltas)` preserves `100%` of *Tẩy Tủy Kinh* bonus points automatically!
     - Re-evaluates equipment requirement activation (`Strength`/`Dexterity`), recomputes `MaxLife`, `MaxMana`, `MaxStamina`, clamps `CurrentLife = Math.Clamp(CurrentLife, 1, MaxLife)`, `CurrentMana = Math.Clamp(CurrentMana, 0, MaxMana)`, `CurrentStamina = Math.Clamp(CurrentStamina, 0, MaxStamina)`, and sends `4x` `0x5D` (`s2c_playerattributesync`) + `0x46` (`PlayerLevelUpSync` / vitals refresh).
   - **Skill Wash (`WashSkills`)**:
     - Protected non-refundable skill IDs: starter melee/weapon skills (`1, 2, 53, 229, 230, 231, 232`) and utility skills (`210` *Khinh Công*, `400` *Kiếp Phú Tế Bần*), plus any skill whose `SkillTemplate.IsExpSkill == true` (all 23 proficiency 90x skills and all 10 120x skills).
     - Every other skill in `player.LearnedSkills` (`!tpl.IsExpSkill`, including all base sect skills `10x..60x` and the 5 non-exp 90x skills `332, 351, 390, 391, 394`) with `BaseLevel > 0` adds `slot.BaseLevel` to `player.UnallocatedSkillPoints` and resets to `BaseLevel = 0, SkillExp = 0`.
     - If `player.ActiveAuraSkillId` was among the reset skills, sets `player.ActiveAuraSkillId = 0`.
     - Recomputes passive skill modifiers and sends full `0x45` (`s2c_synccurplayerskill`) + `0x5E` (`s2c_playerskilllevel` for updated `UnallocatedSkillPoints`) so client F5 skill tree updates cleanly in one frame.
