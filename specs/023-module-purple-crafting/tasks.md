# Tasks: Purple Equipment Crafting & Enchasing (Spec 023)

**Feature Branch**: `023-module-purple-crafting`  
**Status**: Completed & Verified  

---

## Task List

- [X] **T01 — Primitives & Contracts** `src/JXReborn.Primitives/Crafting/` & `src/JXReborn.Contracts/Crafting/`
  - [X] Define `EquipUpgradeType.cs` (Cpd=0, UpCryolite=1, UpPropMine=2, DistillMine=3, ForgeEquip=4, EnchaseEquip=5, Atlas=6)
  - [X] Define `FoundryResultType.cs` (Succeed=0, Fail=1, NoMoney=2, LevelFull=3, LackResource=4, LevelError=5, MagicError=6, SeriesError=7, RuleError=8)
  - [X] Define `CraftingItemPositionDto.cs` (ItemContainerPlace Place, byte GridX, byte GridY)

- [X] **T02 — Game Assets & Catalogs** `src/JXReborn.GameAssets/Crafting/`
  - [X] Define `ICraftingValueCatalog.cs` interface
  - [X] Implement `CraftingValueCatalog.cs` with `ore.txt` scale (levels 1..10 values 10k to 1.849b) and `magicattriblevel.txt` parsing
  - [X] Register catalog in DI extensions

- [X] **T03 — Zone Engine Core Crafting Systems** `src/JXReborn.ZoneEngine/Crafting/`
  - [X] Implement `ItemValueProbabilityCalculator.cs` matching `_transItemValImpl_2` algorithm
  - [X] Implement `XuanJingCompoundEngine.cs` (jewelry to XuanJing stone conversion)
  - [X] Implement `OreUpgradeEngine.cs` (3 stones of level L to level L+1, preserving stones 2 & 3 on failure)
  - [X] Implement `MagicDistillEngine.cs` (extracts affix into Magic Ore 200..205 with series check)
  - [X] Implement `EquipForgeEngine.cs` (forges blank purple equipment base)
  - [X] Implement `EquipEnchaseEngine.cs` (sequential 6-socket enchasing with EnchaseLevel increment, duplicate affix checks, and gear preservation on failure)
  - [X] Implement commands: `FoundryCraftZoneCommand.cs`, `EnchaseItemZoneCommand.cs`
  - [X] Implement `FoundryCraftCommandHandler.cs`, `EnchaseItemCommandHandler.cs`
  - [X] Implement `PurpleCraftingZoneModule.cs : IZoneModule`
  - [X] Implement `ICraftingOutboundSink.cs` and `NoOpCraftingOutboundSink.cs`
  - [X] Register module in `ZoneEngineServiceCollectionExtensions.cs`

- [X] **T04 — Wire Packets & Handlers** `src/JXReborn.Adapter.Legacy/`
  - [X] Add opcodes `c2s_foundry_request` (0x92), `c2s_enchaseritem` (0x7F), `s2c_foundry_result` (0xA0) in `ProtocolOpcodes.cs` and `ProtocolSizeTable.cs`
  - [X] Implement `FoundryRequestC2SPacket.cs` (204 bytes)
  - [X] Implement `EnchaserItemC2SPacket.cs` (50 bytes)
  - [X] Implement `FoundryResultS2CPacket.cs` (7 bytes)
  - [X] Implement packet handlers: `FoundryRequestPacketHandler.cs`, `EnchaserItemPacketHandler.cs`
  - [X] Implement session sink: `GameServerClientSession.CraftingSink.cs`
  - [X] Register in `LegacyAdapterServiceCollectionExtensions.cs`

- [X] **T05 — Automated Testing & Verification**
  - [X] Unit tests: `tests/JXReborn.ZoneEngine.Tests/Crafting/OreUpgradeEngineTests.cs`
  - [X] Unit tests: `tests/JXReborn.ZoneEngine.Tests/Crafting/MagicDistillEngineTests.cs`
  - [X] Unit tests: `tests/JXReborn.ZoneEngine.Tests/Crafting/EquipEnchaseEngineTests.cs`
  - [X] Unit tests: `tests/JXReborn.ZoneEngine.Tests/Crafting/ItemValueProbabilityCalculatorTests.cs`
  - [X] Protocol tests: `tests/JXReborn.Protocol.Tests/Crafting/FoundryPacketTests.cs`
  - [X] Parity tests: `qa/JXReborn.ClientDriver.Tests/DifferentialParity/DifferentialParityPurpleCraftingTests.cs`
