# Implementation Plan: Two-Phase Zero-Dupe P2P Player Trade

**Branch**: `021-module-p2p-trade` | **Date**: 2026-11-15 | **Spec**: [spec.md](spec.md)

## Summary

Deliver the **021-module-p2p-trade** specification for `JXReborn` as an isolated plug-in module:
1. **Plug-in Architecture (`P2PTradeZoneModule : IZoneModule`)**: Registers trade commands (apply, reply, move money, decision), touching 0 lines of `MapZoneWorker.cs`.
2. **Session Slot (`PlayerTradeSessionSlot`)**: Manages 2-phase state machine (Application, Placement, Lock, Confirm) and handles disconnect rollback in `OnPlayerDetached`.
3. **Atomic Swap & Lease Fencing**: Implements `ITradeAppService.ExecuteTradeSwapAsync` validating `LeaseEpoch` for both participants and persisting audit records to `TradeAuditLogs`.
4. **Wire Packets & Handlers**: Implements opcodes `0x6B`, `0x6C`, `0x6D`, `0x71`, `0x72`, `0x75`, `0x77`, `0x78`, `0x8E`.

## Technical Context

- Platform: .NET 10 (`net10.0`), C# 14.
- Target Assemblies: `JXReborn.Primitives`, `JXReborn.Contracts`, `JXReborn.Domain`, `JXReborn.Infrastructure`, `JXReborn.Application`, `JXReborn.ZoneEngine`, `JXReborn.Adapter.Legacy`.
- Tests: `JXReborn.Protocol.Tests`, `JXReborn.ZoneEngine.Tests`, `JXReborn.Integration.Tests`, `JXReborn.ClientDriver.Tests`.
