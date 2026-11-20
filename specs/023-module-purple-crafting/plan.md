# Implementation Plan: Purple Equipment Crafting & Enchasing (Spec 023)

**Branch**: `023-module-purple-crafting` | **Date**: 2026-11-15 | **Spec**: [spec.md](spec.md)

## Summary

Deliver the **023-module-purple-crafting** specification for `JXReborn` as an isolated plug-in zone module:
1. **Plug-in Architecture (`PurpleCraftingZoneModule : IZoneModule`)**: Registers crafting command handlers and execution contexts without altering `MapZoneWorker.cs`.
2. **Foundry Crafting & Enchasing Commands**: `FoundryCraftZoneCommand` and `EnchaseItemZoneCommand` executing on the authoritative 18-Hz simulation worker thread.
3. **Engines & Calculations**:
   - `XuanJingCompoundEngine`: 1,000 silver jewelry to XuanJing stone conversion.
   - `OreUpgradeEngine`: 3 stones of level $L$ to level $L+1$ with failure item preservation.
   - `MagicDistillEngine`: Extracts affix into Magic Ore 200..205 with series lock on even sockets.
   - `EquipForgeEngine`: Forges blank purple equipment base.
   - `EquipEnchaseEngine`: 6-socket sequential enchasing with `EnchaseLevel` increment, duplicate affix checks, and gear preservation on failure.
   - `ItemValueProbabilityCalculator`: Exact mathematical implementation of `_transItemValImpl_2`.
4. **Wire Packets & Handlers**:
   - Packets: `FoundryRequestC2SPacket` (`0x92`, 204B), `EnchaserItemC2SPacket` (`0x7F`, 50B), `FoundryResultS2CPacket` (`0xA0`, 7B).
   - Handlers: `FoundryRequestPacketHandler` and `EnchaserItemPacketHandler`.
   - Outbound sinks: `ICraftingOutboundSink` implemented on `GameServerClientSession.CraftingSink.cs`.

## Technical Context

- Platform: .NET 9 / 10 (`net10.0`), C# 14.
- Target Assemblies: `JXReborn.Primitives`, `JXReborn.Contracts`, `JXReborn.GameAssets`, `JXReborn.Domain`, `JXReborn.ZoneEngine`, `JXReborn.Adapter.Legacy`.
- Tests: `JXReborn.ZoneEngine.Tests`, `JXReborn.Protocol.Tests`, `JXReborn.ClientDriver.Tests`.
