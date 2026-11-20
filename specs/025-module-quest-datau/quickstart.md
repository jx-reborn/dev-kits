# Quickstart: 8,000 Dã Tẩu Quest Engine (Spec 025)

## Overview
This document guides developers on executing and verifying 8,000 Dã Tẩu Quest operations.

## Running Quest Engine Tests
```bash
# Run catalog parser tests (trea_pos.txt, award_link.txt)
dotnet test tests/JXReborn.GameAssets.Tests/ --filter "FullyQualifiedName~DaTau"

# Run quest engine unit and streak tests
dotnet test tests/JXReborn.ZoneEngine.Tests/ --filter "FullyQualifiedName~DaTau"

# Run live Differential Parity Journey J16 via standalone Oracle container
.\tools\run-parity.ps1 -Profile kit -Filter "DifferentialParityEndgameMissionTests"
```

## Basic Workflows

### 1. Accepting a Quest
1. Speak to Dã Tẩu NPC.
2. Server verifies character level $\ge 60$ and daily quota.
3. Server rolls quest category based on level matrix (`tasklink_mainlink.txt`).
4. Generates `ActiveQuest` and synchronizes task target to client.

### 2. Submitting a Quest & 3-Way Reward Selection
1. Player completes requirement (delivers items, slays monsters, or reaches progress delta).
2. Player interacts with NPC to submit.
3. Server presents 3 random reward choices via `s2c_scriptaction` (`0x63` subtype `0x0D`).
4. Player sends choice via `c2s_playerselui` (`0x5F`).
5. Server grants chosen reward, advances streak towards 8,000, and evaluates milestone rewards.

### 3. Cancellation vs. Streak Preservation
1. If player has cancellation token: cancels active quest, consumes 1 token, **streak count is preserved**.
2. If player cancels normally without token: **streak is reset to 0**, 10-minute cooldown penalty enforced.
