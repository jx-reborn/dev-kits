# Quickstart: Purple Equipment Crafting & Enchasing (Spec 023)

## Overview
This document guides developers on executing and verifying Purple Equipment Crafting operations.

## Running Crafting Tests
```bash
# Run all unit tests for Crafting
dotnet test tests/JXReborn.ZoneEngine.Tests/ --filter "FullyQualifiedName~Crafting"

# Run wire packet protocol tests
dotnet test tests/JXReborn.Protocol.Tests/ --filter "FullyQualifiedName~Foundry"

# Run live Differential Parity Journey J15 via standalone Oracle container
.\tools\run-parity.ps1 -Profile kit -Filter "DifferentialParityPurpleCraftingTests"
```

## Basic Workflows

### 1. XuanJing Upgrade
1. Place 3 XuanJing stones of level $L$ in inventory.
2. Send `c2s_foundry_request` (`0x92`) with `UpgradeType = 0` (or `c2s_enchaseritem` `0x7F`).
3. Server verifies 5,000 silver fee and calculates probability.
4. If successful: generates 1 XuanJing stone of level $L+1$.
5. If failed: only stone 1 is removed; stones 2 and 3 remain in inventory.

### 2. Magic Ore Distillation ("Hút Khoáng")
1. Place blue equipment and OrgMine stone (`149..154`) in inventory.
2. For even sockets (2, 4, 6), ensure matching Five-Elements series.
3. Send `c2s_foundry_request` with `UpgradeType = 3`.
4. Produces Magic Ore (`200..205`) retaining affix ID and rolled level.

### 3. Sequential 6-Socket Enchasing
1. Place purple gear base and Magic Ore matching next empty socket in inventory.
2. Ensure even sockets match equipment series and affix is not a duplicate.
3. Send `c2s_foundry_request` with `UpgradeType = 5`.
4. If successful: `EnchaseLevel` increments, durability is preserved.
5. If failed: equipment base is NOT destroyed; only ore and catalysts are lost.
