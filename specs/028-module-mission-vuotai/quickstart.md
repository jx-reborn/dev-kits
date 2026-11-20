# Quickstart: Vượt Ải Dungeon (Spec 028)

## Party Leader Entering the Challenge

```csharp
var enterCmd = new EnterVuotAiZoneCommand(leader.CharacterId, leader.LeaseEpoch);
await worker.TryEnqueueCommand(enterCmd);
```

## Progressing Dungeon Waves

```csharp
var progressCmd = new ProgressVuotAiWaveZoneCommand(leader.CharacterId, MonsterDefeatCount: 15);
await worker.TryEnqueueCommand(progressCmd);
```

## Claiming Milestone Clear Chest

```csharp
var claimCmd = new ClaimVuotAiChestZoneCommand(player.CharacterId, player.LeaseEpoch);
await worker.TryEnqueueCommand(claimCmd);
```

## Live Differential Parity Verification (Journey `J16`)
```powershell
.\tools\run-parity.ps1 -Profile kit -Filter "DifferentialParityEndgameMissionTests"
```
