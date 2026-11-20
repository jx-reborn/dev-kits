# Quickstart: Song-Jin Battleground (Spec 027)

## Registering for a Camp

```csharp
var regCmd = new RegisterSongJinZoneCommand(player.CharacterId, player.LeaseEpoch, SongJinCamp.Song);
await worker.TryEnqueueCommand(regCmd);
```

## Recording Combat Kills & Streaks

```csharp
var killCmd = new RecordSongJinCombatKillZoneCommand(killer.CharacterId, victim.CharacterId);
await worker.TryEnqueueCommand(killCmd);
```

## Exchanging BattleScore at Quartermaster

```csharp
var exchangeCmd = new ExchangeBattleScoreZoneCommand(
    player.CharacterId,
    player.LeaseEpoch,
    PointsToSpend: 1500,
    ItemDetail: 0,
    ItemParticular: 1);
await worker.TryEnqueueCommand(exchangeCmd);
```

## Live Differential Parity Verification (Journey `J16`)
```powershell
.\tools\run-parity.ps1 -Profile kit -Filter "DifferentialParityEndgameMissionTests"
```
