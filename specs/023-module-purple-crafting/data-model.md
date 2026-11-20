# Data Model: Purple Equipment Crafting & Enchasing (Spec 023)

## Entities & Enums

### 1. Enums (`src/JXReborn.Primitives/Crafting/`)
- `EquipUpgradeType`:
  - `Cpd = 0`: XuanJing compound
  - `UpCryolite = 1`: Upgrade cryolite
  - `UpPropMine = 2`: Upgrade property mine
  - `DistillMine = 3`: Magic property ore extraction
  - `ForgeEquip = 4`: Forge empty purple equipment
  - `EnchaseEquip = 5`: Sequential 6-socket enchasing
  - `Atlas = 6`: Recipe blueprint crafting
- `FoundryResultType`:
  - `Succeed = 0`: Successful crafting
  - `Fail = 1`: Crafting failure (materials consumed according to rule)
  - `NoMoney = 2`: Insufficient silver balance
  - `LevelFull = 3`: Stone or equipment already at maximum level
  - `LackResource = 4`: Required item missing
  - `LevelError = 5`: Incompatible item tier
  - `MagicError = 6`: Incompatible or duplicate magic affix
  - `SeriesError = 7`: Five-elements series mismatch
  - `RuleError = 8`: Sequential rule violation

### 2. DTOs (`src/JXReborn.Contracts/Crafting/`)
- `CraftingItemPositionDto`:
  - `ItemContainerPlace Place`: Target container (EquipRoom = 3)
  - `byte GridX`: Cell column
  - `byte GridY`: Cell row

### 3. Domain Persistence (`CharacterItem`)
- `Quality`: Set to `ItemQualityKind.Purple` (2) for purple gear.
- `EnchaseLevel`: Byte in range `0..10`, representing number of filled sockets or enchase tier.
- `MagicAttributes`: Array of `ItemMagicAttributeValue`, holding active and hidden line affixes and values.
- `CurrentDurability` and `MaxDurability`: Preserved during all enchasing operations.
