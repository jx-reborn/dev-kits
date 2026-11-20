# Wire Protocol Contract: PK & PvP Binary Packets (`014-core-pk-combat-rules`)

**Adapter Boundary**: `src/JXReborn.Adapter.Legacy`  
**Framing**: Packed binary structs (`Pack = 1`), little-endian integer serialization, legacy `TCVN3` string transcoding strictly at the adapter boundary.  
**Compatibility Target**: Legacy 6.0 PK, camp-sync, enmity, and sparring binary packet layouts (`0x54`, `0x58`, `0x76`, `0x77`, `0x90`, `0x91`, `0x92`, `0x93`).

---

## 1. Opcode & Size Summary (`ProtocolSizeTable`)

| Direction | Opcode | Wire Name | JXReborn Type | Payload Size (Bytes) | `m_wLength` Header? |
| :---: | :---: | :--- | :--- | :---: | :---: |
| `C2S` | `0x54` (`84`) | `c2s_playerapplysetpk` | `PlayerApplySetPkC2SPacket` | `2` | No |
| `C2S` | `0x76` (`118`) | `c2s_pkapplychangenormalflag` | `PkApplyChangeNormalFlagC2SPacket` | `2` | No |
| `C2S` | `0x77` (`119`) | `c2s_pkapplyenmity` | `PkApplyEnmityC2SPacket` | `5` | No |
| `S2C` | `0x58` (`88`) | `s2c_npcchangecurcamp` | `NpcChangeCurCampS2CPacket` | `6` | No |
| `S2C` | `0x90` (`144`) | `s2c_pksyncnormalflag` | `PkSyncNormalFlagS2CPacket` | `2` | No |
| `S2C` | `0x91` (`145`) | `s2c_pksyncenmitystate` | `PkSyncEnmityStateS2CPacket` | `8 + N` (`8..39`) | Yes (`7 + N`) |
| `S2C` | `0x92` (`146`) | `s2c_pksyncexercisestate` | `PkSyncExerciseStateS2CPacket` | `8 + N` (`8..39`) | Yes (`7 + N`) |
| `S2C` | `0x93` (`147`) | `s2c_pksyncpkvalue` | `PkSyncPkValueS2CPacket` | `5` | No |

> **Important**: `1v1 Sparring` (`ExercisePK`) has **no** standalone C2S opcode in the legacy 6.0 protocol. It is triggered server-side via `StartExerciseSparZoneCommand` / `IScriptDialogContext.StartExerciseSpar` (e.g., after an NPC dialog selection over standard `0x5F` `c2s_playerselui`) and synchronized outbound via `0x92`.

---

## 2. Client-to-Server (`C2S`) Packet Contracts

### 2.1 `c2s_playerapplysetpk` (`0x54`, `2` Bytes) — `PlayerApplySetPkC2SPacket`
Toggles the player's temporary Free-PK camp indicator.

| Byte Offset | Size | Type | Field | Description |
| :---: | :---: | :--- | :--- | :--- |
| `0x00` | `1` | `uint8` | `ProtocolType` | `0x54` |
| `0x01` | `1` | `uint8` | `PkCampFlag` | `0` = restore `CurrentCamp` to `BaseCamp`; non-zero (`1`) = set `CurrentCamp` to `EntityCamp.Free` (`4`) if `BaseCamp != Begin` |

**Hex Test Vectors**:
- Enable temporary Free camp: `54 01`
- Restore faction Base camp: `54 00`

---

### 2.2 `c2s_pkapplychangenormalflag` (`0x76`, `2` Bytes) — `PkApplyChangeNormalFlagC2SPacket`
Requests switching the character's normal PK stance.

| Byte Offset | Size | Type | Field | Description |
| :---: | :---: | :--- | :--- | :--- |
| `0x00` | `1` | `uint8` | `ProtocolType` | `0x76` |
| `0x01` | `1` | `uint8` | `PkMode` | `0` = `Peace` (*Luyện công*), `1` = `Fight` (*Chiến đấu*), `2` = `Slaughter` (*Đồ sát*) |

**Hex Test Vectors**:
- Switch to `Peace` (`0`): `76 00`
- Switch to `Fight` (`1`): `76 01`
- Switch to `Slaughter` (`2`): `76 02`

---

### 2.3 `c2s_pkapplyenmity` (`0x77`, `5` Bytes) — `PkApplyEnmityC2SPacket`
Declares targeted Enmity (*Cừu sát*) against a nearby player entity.

| Byte Offset | Size | Type | Field | Description |
| :---: | :---: | :--- | :--- | :--- |
| `0x00` | `1` | `uint8` | `ProtocolType` | `0x77` |
| `0x01` | `4` | `uint32 LE` | `TargetNpcId` | Runtime `NpcId` (`> 0`) of the target player in the initiator's 3x3 AOI |

**Hex Test Vector** (`TargetNpcId = 1002` / `0x000003EA`):
- `77 EA 03 00 00`

---

## 3. Server-to-Client (`S2C`) Packet Contracts

### 3.1 `s2c_npcchangecurcamp` (`0x58`, `6` Bytes) — `NpcChangeCurCampS2CPacket`
Broadcasts an entity's updated `CurrentCamp` to all players in the 3x3 region AOI.

| Byte Offset | Size | Type | Field | Description |
| :---: | :---: | :--- | :--- | :--- |
| `0x00` | `1` | `uint8` | `ProtocolType` | `0x58` |
| `0x01` | `4` | `uint32 LE` | `NpcId` | Runtime `NpcId` of the entity whose `CurrentCamp` changed |
| `0x05` | `1` | `uint8` | `Camp` | Updated `EntityCamp` (`0`=Begin, `1`=Justice, `2`=Evil, `3`=Balance, `4`=Free) |

