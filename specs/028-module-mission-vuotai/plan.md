# Implementation Plan: Vượt Ải Dungeon (Spec 028)

**Branch**: `028-module-mission-vuotai` | **Date**: 2026-11-15 | **Spec**: [spec.md](spec.md)

## Summary

Deliver the **028-module-mission-vuotai** specification for `JXReborn` as an isolated plug-in zone module:
1. **Plug-in Architecture (`VuotAiZoneModule : IZoneModule`)**: Registers `VuotAiCommandHandler` and `PlayerVuotAiSessionSlot` on `IZoneExtensionBuilder`. Zero changes to `MapZoneWorker.cs`.
2. **Authoritative Mission Controller (`VuotAiMissionController : IMissionController`)**: Drives party entry, 28 sequential waves, 1-second Wave Timer Board announcements, 18-minute timeout, and milestone clear chests.
3. **Session State (`PlayerVuotAiSessionSlot : IPlayerFeatureSlot`)**: Stores dungeon participation, entrance tick, and current wave index.
4. **Wire Packets & Handlers**:
   - `VuotAiWaveStatusS2CPacket` (0x8C).
   - `ScriptProtocolPacketHandler` (0xAB) routing for operation 28.

## Technical Context

- Platform: .NET 9 / 10 (`net10.0`), C# 14.
- Target Assemblies: `JXReborn.Primitives`, `JXReborn.Contracts`, `JXReborn.ZoneEngine`, `JXReborn.Adapter.Legacy`.
- Tests: `JXReborn.ZoneEngine.Tests`, `JXReborn.Protocol.Tests`, `JXReborn.Integration.Tests`.
