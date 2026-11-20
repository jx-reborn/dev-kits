# Gameplay & Scripting Contracts: 10 Sects, Skill Books, Scriptures & `WashPoints`

**Feature**: `015-core-factions-and-skills`  
**Compatibility Sources**: Legacy 6.0 faction settings (`门派设定.ini`, `factiontitle.txt`) and sect/book/washpoint gameplay rules (`skills_table.lua`, `skillbook.lua`, `lv120skillbook.lua`, `head.lua`, `wulin-miji.lua`, `xisui-jing.lua`, `路人_叛僧.lua`).

---

## 1. Canonical 10 Classic Martial Arts Sects (`0..9`)

Active sects are strictly `0 <= factionId < CharacterConsts.ClassicFactionCount (10)`. Catalog entry `10` (`hoa-son-phai`) remains inactive.

| `Id` | `Code` | Vietnamese Name | `Series` | `Camp` | `AllowedGenders` | `factiontitle.txt` `FACTION` Col | Base Sect `SkillIds` (Granted at `BaseLevel = 0`) |
| :-: | :--- | :--- | :-: | :-: | :-: | :-: | :--- |
| `0` | `thieu-lam-phai` | Thiếu Lâm phái | `0` (Metal) | `1` (Justice) | `[0]` (Male) | `1` | `10, 14, 4, 6, 8, 15, 16, 20, 11, 19, 271, 21, 273` |
| `1` | `thien-vuong-bang` | Thiên Vương Bang | `0` (Metal) | `3` (Balance) | `[0, 1]` | `8` | `29, 30, 34, 23, 24, 26, 33, 31, 35, 37, 40, 42, 32, 36, 41, 324` |
| `2` | `duong-mon` | Đường Môn | `1` (Wood) | `3` (Balance) | `[0, 1]` | `5` | `45, 43, 347, 303, 47, 50, 54, 343, 345, 349, 48, 58, 249, 341` |
| `3` | `ngu-doc-giao` | Ngũ Độc Giáo | `1` (Wood) | `2` (Evil) | `[0, 1]` | `9` | `63, 65, 60, 62, 67, 66, 70, 64, 68, 69, 384, 73, 356, 72, 71, 74, 75` |
| `4` | `nga-my-phai` | Nga My phái | `2` (Water) | `1` (Justice) | `[1]` (Female) | `3` | `80, 85, 77, 79, 93, 82, 89, 385, 86, 92, 88, 91, 252, 282` |
| `5` | `thuy-yen-mon` | Thúy Yên môn | `2` (Water) | `3` (Balance) | `[1]` (Female) | `6` | `99, 102, 95, 97, 269, 105, 113, 100, 109, 108, 111, 114` |
| `6` | `cai-bang` | Cái Bang | `3` (Fire) | `1` (Justice) | `[0, 1]` | `7` | `119, 122, 115, 116, 129, 124, 274, 277, 125, 128, 130, 360` |
| `7` | `thien-nhan-giao` | Thiên Nhẫn Giáo | `3` (Fire) | `2` (Evil) | `[0, 1]` | `10` | `135, 145, 131, 132, 136, 137, 138, 140, 141, 364, 143, 142, 148, 150` |
| `8` | `vo-dang-phai` | Võ Đang phái | `4` (Earth) | `1` (Justice) | `[0, 1]` | `2` | `153, 155, 151, 152, 159, 158, 164, 160, 157, 165, 166, 267` |
| `9` | `con-lon-phai` | Côn Lôn phái | `4` (Earth) | `3` (Balance) | `[0, 1]` | `4` | `169, 179, 167, 168, 171, 392, 174, 172, 173, 178, 393, 175, 181, 90, 176, 182, 275, 630` |

---

## 2. All 28 Level 90 Sect Skill Books (`Genre = 6, DetailType = 1, ParticularType = 27..59`)

**Gates**:
1. `player.Level >= 80` (`CharacterConsts.MinAdvancedBookLevel`)
2. `player.EffectiveFactionId == NeedFaction` (`EffectiveFactionId = CurrentFactionId != 0xFF ? CurrentFactionId : FirstFactionId`)
3. `!player.HasLearnedSkill(SkillId)`

*(Note: `ParticularType` `29, 30, 31, 32, 44` are non-book items in `magicscript.txt` and are NOT Level 90 skill books.)*

