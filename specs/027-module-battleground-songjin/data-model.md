# Data Model: Song-Jin Battleground (Spec 027)

## Enums

- `SongJinPhase : byte`: `Registration = 0`, `Battle = 1`, `Settlement = 2`, `Cooldown = 3`, `Idle = 4`.
- `SongJinCamp : byte`: `None = 0`, `Song = 1`, `Jin = 2`.
- `SongJinMilitaryRank : byte`: `BinhSi = 1`, `HieuUy = 2`, `ThongLanh = 3`, `PhoTuong = 4`, `DaiTuong = 5`.

## Records & Classes

### `SongJinParticipantState`
- `Guid CharacterId`
- `string RoleName`
- `SongJinCamp Camp`
- `int Score`
- `int Kills`
- `int Deaths`
- `int NpcKills`
- `int KillStreak`
- `SongJinMilitaryRank MilitaryRank`
- `SongJinParticipantScoreDto ToDto()`

### `SongJinScoreboardDto`
- `SongJinPhase Phase`
- `int RemainingSeconds`
- `int SongTotalScore`
- `int JinTotalScore`
- `int SongPlayerCount`
- `int JinPlayerCount`
- `IReadOnlyList<SongJinParticipantScoreDto> Top10Participants`

### `PlayerSongJinSessionSlot : IPlayerFeatureSlot`
- `SongJinCamp CurrentCamp`
- `bool IsRegistered`
