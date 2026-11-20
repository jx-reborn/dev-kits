# Implementation Plan: Song-Jin Battleground (Spec 027)

**Branch**: `027-module-battleground-songjin` | **Date**: 2026-11-15 | **Spec**: [spec.md](spec.md)

## Summary

Deliver the **027-module-battleground-songjin** specification for `JXReborn` as an isolated plug-in zone module:
1. **Plug-in Architecture (`SongJinZoneModule : IZoneModule`)**: Registers `SongJinCommandHandler` and `PlayerSongJinSessionSlot` on `IZoneExtensionBuilder`. Zero changes to `MapZoneWorker.cs`.
2. **Authoritative Mission Controller (`SongJinMissionController : IMissionController`)**: Drives 5-state lifecycle, camp balancing, rank-scaled scoring, kill streaks, and 20s announcement sync.
3. **Session State (`PlayerSongJinSessionSlot : IPlayerFeatureSlot`)**: Stores per-player camp, battle status, and score context.
4. **Wire Packets & Handlers**:
   - `SongJinScoreboardS2CPacket` (0xA1), `SongJinStatusSyncS2CPacket` (0xA0).
   - `ScriptProtocolPacketHandler` (0xAB) routing for operation 27.

## Technical Context

- Platform: .NET 9 / 10 (`net10.0`), C# 14.
- Target Assemblies: `JXReborn.Primitives`, `JXReborn.Contracts`, `JXReborn.ZoneEngine`, `JXReborn.Adapter.Legacy`.
- Tests: `JXReborn.ZoneEngine.Tests`, `JXReborn.Protocol.Tests`, `JXReborn.Integration.Tests`.
