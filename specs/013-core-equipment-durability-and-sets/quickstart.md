# Quickstart & Verification Guide: Core Equipment Durability, Golden Gear & Set Bonuses

**Feature Branch**: `013-core-equipment-durability-and-sets`  
**Date**: 2026-11-11  
**Spec**: [spec.md](spec.md) | **Plan**: [plan.md](plan.md)

---

## 1. Prerequisites

- **.NET 10 SDK** (`net10.0`, C# 14)
- **Repository Root**: repository root

---

## 2. Recompile Tier 2 Binary Snapshot (`artifacts/jxreborn-data.pack`) & Zero-Warning Build

After implementing the updated `MemoryPack` templates (`GoldenFixedAffixBindingTemplate`, `EquipmentSuiteBindingTemplate`, `EquipmentSetTemplate`, `ItemTemplate`, `GameAssetsSnapshotBundle`), rebuild the solution and regenerate `artifacts/jxreborn-data.pack` so Tier 2 SHA-256 snapshot loading includes all Golden fixed affixes and set definitions:

```powershell
dotnet build JXReborn.slnx -warnaserror
dotnet run --project tools/JXReborn.DataExporter -- compile --data data --artifacts artifacts
```

---

## 3. Three-Tier Automated Verification Suite

### Tier 1 — Symmetric Wire Protocol Unit Gate (`0x9B` & Golden/Indestructible `0x5F`)

Verifies exact 13-byte `ItemDurabilityChangeS2CPacket` (`0x9B`, `<BIii`) serialization/parsing across abrasion, break (`0`), and Cheap Golden repair vectors, plus 80-byte `SyncItemS2CPacket` (`0x5F`) encoding `Pattern = 0x10` and `DetailType = (short)(GoldRowId - 1)` for Golden items and `-1` (`0xFFFF`) durability for indestructible items:

```powershell
dotnet test tests/JXReborn.Protocol.Tests --filter "FullyQualifiedName~EquipmentDurabilityAndGoldenPacketTests" --no-build
```

### Tier 2 — Deterministic `ZoneSimulationHarness` Engine Gate

Runs `EquipmentDurabilityAndGoldenSetTests` inside `ZoneSimulationHarness` with deterministic PRNG seeds and zero sockets:
1. **Combat & Movement Abrasion (`US1`)**: Asserts `1/256` weapon attack wear (`FightMode != 0` only), `1/320` helm/body/belt/cuff defense wear (`Foot == 0` on defend!), `1/2560` boots and `1/5120` horse wear on region crossing (`Mask == 0`), indestructible immunity (`MaxDurability == 0` and `indestructible_b` `AttributeId == 38`), and immediate `SendItemDurabilitySync` notifications.
2. **Broken-in-Slot Stat Suspension, Vital Clamping & Repair (`US2`)**: Wears down equipped life-boosting armor from `1 -> 0` durability with a completely full `6x10` `EquipRoom`; asserts the armor stays in `Equip` marked `IsBroken = true`, suspends 100% of base/magic/Five-Elements bonuses, clamps `CurrentLife` to the lowered `EffectiveMaxLife`, blocks re-equipping if moved to bag, and restores all stats upon `RepairEquipmentItemZoneCommand` (Normal/Blue, Golden Cheap `MaxDurability` reduction, and Golden Costly).
3. **Golden Generation & Fixed Affix Bounds (`US3`)**: Generates *Mộng Long* Golden pieces via `ItemGenerator.GenerateGoldEquipment`, asserting `GoldRowId`, explicit (`0, 2, 4`) vs hidden (`1, 3, 5`) affix bounds, and Five-Elements single-piece activation.
4. **Classic & Extended Multi-Piece Set Bonuses (`US4`)**: Equips partial vs full Classic Golden Sets (`suiteId 1..82` and `83+`) and Extended Sets (`extSuiteId` partial `3`-piece vs full `5`-piece), asserts duplicate ring/piece non-stacking, breaks 1 piece to `0` durability to assert immediate set bonus deactivation, and repairs it to assert immediate reactivation.

```powershell
dotnet test tests/JXReborn.ZoneEngine.Tests --filter "FullyQualifiedName~EquipmentDurabilityAndGoldenSetTests" --no-build
```

### Tier 3 — End-to-End Live TCP & EF Core Persistence Integration Gate

Connects headless encrypted legacy 6.0 client sessions over live TCP (`GameServerClientSession`) backed by EF Core SQLite:
- Verifies encrypted `0x5F` Golden item sync (`WireDetailType == GoldRowId - 1`) on world entry, live `0x9B` durability frames during combat, `RepairEquipmentItemZoneCommand` `0x68` + `0x9B` wire sequence, and full logout/re-login persistence of `GoldRowId`, `CurrentDurability`, `MaxDurability`, and `IsBroken`.

```powershell
dotnet test tests/JXReborn.Integration.Tests --filter "FullyQualifiedName~EquipmentDurabilityAndSetIntegrationTests" --no-build
```

---

## 4. Full Regression Verification

Run the entire solution test suite to confirm zero regressions across domains `001`–`016`:

```powershell
dotnet test JXReborn.slnx --no-build
```
