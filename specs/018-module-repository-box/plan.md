# Implementation Plan: Repository Box, Secondary Password & Revival Binding

**Branch**: `018-module-repository-box` | **Date**: 2026-11-15 | **Spec**: [spec.md](spec.md)

## Summary

Deliver the **018-module-repository-box** specification for `JXReborn` as an isolated plug-in module:
1. **Plug-in Architecture (`RepositoryBoxZoneModule : IZoneModule`)**: Registers storage commands (deposit/withdraw money, box PIN operations, revival point binding), touching 0 lines of `MapZoneWorker.cs`.
2. **Repository Grid (`PlayerInventoryGrid`)**: Extends inventory management with `_repositoryRoomGrid[6, 10]` for `ItemContainerPlace.RepositoryRoom = 4`.
3. **Atomic Money Transfer**: Implements atomic silver exchange between bag and repository with negative and overflow bounds checking.
4. **Secondary PIN Protection**: Implements sub-opcodes 0..4 for setting, unlocking, locking, changing, and canceling storage passwords using MD5 hashing.
5. **Revival Point Binding**: Updates character respawn coordinates.

## Technical Context

- Platform: .NET 10 (`net10.0`), C# 14.
- Target Assemblies: `JXReborn.Primitives`, `JXReborn.Domain`, `JXReborn.ZoneEngine`, `JXReborn.Adapter.Legacy`.
- Tests: `JXReborn.Protocol.Tests`, `JXReborn.ZoneEngine.Tests`, `JXReborn.Integration.Tests`, `JXReborn.ClientDriver.Tests`.
