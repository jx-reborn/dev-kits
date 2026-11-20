# Contract: Spatial AOI Entity Visibility & Detail Query Wire Protocol (Port 6666)

**Feature Branch**: `005-core-spatial-aoi-and-spawns` | **Date**: 2026-11-11

All packets below are exchanged over the **GameServer TCP listener (`6666`)**, prefixed by a 2-byte little-endian length header and encrypted/decrypted with the per-session `LegacyStreamCipher` stream cipher. All multi-byte values are **Little-Endian (`Pack = 1`)**.

---

## 1. On-Demand Entity Detail Requests

### 1.1 `c2s_requestplayer` (`0x48` / `72`) & `c2s_requestnpc` (`0x49` / `73`)
- **Direction**: `Client -> Server (C2S)`
- **Payload Size**: `5 bytes` (`wPacketLen = 0x0005`)
- **C# Structs**: `RequestPlayerC2SPacket`, `RequestNpcC2SPacket`

| Offset | Size | Type | Field | Description |
| :--- | :--- | :--- | :--- | :--- |
| `0x00` | `1` | `byte` | `ProtocolType` | `0x48` (`c2s_requestplayer`) or `0x49` (`c2s_requestnpc`) |
| `0x01` | `4` | `uint` | `TargetNpcId` | Runtime `NpcId` whose full `0x4C` details are requested |

**Hex Test Vector (`c2s_requestnpc`, Target = 15)**:
`49 0F 00 00 00`

---

## 2. Spatial Entity Synchronization Packets

### 2.1 `s2c_syncnpc` (`0x4C` / `76`) — Full Entity Sync
- **Direction**: `Server -> Client (S2C)`
- **Payload Size**: Variable $\ge 34$ bytes (`33B header + NameBytes + 1B null`)
- **C# Struct**: `SyncNpcFullS2CPacket`

| Offset | Size | Type | Field | Description |
| :--- | :--- | :--- | :--- | :--- |
| `0x00` | `1` | `byte` | `ProtocolType` | Always `0x4C` (`s2c_syncnpc`) |
| `0x01` | `2` | `ushort` | `m_wLength` | `TotalPayloadBytes - 1` |
| `0x03` | `1` | `byte` | `Camp` | Current camp (`0..6`) |
| `0x04` | `1` | `byte` | `Series` | Five-elements series (`0..4`: Metal, Wood, Water, Fire, Earth) |
| `0x05` | `1` | `byte` | `m_btLifePerCent` | Health scale `0..128` (`(CurrentLife << 7) / MaxLife`) |
| `0x06` | `4` | `int` | `MapMpsY` | World `MpsY` pixel coordinate |
| `0x0A` | `1` | `byte` | `DoingState` | `EntityDoingPosture` (`1` = Stand, `2` = Walk, `3` = Run, `4` = Sit, `5` = Jump) |
| `0x0B` | `1` | `byte` | `StateInfo` | Status bitmask (`0x00`) |
| `0x0C` | `4` | `int` | `MapMpsX` | World `MpsX` pixel coordinate |
| `0x10` | `1` | `byte` | `MenuState` | Menu state (`0`) |
| `0x11` | `2` | `short` | `GoldType` | Special/boss NPC indicator (`1` if special/boss NPC, else `0`) |
| `0x13` | `1` | `byte` | `Kind` | `EntityKind` (`0` = Monster, `1` = Player, `2` = Partner, `3` = Dialog NPC) |
| `0x14` | `4` | `uint` | `NpcId` | Runtime entity identifier (`1..9999` for NPCs, `>= 10000` for players) |
| `0x18` | `4` | `int` | `MasterId` | Master runtime ID (`-1` when not a partner) |
| `0x1C` | `4` | `uint` | `NpcSettingIdx` | Low 16 bits = `Level`, High 16 bits = `(ushort)(short)TemplateId` (Male Player = `0xFFFF`, Female Player = `0xFFFE`) |
| `0x20` | `1` | `byte` | `RankFfId` | Rank / special flag (`0`) |
| `0x21` | var | `byte[]` | `Name` | Null-terminated `TCVN3` encoded display name |

---

### 2.1b `s2c_syncplayer` (`0x4A` / `74`) & `s2c_syncplayermin` (`0x4B` / `75`) — Player Equipment Appearance Sync
- **`s2c_syncplayer` (`0x4A`, `tagPLAYER_SYNC`, `24 bytes` when `TongNameID == 0`)**: Sent immediately after `0x4C` whenever syncing a `Player` entity:
  - `+0x00` (`byte`): `0x4A`, `+0x01` (`ushort`): `m_wLength` (`23`), `+0x03` (`byte`): `WalkSpeed`, `+0x04` (`byte`): `AttackSpeed`, `+0x05` (`byte`): `RunSpeed`, `+0x06` (`byte`): `ArmorType` (`18` naked), `+0x07` (`byte`): `HelmType` (`18` naked), `+0x08` (`byte`): `RankID`, `+0x09` (`uint`): `NpcId`, `+0x0D` (`byte`): `HorseType` (`0xFF` unmounted), `+0x0E` (`byte`): `SomeFlag`, `+0x0F` (`byte`): `CastSpeed`, `+0x10` (`byte`): `WeaponType` (`0` bare hands), `+0x11` (`byte`): `Sex`, `+0x12` (`ushort`): `TitleID`, `+0x14` (`uint`): `TongNameID` (`0`).
