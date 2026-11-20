# Implementation Plan: Social Relations (Spec 020)

**Branch**: `020-module-social-relations` | **Date**: 2026-11-15 | **Spec**: [spec.md](spec.md)

## Summary

Deliver the **020-module-social-relations** specification for `JXReborn` as an isolated plug-in module:
1. **Plug-in Architecture (`SocialRelationsZoneModule : IZoneModule`)**: Registers social commands (apply add friend, accept, refuse, delete, redelete, query friend list, query one name, record slaughter enemy, sync presence), touching 0 lines of `MapZoneWorker.cs`.
2. **Session Slot (`PlayerSocialSessionSlot : IPlayerFeatureSlot`)**: Maintains the character's active relations (friends, enemies, blacklist) and checks interaction permissions.
3. **Domain & Persistence**: Entity `CharacterSocialRelation`, repository `ICharacterSocialRelationRepository`, EF Core mapping in `JXRebornDbContext`, and `ISocialAppService`.
4. **Wire Packets & Handlers**: Implements opcodes `0x61`..`0x68` (C2S) and `0x6A`..`0x72` (S2C).

## Technical Context

- Platform: .NET 10 (`net10.0`), C# 14.
- Target Assemblies: `JXReborn.Primitives`, `JXReborn.Contracts`, `JXReborn.Domain`, `JXReborn.Infrastructure`, `JXReborn.Application`, `JXReborn.ZoneEngine`, `JXReborn.Adapter.Legacy`.
- Tests: `JXReborn.Protocol.Tests`, `JXReborn.ZoneEngine.Tests`, `JXReborn.ClientDriver.Tests`.
