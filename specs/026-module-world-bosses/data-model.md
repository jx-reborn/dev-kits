# Data Model: Great Golden World Bosses (Spec 026)

## Enums & Types

- `WorldBossId : ushort`: Identifies the 11 canonical bosses.
- `BossDropOwnershipKind : byte`: `None = 0`, `Individual = 1`, `Party = 2`.

## Records & Classes

### `BossTemplate`
- `WorldBossId Id`
- `string Name`
- `ElementSeries Series`
- `int Level`
- `int MaxLife`
- `int AttackRating`
- `int Defense`
- `int PhysicalDamageBase`
- `int ElementalDamageBase`
- `string AuraSkillName`
- `int AuraSkillLevel`
- `string PassiveSkillName`
- `int PassiveSkillLevel`
- `ushort DefaultMapId`
- `int DefaultMpsX`
- `int DefaultMpsY`
- `string DropTableCode`

### `WorldBossAttributionSession`
- `uint BossNpcId`
- `BossTemplate Template`
- `NpcZoneState BossNpc`
- `long SpawnTick`
- `Dictionary<Guid, long> DamageByCharacterId`
- `Dictionary<Guid, string> RoleNamesByCharacterId`
- `bool EvaluateTopDamager(...)`