| `ParticularType` | `SkillId` | `NeedFaction` | `IniLevel` | `IsExpSkill` | Sect Name | Skill Name |
| :-: | :-: | :-: | :-: | :-: | :--- | :--- |
| `27` | `302` | `2` | `1` | `true` | Đường Môn | Bạo Vũ Lê Hoa |
| `28` | `351` | `2` | **`0`** | **`false`** | Đường Môn | Loạn Hoàn Kích |
| `33` | `365` | `8` | `1` | `true` | Võ Đang | Thiên Địa Vô Cực |
| `34` | `368` | `8` | `1` | `true` | Võ Đang | Nhân Kiếm Hợp Nhất |
| `35` | `361` | `7` | `1` | `true` | Thiên Nhẫn | Vân Long Kích |
| `36` | `362` | `7` | `1` | `true` | Thiên Nhẫn | Thiên Ngoại Lưu Tinh |
| `37` | `325` | `1` | `1` | `true` | Thiên Vương | Truy Phong Quyết |
| `38` | `323` | `1` | `1` | `true` | Thiên Vương | Truy Tinh Trục Nguyệt |
| `39` | `322` | `1` | `1` | `true` | Thiên Vương | Phá Thiên Trảm |
| `40` | `336` | `5` | `1` | `true` | Thúy Yên | Băng Tung Vô Ảnh |
| `41` | `337` | `5` | `1` | `true` | Thúy Yên | Băng Tâm Tiên Tử |
| `42` | `328` | `4` | `1` | `true` | Nga My | Tam Nga Tề Tuyết |
| `43` | `380` | `4` | `1` | `true` | Nga My | Phong Sương Toái Ảnh |
| `45` | `339` | `2` | `1` | `true` | Đường Môn | Nhiếp Hồn Nguyệt Ảnh |
| `46` | `342` | `2` | `1` | `true` | Đường Môn | Cửu Cung Phi Tinh |
| `47` | `353` | `3` | `1` | `true` | Ngũ Độc | Âm Phong Thực Cốt |
| `48` | `355` | `3` | `1` | `true` | Ngũ Độc | Huyền Âm Trảm |
| `49` | `390` | `3` | **`0`** | **`false`** | Ngũ Độc | Đoạn Cân Hủ Cốt |
| `50` | `372` | `9` | `1` | `true` | Côn Lôn | Ngạo Tuyết Tiêu Phong |
| `51` | `375` | `9` | `1` | `true` | Côn Lôn | Lôi Động Cửu Thiên |
| `52` | `394` | `9` | **`0`** | **`false`** | Côn Lôn | Túy Tiên Tá Cốt |
| `53` | `391` | `7` | **`0`** | **`false`** | Thiên Nhẫn | Nhiếp Hồn Loạn Tâm |
| `54` | `357` | `6` | `1` | `true` | Cái Bang | Phi Long Tại Thiên |
| `55` | `359` | `6` | `1` | `true` | Cái Bang | Thiên Hạ Vô Cẩu |
| `56` | `318` | `0` | `1` | `true` | Thiếu Lâm | Đạt Ma Độ Giang |
| `57` | `319` | `0` | `1` | `true` | Thiếu Lâm | Hoành Tảo Thiên Quân |
| `58` | `321` | `0` | `1` | `true` | Thiếu Lâm | Vô Tướng Trảm |
| `59` | `332` | `4` | **`0`** | **`false`** | Nga My | Phổ Độ Chúng Sinh |

---

## 3. Level 120 Sect Skill Book (`Genre = 6, DetailType = 1, ParticularType = 1125`)

**Gates**:
1. `player.EffectiveFactionId < 10` (`0..9`)
2. `player.Level >= 120` (`CharacterConsts.MinSkill120BookLevel`)
3. `player.GetTaskValue(2463) == 0` (`CharacterConsts.Skill120BookTaskId`)
4. `!player.HasLearnedSkill(TargetSkillId)`

**Effect on Success**: Consume `1` item (`6, 1, 1125`), grant `TargetSkillId` at `BaseLevel = 1, SkillExp = 0` (`IsExpSkill = true`), and set `Task[2463] = TargetSkillId`.