- **`s2c_syncplayermin` (`0x4B`, `PLAYER_NORMAL_SYNC`, `24 bytes` fixed)**: Broadcast to self and 3x3 AOI when player equipment appearance/speeds change:
  - `+0x00` (`byte`): `0x4B`, `+0x01` (`uint`): `NpcId`, `+0x05` (`byte`): `AttackSpeed`, `+0x06` (`byte`): `RunSpeed`, `+0x07` (`byte`): `ChangeFeatureCount`, `+0x08` (`byte`): `HelmType`, `+0x09` (`byte`): `HorseType`, `+0x0A` (`byte`): `WalkSpeed`, `+0x0B` (`byte`): `CastSpeed`, `+0x0C` (`byte`): `ArmorType`, `+0x0D` (`byte`): `WeaponType`, `+0x0E` (`byte`): `MantleType` (`0xFF`), `+0x0F` (`byte`): `SomeFlag1`, `+0x10` (`byte`): `SomeFlag`, `+0x11` (`ushort`): `TitleID`, `+0x13` (`uint`): `TongNameID`, `+0x17` (`byte`): `FortuneRank`.

---

### 2.2 `s2c_syncnpcmin` (`0x4D` / `77`) — Compact Neighbor Position Sync (`tagNPC_NORMAL_SYNC`)
- **Direction**: `Server -> Client (S2C)`
- **Payload Size**: `18 bytes` (`wPacketLen = 0x0012`)
- **C# Struct**: `SyncNpcMinS2CPacket`

| Offset | Size | Type | Field | Description |
| :--- | :--- | :--- | :--- | :--- |
| `0x00` | `1` | `byte` | `ProtocolType` | Always `0x4D` (`s2c_syncnpcmin`) |
| `0x01` | `4` | `uint` | `NpcId` | Runtime entity identifier |
| `0x05` | `4` | `int` | `MpsX` | World `MpsX` pixel coordinate |
| `0x09` | `1` | `byte` | `Camp` | Current camp (`0..6`) |
| `0x0A` | `1` | `byte` | `State` | Status bitmask (`bit0=Poison, bit1=Freeze, bit2=Hide, bit3=Stun`) |
| `0x0B` | `4` | `int` | `MpsY` | World `MpsY` pixel coordinate |
| `0x0F` | `1` | `byte` | `LifeLow8` | Low 8 bits of 11-bit scaled life (`0..2048`, `LifePer128 << 4`) |
| `0x10` | `1` | `byte` | `DoingAndLifeHigh` | `((ScaledLife11 & 0x0700) >> 3) \| (Doing & 0x1F)` |
| `0x11` | `1` | `byte` | `IsSyncName` | Always `1` when entity name is synchronized |

---

### 2.4 `s2c_npcremove` (`0x4F` / `79`) — Entity Removal Sync
- **Direction**: `Server -> Client (S2C)`
- **Payload Size**: `5 bytes` (`wPacketLen = 0x0005`, `<BI`)
- **C# Struct**: `NpcRemoveS2CPacket`
- `+0x00` (`byte`): `0x4F`, `+0x01` (`uint`): `NpcId` removed from zone/AOI.

---

### 2.3 `s2c_syncnpcminplayer` (`0x4E` / `78`) — Authoritative Self Position Sync
- **Direction**: `Server -> Client (S2C)`
- **Payload Size**: `13 bytes` (`wPacketLen = 0x000D`)
- **C# Struct**: `SyncNpcMinPlayerS2CPacket`

| Offset | Size | Type | Field | Description |
| :--- | :--- | :--- | :--- | :--- |
| `0x00` | `1` | `byte` | `ProtocolType` | Always `0x4E` (`s2c_syncnpcminplayer`) |
| `0x01` | `4` | `uint` | `NpcId` | Player's own runtime `NpcId` |
| `0x05` | `2` | `ushort` | `OffX` | Sub-cell X offset: `(MpsX % 32) << 10` |
| `0x07` | `2` | `ushort` | `OffY` | Sub-cell Y offset: `(MpsY % 32) << 10` |
| `0x09` | `1` | `byte` | `CellX` | Cell X inside current region: `(MpsX / 32) % 16` |
| `0x0A` | `1` | `byte` | `CellY` | Cell Y inside current region: `(MpsY / 32) % 32` |
| `0x0B` | `1` | `byte` | `Doing` | `EntityDoingPosture` (`1` = Stand, etc.) |
| `0x0C` | `1` | `byte` | `Dir` | 64-direction facing angle (`0..63`) |
