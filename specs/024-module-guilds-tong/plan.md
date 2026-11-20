# Implementation Plan: Guilds/Tong System (Spec 024)

**Branch**: `024-module-guilds-tong` | **Date**: 2026-11-15 | **Spec**: [spec.md](spec.md)

## Summary

Deliver the **024-module-guilds-tong** specification for `JXReborn` as an isolated plug-in zone module:
1. **Plug-in Architecture (`GuildZoneModule : IZoneModule`)**: Registers guild lifecycle commands (create, invite, accept, instate, kick, leave, transfer master, donate money, upkeep tick), touching 0 lines of `MapZoneWorker.cs`.
2. **Session Slot (`PlayerGuildSessionSlot : IPlayerFeatureSlot`)**: Tracks player's guild membership, rank, custom titles, and permissions.
3. **Domain & Persistence**: Aggregate roots `Guild` and `GuildMember`, repository `IGuildRepository`, EF Core mapping in `JXRebornDbContext`, and `IGuildAppService`.
4. **Upkeep System (`GuildTreasuryUpkeepTickSystem`)**: Automated 750s periodic upkeep calculating fee and awarding guild experience.
5. **Channel 6 Chat Filtering**: Enforces strict guild ID matching in `ChatCommandHandler`.
6. **Wire Packets & Handlers**: Multiplexed `0x9A`, `0xA5`, `0xA8`, `0xFD` (C2S) and `0xA0`, `0xC3`, `0xFD` (S2C).

## Technical Context

- Platform: .NET 10 (`net10.0`), C# 14.
- Target Assemblies: `JXReborn.Primitives`, `JXReborn.Contracts`, `JXReborn.Domain`, `JXReborn.Infrastructure`, `JXReborn.Application`, `JXReborn.ZoneEngine`, `JXReborn.Adapter.Legacy`.
- Tests: `JXReborn.Protocol.Tests`, `JXReborn.ZoneEngine.Tests`, `JXReborn.ClientDriver.Tests`.