| `EffectiveFactionId` | Sect Name | `TargetSkillId` | Skill Name |
| :-: | :--- | :-: | :--- |
| `0` | Thiếu Lâm phái | **`709`** | Đại Thừa Như Lai Chú |
| `1` | Thiên Vương Bang | **`708`** | Đảo Hư Thiên |
| `2` | Đường Môn | `710` | Mê Ảnh Tung |
| `3` | Ngũ Độc Giáo | `711` | Hấp Tinh Yểm |
| `4` | Nga My phái | `712` | Bế Nguyệt Phật Trần |
| `5` | Thúy Yên môn | `713` | Ngự Tuyết Ẩn |
| `6` | Cái Bang | `714` | Hỗn Thiên Khí Công |
| `7` | Thiên Nhẫn Giáo | `715` | Ma Âm Phệ Phách |
| `8` | Võ Đang phái | `716` | Xuất Ứ Bất Nhiễm |
| `9` | Côn Lôn phái | `717` | Lưỡng Nghi Chân Khí |

---

## 4. Permanent Bonus Point Scriptures

| Item (`G, D, P`) | Vietnamese Name | Min Level | Task ID & Counter Mask | Max Uses | Effect per Use |
| :-: | :--- | :-: | :--- | :-: | :--- |
| `6, 1, 26` | *Võ Lâm Mật Tịch* | `80` | `Task[80] & 0xF` → `(val & ~0xF) \| (n + 1)` | `15` | `+1` `UnallocatedSkillPoints` (`+15` max) |
| `6, 1, 22` | *Tẩy Tủy Kinh* | `80` | `Task[81] & 0xF` → `(val & ~0xF) \| (n + 1)` | `15` | Flat **`+5` `UnallocatedAttributePoints`** (`+75` max) |

---

## 5. Skill Allocation & `WashPoints` Decision Matrix

| Skill Category | Skill IDs | Granted `BaseLevel` | Manual Point Allocation (`0x58`) | `WashSkills` Behavior |
| :--- | :--- | :-: | :--- | :--- |
| **Starter Melee / Weapon** | `1, 2, 53, 229, 230, 231, 232` | `1` | **Rejected** (`IsWeaponSkill` / starter lock) | **Preserved** at current level (`0` refunded) |
| **Protected Utility Skills** | `210` (*Khinh Công*), `400` (*Kiếp Phú Tế Bần*) | `1..20` | **Rejected** (quest/script progression only) | **Preserved** at current level (`0` refunded) |
| **Base Sect Skills (`10x..60x` + Passives)** | 142 sect skills in Section 1 | `0` | **Allowed** (`0 -> 1 .. min(MaxLevel, Lv - ReqLv + 1)`) | **Reset to `BaseLevel = 0`**; full `BaseLevel` refunded; aura cleared |
| **90x Non-Exp Support/Curse Books** | `332, 351, 390, 391, 394` | `0` | **Allowed** (`0 -> 1 .. min(MaxLevel, Lv - ReqLv + 1)`) | **Reset to `BaseLevel = 0`**; full `BaseLevel` refunded |
| **90x Combat-Proficiency Books** | 23 proficiency skills (`302..380`) | `1` | **Rejected** (`IsExpSkill == true`) | **Preserved** at current `BaseLevel` & `SkillExp` (`0` refunded) |
| **120x Sect Mastery Books** | `708..717` | `1` | **Rejected** (`IsExpSkill == true`) | **Preserved** at current `BaseLevel` & `SkillExp` (`0` refunded) |

---

## 6. C# 14 Fluent Scripting Context Contracts (`JXReborn.ZoneEngine.Scripting`)

Thin engine-delegating extensions added to Spec `012`'s scripting contexts so `JXReborn.Scripts` implementations contain zero low-level state mutation:

### `IScriptDialogContext` Additions
```csharp
byte CurrentFactionId { get; }
byte FirstFactionId { get; }
byte EffectiveFactionId { get; }
ushort FactionJoinCount { get; }

FactionProgressionResult CanJoinFaction(byte factionId);
FactionProgressionResult TryJoinFaction(byte factionId);
FactionProgressionResult TryGraduateFaction();
FactionProgressionResult TryWashPoints(WashPointsMode mode);
```

### `IScriptItemContext` Additions
```csharp
byte CurrentFactionId { get; }
byte EffectiveFactionId { get; }

bool HasLearnedSkill(int skillId);
FactionProgressionResult TryGrantBookSkill(int skillId, ushort initialBaseLevel);
void AddBonusSkillPoints(int points);
void AddBonusAttributePoints(int points);
```
