# Data Model: 8,000 Dã Tẩu Quest Engine (Spec 025)

## Entities & Enums

### 1. Enums (`src/JXReborn.Primitives/Quests/`)
- `DaTauQuestType`:
  - `BuyGoods = 1`: Purchase/deliver shop goods
  - `FindGoods = 2`: Green equipment delivery with magic affix matching
  - `ShowGoods = 3`: Show equipment without consuming
  - `FindMaps = 4`: Regional map / Đồ Chí delivery
  - `UpGround = 5`: Progress Delta (EXP, Repute, FuYuan, PK, SongJin)
  - `WorldMaps = 6`: Sơn Hà Xã Tắc delivery
  - `KillMonsters = 7`: Defeat designated monsters in target map
- `DaTauNumericType`:
  - `Exp = 2`
  - `Repute = 3`
  - `FuYuan = 4`
  - `PkValue = 5`
  - `BattleScore = 6`

### 2. State Models (`src/JXReborn.Domain/Quests/`)
- `DaTauQuestState`:
  - `int TotalCompletedCount`: 0..8000 absolute streak count.
  - `int CurrentTaskTimes`: 0..20 tasks in current link.
  - `int CurrentTaskLinks`: 0..20 links in current loop.
  - `int CurrentTaskLoops`: 0..20 loops completed.
  - `int CancelChancesRemaining`: Saved cancellation privileges.
  - `int DailyStreak`: Consecutive tasks completed today (10, 20, 30, 40).
  - `int DailyCompletedCount`: Total tasks completed today.
  - `int DailyFastCompletedCount`: Fast completions used today (max 30).
  - `ActiveDaTauQuest? ActiveQuest`: Currently tracked quest details.
  - `PendingRewardChoices? PendingReward`: 3-way `Prise` options pending player selection.

### 3. Catalogs (`src/JXReborn.GameAssets/Quests/DaTau/`)
- `TreaPosEntry`:
  - `int RowId`, `string MapName`, `int MapId`, `int WorldX`, `int WorldY`, `int MapX`, `int MapY`, `string Description`, `string SprPath`.
- `MilestoneAwardEntry`:
  - `int MilestoneStep`, `int Genre`, `int Detail`, `int Particular`, `int Level`, `int Quality`.
- `BasicAwardEntry`:
  - `int AwardId`, `int AwardType`, `string Name`, `int Value1`, `int Value2`.
