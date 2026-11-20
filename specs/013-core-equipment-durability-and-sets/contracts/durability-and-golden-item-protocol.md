# Wire Protocol Contract: Durability Sync (`0x9B`) & Golden / Indestructible Item Sync (`0x5F`)

**Feature Branch**: `013-core-equipment-durability-and-sets`  
**Date**: 2026-11-11  
**Compatibility Sources**: Legacy 6.0 item synchronization (`0x5F`, `0x9B`), `src/JXReborn.Adapter.Legacy/Packets/ProtocolOpcodes.cs:133, 160`, `SyncItemS2CPacket.cs`

---

## 1. `0x9B` — `ItemDurabilityChangeS2CPacket` (`s2c_itemrepair` / `s2c_itemdurabilitychange`)

- **Direction**: Server-to-Client (`S2C`)
- **Opcode**: `0x9B` (`155` — `ProtocolOpcodes.s2c_itemrepair`)
- **Fixed Payload Length**: **13 bytes** (`struct.pack("<BIii", 0x9B, runtimeItemId, maxDurability, currentDurability)`)
- **Triggers**:
  1. **Combat / Movement Abrasion**: Sent immediately when an equipped item's `CurrentDurability` decrements by `1` (including `1 -> 0` Broken transition).
  2. **Equipment Repair**: Sent immediately when `RepairEquipmentItemZoneCommand` restores `CurrentDurability` (and reduces `MaxDurability` on Simple/Cheap Golden repair).

### 1.1 Byte-Offset Layout (`13` Bytes)

| Byte Offset | Size | Endianness | C# Type | Field Name | Description |
| :---: | :---: | :---: | :---: | :--- | :--- |
| `+0` (`0x00`) | `1` | — | `byte` | `Opcode` | Constant `0x9B` (`ProtocolOpcodes.s2c_itemrepair`). |
| `+1..4` (`0x01..0x04`) | `4` | Little-Endian | `uint` | `RuntimeItemId` | Ephemeral zone runtime item ID matching `0x5F` `+1..4`. |
| `+5..8` (`0x05..0x08`) | `4` | Little-Endian | `int` | `MaxDurability` | Updated maximum durability (`>= 1`). |
| `+9..12` (`0x09..0x0C`) | `4` | Little-Endian | `int` | `CurrentDurability` | Updated current durability (`0..MaxDurability`; `0` = Broken). |

### 1.2 C# Packet Contract (`src/JXReborn.Adapter.Legacy/Packets/ItemDurabilityChangeS2CPacket.cs`)

```csharp
namespace JXReborn.Adapter.Legacy.Packets;

public readonly record struct ItemDurabilityChangeS2CPacket(
    uint RuntimeItemId,
    int MaxDurability,
    int CurrentDurability)
{
    public const int PayloadSize = 13;

    public static bool TryParse(ReadOnlySpan<byte> payload, out ItemDurabilityChangeS2CPacket packet);
    public void WritePayload(Span<byte> destination);
}
```

### 1.3 Normative `0x9B` Hex Test Vectors

1. **Vector 1 — Weapon Attack Abrasion (`RuntimeItemId = 42`, `MaxDurability = 50`, `CurrentDurability = 49`)**:
   ```text
   9B 2A 00 00 00 32 00 00 00 31 00 00 00
   ```
2. **Vector 2 — Armor Breaks Mid-Combat (`RuntimeItemId = 101`, `MaxDurability = 85`, `CurrentDurability = 0`)**:
   ```text
   9B 65 00 00 00 55 00 00 00 00 00 00 00
   ```
3. **Vector 3 — Cheap Golden Repair (`RuntimeItemId = 101`, `MaxDurability = 77` (`85 - 8`), `CurrentDurability = 77`)**:
   ```text
   9B 65 00 00 00 4D 00 00 00 4D 00 00 00
   ```

---

## 2. `0x5F` — `SyncItemS2CPacket` (`s2c_syncitem`) Golden & Indestructible Wire Rules

- **Direction**: Server-to-Client (`S2C`)
- **Opcode**: `0x5F` (`95` — `ProtocolOpcodes.s2c_syncitem`)
- **Fixed Payload Length**: **80 bytes**

### 2.1 Critical `0x5F` Field Rules for Golden & Indestructible Items

1. **`+5` (`Pattern`)**:
   `Pattern = (byte)(((byte)Genre & 0x0F) | (((byte)Quality & 0x0F) << 4))`
   - Normal/Blue Equipment (`Genre = 0, Quality = 0`): `0x00`
   - **Golden Equipment (`Genre = 0, Quality = 1`)**: **`0x10`**
2. **`+6..7` (`WireDetailType`, `int16` LE)**:
   - For `Quality == ItemQualityKind.Golden` with `GoldRowId >= 1`:
     **MUST serialize `(short)(GoldRowId - 1)`** (`0`-based row index in `goldequip.txt`!).
     - Example: *Mộng Long Chính Hồng Tăng Mão* (`rowId = 1`, internal `DetailType = 7` `equip_helm`) serializes `+6..7 = 0x0000` (`00 00`).
     - Example: *Mộng Long Kim Ti Chính Hồng Cà Sa* (`rowId = 2`, internal `DetailType = 2` `equip_armor`) serializes `+6..7 = 0x0001` (`01 00`).
   - For non-Golden items (`Quality != Golden`): serializes canonical `DetailType` (`0..14`).
   - On `SyncItemS2CPacket.TryParse`: if `Quality == Golden`, `DetailType` holds the parsed wire `int16` (`GoldRowId - 1`), so `GoldRowId == (ushort)(packet.DetailType + 1)`.