**Hex Test Vectors** (`NpcId = 1001` / `0x000003E9`):
- Switched to `EntityCamp.Free` (`4`): `58 E9 03 00 00 04`
- Restored to `EntityCamp.Justice` (`1`): `58 E9 03 00 00 01`

---

### 3.2 `s2c_pksyncnormalflag` (`0x90`, `2` Bytes) — `PkSyncNormalFlagS2CPacket`
Synchronizes the recipient's authoritative normal PK stance on initial world entry (`SendInitialWorldSyncBurst`) and after every `0x76` request (accepted or rejected).

| Byte Offset | Size | Type | Field | Description |
| :---: | :---: | :--- | :--- | :--- |
| `0x00` | `1` | `uint8` | `ProtocolType` | `0x90` |
| `0x01` | `1` | `uint8` | `PkMode` | Authoritative `PlayerPkMode` (`0`=Peace, `1`=Fight, `2`=Slaughter) |

**Hex Test Vectors**:
- `Peace`: `90 00`
- `Fight`: `90 01`
- `Slaughter`: `90 02`

---

### 3.3 `s2c_pksyncenmitystate` (`0x91`, `8 + N` Bytes) — `PkSyncEnmityStateS2CPacket`
Synchronizes targeted Enmity state transitions (`Closed = 0`, `Countdown = 1`, `Active = 2`).

| Byte Offset | Size | Type | Field | Description |
| :---: | :---: | :--- | :--- | :--- |
| `0x00` | `1` | `uint8` | `ProtocolType` | `0x91` |
| `0x01` | `2` | `uint16 LE` | `LengthMinusOne` | `7 + N` (`TotalPayloadBytes - 1`) |
| `0x03` | `1` | `uint8` | `State` | `PkEnmityState` (`0` = Closed, `1` = Countdown `200f`, `2` = Active `10800f`) |
| `0x04` | `4` | `uint32 LE` | `OpponentNpcId` | `0` when `State == 0`; opponent's `RuntimeNpcId` when `State` is `1` or `2` |
| `0x08` | `N` (`0..31`) | `byte[N]` | `OpponentNameTcvn3` | `TCVN3`-encoded opponent role name bytes without trailing `0x00` (`N = 0` when `State == 0`) |

**Hex Test Vectors**:
- **Close (`State = 0, OpponentNpcId = 0, N = 0`)** (`8` bytes, `LengthMinusOne = 7`):
  `91 07 00 00 00 00 00 00`
- **Countdown (`State = 1, OpponentNpcId = 1002, Name = "DocCo"`, `N = 5`)** (`13` bytes, `LengthMinusOne = 12` / `0x000C`):
  `91 0C 00 01 EA 03 00 00 44 6F 63 43 6F`
- **Active (`State = 2, OpponentNpcId = 1002, Name = "DocCo"`, `N = 5`)** (`13` bytes, `LengthMinusOne = 12` / `0x000C`):
  `91 0C 00 02 EA 03 00 00 44 6F 63 43 6F`

---

### 3.4 `s2c_pksyncexercisestate` (`0x92`, `8 + N` Bytes) — `PkSyncExerciseStateS2CPacket`
Synchronizes 1v1 Sparring (`Exercise`) state transitions (`0` = Inactive/Closed, `1` = Active).

| Byte Offset | Size | Type | Field | Description |
| :---: | :---: | :--- | :--- | :--- |
| `0x00` | `1` | `uint8` | `ProtocolType` | `0x92` |
| `0x01` | `2` | `uint16 LE` | `LengthMinusOne` | `7 + N` (`TotalPayloadBytes - 1`) |
| `0x03` | `1` | `uint8` | `State` | `0` = Sparring Closed, `1` = Sparring Active |
| `0x04` | `4` | `uint32 LE` | `OpponentNpcId` | `0` when `State == 0`; sparring partner's `RuntimeNpcId` when `State == 1` |
| `0x08` | `N` (`0..31`) | `byte[N]` | `OpponentNameTcvn3` | `TCVN3`-encoded partner role name bytes without trailing `0x00` (`N = 0` when `State == 0`) |

**Hex Test Vectors**:
- **Close (`State = 0, OpponentNpcId = 0, N = 0`)** (`8` bytes, `LengthMinusOne = 7`):
  `92 07 00 00 00 00 00 00`
- **Open (`State = 1, OpponentNpcId = 1002, Name = "DocCo"`, `N = 5`)** (`13` bytes, `LengthMinusOne = 12` / `0x000C`):
  `92 0C 00 01 EA 03 00 00 44 6F 63 43 6F`

---

### 3.5 `s2c_pksyncpkvalue` (`0x93`, `5` Bytes) — `PkSyncPkValueS2CPacket`
Synchronizes the character's current criminal PK murder value (`0..10`) on initial world entry (`SendInitialWorldSyncBurst`) and whenever `PkValue` increments or decays.

| Byte Offset | Size | Type | Field | Description |
| :---: | :---: | :--- | :--- | :--- |
| `0x00` | `1` | `uint8` | `ProtocolType` | `0x93` |
| `0x01` | `4` | `int32 LE` | `PkValue` | Authoritative PK murder value (`0..10`) |

**Hex Test Vectors**:
- `PkValue = 0`: `93 00 00 00 00`
- `PkValue = 2` (Enmity launcher kill): `93 02 00 00 00`
- `PkValue = 10` (Maximum red-name cap): `93 0A 00 00 00`
