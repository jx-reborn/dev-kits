# Quickstart: Great Golden World Bosses (Spec 026)

## Spawning a Boss via Zone Command

```csharp
var spawnCmd = new SpawnWorldBossZoneCommand(WorldBossId.CoBach, 50304, 103168);
await worker.TryEnqueueCommand(spawnCmd);
```

## Attacking & Damage Attribution

```csharp
var attackCmd = new AttackWorldBossZoneCommand(player.CharacterId, player.LeaseEpoch, bossNpcId, damage: 15000);
await worker.TryEnqueueCommand(attackCmd);
```

## Resolving Drops

When the boss reaches 0 HP, `WorldBossDamageAttributionTickSystem` invokes `session.EvaluateTopDamager(...)` and `WorldBossDropEvaluator.SpawnBossDrops(...)` to distribute Archimedean spiral loot with 30s protection.

## Live Differential Parity Verification (Journey `J16`)
```powershell
.\tools\run-parity.ps1 -Profile kit -Filter "DifferentialParityEndgameMissionTests"
```
