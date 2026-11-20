# Implementation Plan: Personal City Stalls & Atomic Market Buyout

**Branch**: `022-module-player-stalls` | **Date**: 2026-11-15 | **Spec**: [spec.md](spec.md)

## Summary

Deliver the **022-module-player-stalls** specification for `JXReborn` as an isolated plug-in module:
1. **Plug-in Architecture (`PlayerStallZoneModule : IZoneModule`)**: Registers stall commands (mark price, make adv, buy item, exit, query adv, query catalog, query tax rate), touching 0 lines of `MapZoneWorker.cs`.
2. **Session Slot (`PlayerStallSessionSlot`)**: Manages active stall status, banner, and priced items on `PlayerZoneState`.
3. **AOI Perception (`0x76`)**: Broadcasts stall posture (`PLAYER_MENU_STATE_STALL` = 5) and banner string to 3x3 AOI neighbors.
4. **Market Buyout & Tax Deduction**: Implements atomic buyout calculating city tax (10% Cities, 6% Villages, 3% Sects) via `IStallTaxCatalog`.
5. **Wire Packets & Handlers**: Implements `0x7C` C2S and `0x9D` S2C.

## Technical Context

- Platform: .NET 10 (`net10.0`), C# 14.
- Target Assemblies: `JXReborn.Primitives`, `JXReborn.GameAssets`, `JXReborn.ZoneEngine`, `JXReborn.Adapter.Legacy`.
- Tests: `JXReborn.Protocol.Tests`, `JXReborn.ZoneEngine.Tests`, `JXReborn.Integration.Tests`, `JXReborn.ClientDriver.Tests`.
