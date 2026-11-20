# Quickstart: Multi-Channel Chat Channels (Golden Reference Module)

**Feature Branch**: `016-module-chat-channels`  
**Date**: 2026-11-11  

---

## 1. Prerequisites

- .NET 10 SDK (`net10.0`)
- Working directory: repository root

---

## 2. Build Verification

Build the entire solution with zero warnings:

```powershell
dotnet build JXReborn.slnx -warnaserror
```

---

## 3. Running Unit and Integration Tests

### 3.1 Chat Wire Packet Serialization & Transcoding Tests
Verifies byte layouts and TCVN3 $\leftrightarrow$ UTF-8 transcoding for `0x60`, `0x52`, `0x5B`, `0x56`, `0x7E`, and `0xFB`:

```powershell
dotnet test tests/JXReborn.Protocol.Tests --filter "FullyQualifiedName~ChatPacketTests" --no-restore
```

### 3.2 Modular Zone Deconstruction & Plug-in Tests
Verifies `ChatZoneModule` dynamic registration through `IZoneExtensionBuilder` into `ZoneExtensionRegistry` without core loop modifications:

```powershell
dotnet test tests/JXReborn.ZoneEngine.Tests --filter "FullyQualifiedName~ModularZoneDeconstructionTests" --no-restore
```

### 3.3 End-to-End Multi-Channel Chat Integration Tests
Verifies spatial nearby talk, world cross-shard broadcast, private whisper routing with echo, and subscription mask muting:

```powershell
dotnet test tests/JXReborn.Integration.Tests --filter "FullyQualifiedName~MultiChannelChatIntegrationTests" --no-restore
```

### 3.4 Live Zero-Sim Containerized Differential Parity Gate (Journey `J08`)
Verifies Nearby (`channel=2`), Whisper (`channel=3` via `g_HashString2Id`), Wudang Faction (`channel=5`), `TCVN3` + `TEncodeText` color-tag fidelity, and `0x60` (`g_dwChannelOr`) channel bitmask filtering on `jx-runtime-standalone`:

```powershell
.\tools\run-parity.ps1 -Profile minimal -Filter "DifferentialParityChatChannelTests"
```

---

## 4. Key Verification Checklist

- [x] ChatZoneModule registers all 4 chat command handlers cleanly into ZoneExtensionRegistry.
- [x] MapZoneWorker.cs has zero hardcoded references to chat commands.
- [x] Nearby spatial talk broadcasts strictly to observers within the 3x3 region AOI.
- [x] World chat broadcasts across active zone shards via the cluster router.
- [x] Private Whisper routes directly to the designated recipient and echoes back to the sender.
- [x] Muting a channel via `c2s_chatsetchannel` suppresses delivery of messages on that channel.
- [x] System notices broadcast with official `0xFB` header and `"HÖ thèng"` prefix.
