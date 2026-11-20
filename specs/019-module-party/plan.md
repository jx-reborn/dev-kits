# Implementation Plan: Party Management (Spec 019)

**Branch**: `019-module-party` | **Date**: 2026-11-15 | **Spec**: [spec.md](spec.md)

## Summary

Deliver the **019-module-party** specification for `JXReborn` as an isolated plug-in zone module:
1. **Plug-in Architecture (`PartyZoneModule : IZoneModule`)**: Registers party lifecycle commands (create, apply, accept, invite, reply, leave, kick, change captain, dismiss, toggle open/close, distribute monster exp), with zero modifications to `MapZoneWorker.cs`.
2. **Session Slot (`PlayerPartySessionSlot : IPlayerFeatureSlot`)**: Manages individual party session state, captain aura reception, and disconnect departure.
3. **Zone Model & Manager (`PartyModel`, `PartyManager`)**: Coordinates party membership roster, leadership level caps, and 768 Mps AOI EXP distribution.
4. **Wire Packets & Handlers**: Multiplexed Opcode `0x53` (C2S) and `0x69` (S2C) with length-minus-one framing.

## Technical Context

- Platform: .NET 10 (`net10.0`), C# 14.
- Target Assemblies: `JXReborn.Primitives`, `JXReborn.ZoneEngine`, `JXReborn.Adapter.Legacy`.
- Tests: `JXReborn.Protocol.Tests`, `JXReborn.ZoneEngine.Tests`, `JXReborn.ClientDriver.Tests`.
