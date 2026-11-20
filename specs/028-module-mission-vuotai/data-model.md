# Data Model: Vượt Ải Dungeon (Spec 028)

## Enums

- `VuotAiPhase : byte`: `Registration = 0`, `WaveProgression = 1`, `Wipe = 2`, `Victory = 3`, `Teardown = 4`.

## Records & Classes

### `VuotAiWaveDefinition`
- `int WaveNumber`
- `int MonsterCount`
- `int BossCount`
- `int MonsterLevel`
- `int TimeLimitSeconds`
- `bool IsMilestoneChestWave`
- `int AwardChestCount`

### `VuotAiProgressDto`
- `VuotAiPhase Phase`
- `int CurrentWave`
- `int TotalWaves`
- `int RemainingMonstersInWave`
- `int ElapsedMinutes`
- `int RemainingSeconds`
- `bool IsCompleted`

### `PlayerVuotAiSessionSlot : IPlayerFeatureSlot`
- `bool IsInDungeon`
- `int CurrentWave`
- `long EntranceTick`
