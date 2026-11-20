# Implementation Plan: Multi-Currency NPC Shops & Equipment Repair

**Branch**: `017-module-npc-shops` | **Date**: 2026-11-15 | **Spec**: [spec.md](spec.md)

## Summary

Deliver the **017-module-npc-shops** specification for `JXReborn` as an isolated plug-in module:
1. **Plug-in Architecture (`NpcShopZoneModule : IZoneModule`)**: Registers buy, sell back, and repair commands into `ZoneExtensionRegistry`, touching 0 lines of `MapZoneWorker.cs`.
2. **Session Slot (`PlayerShopSessionSlot`)**: Tracks active open shop, scale, and currency type on `PlayerZoneState`.
3. **Multi-Currency Purchasing**: Deducts currency (Gold/Silver, FuYuan, Repute, Honor, BattleScore, Coin) and places item into bag.
4. **Item Sell-Back**: Scales price by divisor `BUY_SELL_SCALE = 4` (min 1 silver) and removes sold item.
5. **Equipment Repair**: Uses `EquipmentRepairCalculator` to restore durability, handling costly advanced golden repair preserving max durability.

## Technical Context

- Platform: .NET 10 (`net10.0`), C# 14.
- Target Assemblies: `JXReborn.Primitives`, `JXReborn.ZoneEngine`, `JXReborn.Adapter.Legacy`.
- Tests: `JXReborn.Protocol.Tests`, `JXReborn.ZoneEngine.Tests`, `JXReborn.Integration.Tests`, `JXReborn.ClientDriver.Tests`.