3. **`+13..24` (`MagicLevel0..5`, `6 x int16` LE)**:
   - For Golden items (`Quality == Golden`), slots `0..5` carry `(short)slot.MagicAttributes[i].Value0` (the concrete rolled fixed affix value within `[Param0Min, Param0Max]`) so the client tooltip renders exact rolled stats.
4. **`+65..66` (`MaxDurability`, `int16` LE) & `+67..68` (`CurrentDurability`, `int16` LE)**:
   - Destructible items (`!IsIndestructible`): `(short)MaxDurability` and `(short)CurrentDurability` (`0..32767`).
   - **Indestructible items (`IsIndestructible == true`)**: **`-1` (`0xFFFF` → `FF FF`)** at both `+65..66` and `+67..68`.

### 2.2 Complete 80-Byte Offset Table (`0x5F`)

| Byte Offset | Size | Type (LE) | Field | Golden / Indestructible Semantics |
| :---: | :---: | :---: | :--- | :--- |
| `+0` | `1` | `uint8` | `Opcode` | `0x5F` |
| `+1..4` | `4` | `uint32` | `RuntimeItemId` | Ephemeral item ID (`> 0`) |
| `+5` | `1` | `uint8` | `Pattern` | `(Quality << 4) \| Genre` (`0x10` for Golden Equipment) |
| `+6..7` | `2` | `int16` | `WireDetailType` | `(short)(GoldRowId - 1)` when `Quality == Golden`; else `DetailType` |
| `+8..9` | `2` | `uint16` | `ParticularType` | `0` for Golden; template `ParticularType` otherwise |
| `+10` | `1` | `uint8` | `Series` | `ElementSeries` (`0..4`) |
| `+11` | `1` | `uint8` | `Level` | Item level (`1..10`) |
| `+12` | `1` | `uint8` | `Luck` | Clamped luck (`0..255`) |
| `+13..24` | `12` | `6 x int16` | `MagicLevel0..5` | Rolled `Value0` per slot for Golden; `EnchaseLevel` for Blue |
| `+25..36` | `12` | `bytes` | `Reserved0` | Zero-filled (`0x00 * 12`) |
| `+37..38` | `2` | `uint16` | `Version` | `4` (`ItemConsts.GeneratorVersion`) |
| `+39..42` | `4` | `uint32` | `RandomSeed` | Deterministic generator seed |
| `+43..44` | `2` | `uint16` | `StackCount` | `1` for equipment |
| `+45..64` | `20` | `bytes` | `Reserved1` | Zero-filled (`0x00 * 20`) |
| `+65..66` | `2` | `int16` | `MaxDurability` | `MaxDurability`, or `-1` (`FF FF`) if Indestructible |
| `+67..68` | `2` | `int16` | `CurrentDurability` | `CurrentDurability` (`0` = Broken), or `-1` (`FF FF`) if Indestructible |
| `+69` | `1` | `uint8` | `ContainerPlace` | `ItemContainerPlace` (`2` = `Equip`, `3` = `EquipRoom`) |
| `+70` | `1` | `uint8` | `GridY` | Container Y (`0` when equipped) |
| `+71` | `1` | `uint8` | `GridX` | Container X (`EquipmentPartKind` `0..14` when equipped) |
| `+72..75` | `4` | `int32` | `WidthCells` | Grid footprint width (`1..2`) |
| `+76..79` | `4` | `int32` | `HeightCells` | Grid footprint height (`1..4`) |

### 2.3 Normative 80-Byte `0x5F` Hex Test Vectors

#### Vector A — Golden Body Armor (*Mộng Long Kim Ti Chính Hồng Cà Sa*, `GoldRowId = 2` → `WireDetailType = 1`)
- `RuntimeItemId = 7` (`07 00 00 00`), `Genre = 0`, `Quality = 1 (Golden)` → `Pattern = 0x10`
- `GoldRowId = 2` → `WireDetailType = 1` (`01 00`), `ParticularType = 0` (`00 00`), `Series = 2 (Water)` (`02`), `Level = 10` (`0A`), `Luck = 0` (`00`)
- `MagicLevel0..5 = [160, 2, 75, 28, 180, 3]` (`A0 00 02 00 4B 00 1C 00 B4 00 03 00`)
- `Version = 4` (`04 00`), `RandomSeed = 0x12345678` (`78 56 34 12`), `StackCount = 1` (`01 00`)
- `MaxDurability = 90` (`5A 00`), `CurrentDurability = 90` (`5A 00`)
- `ContainerPlace = 2 (Equip)` (`02`), `GridY = 0` (`00`), `GridX = 1 (Body)` (`01`), `Width = 2` (`02 00 00 00`), `Height = 3` (`03 00 00 00`)

```text
5F 07 00 00 00 10 01 00 00 00 02 0A 00 A0 00 02
00 4B 00 1C 00 B4 00 03 00 00 00 00 00 00 00 00
00 00 00 00 00 04 00 78 56 34 12 01 00 00 00 00
00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00
00 5A 00 5A 00 02 00 01 02 00 00 00 03 00 00 00
```

#### Vector B — Indestructible Weapon (`AttributeId = 38` `indestructible_b` → `MaxDurability = -1`, `CurrentDurability = -1`)
- Same 80-byte frame with `Quality = 0` (`Pattern = 0x00`), `DetailType = 0` (`00 00`), `Series = 0` (`00`), `MagicLevel0..5 = [10, 0, 0, 0, 0, 0]`, `MaxDurability = -1` (`FF FF`), `CurrentDurability = -1` (`FF FF`), `GridX = 3 (Weapon)` (`03`), `Width = 1`, `Height = 3`:

```text
5F 08 00 00 00 00 00 00 00 00 00 0A 00 0A 00 00
00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00
00 00 00 00 00 04 00 01 00 00 00 01 00 00 00 00
00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00
00 FF FF FF FF 02 00 03 01 00 00 00 03 00 00 00
```
