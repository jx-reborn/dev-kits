# Wire Protocol Contract: Faction, Camp, Attribute & Skill Synchronization

**Feature**: `015-core-factions-and-skills`  
**Byte Order**: Little-Endian (`x86` `#pragma pack(1)`)  
**Compatibility Sources**:
- Legacy 6.0 faction, camp, and skill wire protocol specifications (`0x55`, `0x58`, `0x59`, `0x5D`, `0x5E`, `0x45`, `0x7B`)
- `jx-swarm/src/JXSwarm.Protocol/Packets/` (`C2S_PlayerApplyFactionDataPacket.cs`, `TagPlayerFactionDataSync.cs`, `TagPlayerSkillLevelSync.cs`, `TagNpcChangeCurCampSync.cs`)

---

## 1. Opcode & Framing Summary (`ProtocolSizeTable.cs` & `ProtocolOpcodes.cs`)

| Opcode | Canonical Name | C# Packet Type | Direction | Framing Size | Status in `jx-server` |
| :-: | :--- | :--- | :-: | :-: | :--- |
| `0x55` | `c2s_playerapplyfactiondata` | `PlayerApplyFactionDataC2SPacket` | `C2S` | Fixed `1` B | **ADD** to `ProtocolOpcodes.cs` & `ProtocolSizeTable.cs` (`table[0x55] = 1`) |
| `0x58` | `c2s_playeraddskillpoint` | `PlayerApplyAddSkillPointC2SPacket` | `C2S` | Fixed `17` B | Existing (`[0] 0x58`, `[1..4] int32 SkillId`, `[5..8] int32 PointsToAdd`, `8B` reserved) |
| `0x7B` | `s2c_playerfactiondata` | `PlayerFactionDataS2CPacket` | `S2C` | Fixed `8` B | **ADD** new packet & WorldSink serializer |
| `0x59` | `s2c_npcchgcamp` | `NpcChangeCampS2CPacket` | `S2C` | Fixed `6` B | **FIX** opcode name (`s2c_npcchgcamp`) & **ADD** packet |
| `0x58` | `s2c_npcchangecurcamp` | `NpcChangeCurCampS2CPacket` | `S2C` | Fixed `6` B | Aligned with Spec `014` (`CurrentCamp` AOI broadcast) |
| `0x45` | `s2c_synccurplayerskill` | `SyncCurPlayerSkillS2CPacket` | `S2C` | Variable `483` B | Existing (`[0] 0x45`, `[1..2] u16=482`, `80 * 6B`); include `BaseLevel == 0` slots |
| `0x5D` | `s2c_playerattributesync` | `PlayerAttributeS2CPacket` | `S2C` | Fixed `14` B | Existing; emitted `4x` on `WashAttributes` & `+5` on *Tẩy Tủy Kinh* |
| `0x5E` | `s2c_playerskilllevel` | `PlayerSkillLevelS2CPacket` | `S2C` | Fixed `15` B | **FIX** swapped bytes `[9..12]` (`int32`) & `[13..14]` (`uint16`) |

---

## 2. Exact Byte-Offset Layouts & Hex Test Vectors

### 2.1 `0x55` — `c2s_playerapplyfactiondata` (`1` Byte, `C2S`)
Sent by the client when requesting a refresh of its faction and camp status.

| Offset | Size | Type | Field | Description |
| :-: | :-: | :--- | :--- | :--- |
| `0` | `1` | `byte` | `ProtocolType` | Constant `0x55` |

- **Test Vector (`1` Byte)**:
  ```text
  55
  ```

---

### 2.2 `0x7B` — `s2c_playerfactiondata` (`8` Bytes, `S2C`)
Wire struct reference: 8-byte faction status sync (`TagPlayerFactionDataSync.cs`).

| Offset | Size | Type | Field | Description |
| :-: | :-: | :--- | :--- | :--- |
| `0` | `1` | `byte` | `ProtocolType` | Constant `0x7B` |
| `1` | `1` | `byte` | `BaseCamp` | `0` Begin, `1` Justice, `2` Evil, `3` Balance, `4` Free |
| `2` | `1` | `byte` | `CurrentFactionId` | `0..9` active sect, or `0xFF` (`255`) when novice/graduated |
| `3` | `1` | `byte` | `FirstFactionId` | `0..9` original sect, or `0xFF` (`255`) when never inducted |
| `4..7` | `4` | `int32` | `FactionJoinCount` | Cumulative number of sect inductions (`nAddTimes`, little-endian) |

- **Test Vector A — Pure Novice (`Camp = 0`, `Cur = 0xFF`, `First = 0xFF`, `AddTimes = 0`)**:
  ```text
  7B 00 FF FF 00 00 00 00
  ```
- **Test Vector B — Inducted Shaolin Disciple (`Camp = 1` Justice, `Cur = 0`, `First = 0`, `AddTimes = 1`)**:
  ```text
  7B 01 00 00 01 00 00 00
  ```
- **Test Vector C — Graduated Tangmen Martial Artist (`Camp = 4` Free, `Cur = 0xFF`, `First = 2`, `AddTimes = 1`)**:
  ```text
  7B 04 FF 02 01 00 00 00
  ```

---

### 2.3 `0x59` — `s2c_npcchgcamp` & `0x58` — `s2c_npcchangecurcamp` (`6` Bytes each, `S2C` AOI Broadcast)
Wire contract: `0x59` updates permanent `BaseCamp` (`Npc.m_Camp`), while `0x58` updates active `CurrentCamp` (`Npc.m_CurrentCamp`).

| Offset | Size | Type | Field | Description |
| :-: | :-: | :--- | :--- | :--- |
| `0` | `1` | `byte` | `ProtocolType` | `0x59` (`BaseCamp`) or `0x58` (`CurrentCamp`) |
| `1..4` | `4` | `uint32` | `NpcId` | Runtime world `NpcId` of the character (little-endian) |
| `5` | `1` | `byte` | `Camp` | New `EntityCamp` (`0` Begin, `1` Justice, `2` Evil, `3` Balance, `4` Free) |

- **Test Vector — `NpcId = 1001` (`0x000003E9`) Graduating to `Free` Camp (`4`)**:
  - `0x59` (`BaseCamp`):
    ```text
    59 E9 03 00 00 04
    ```
  - `0x58` (`CurrentCamp`):
    ```text
    58 E9 03 00 00 04
    ```

---

### 2.4 `0x5E` — `s2c_playerskilllevel` (`15` Bytes, `S2C` — **Field Order Fix Mandatory**)
Wire contract: 15-byte skill level & remaining point sync (`TagPlayerSkillLevelSync.cs` in `jx-swarm`, `movement-and-combat.md:310-318`).

> **CRITICAL REGRESSION GUARD**: Previously `PlayerSkillLevelS2CPacket.cs` swapped bytes `[9..12]` and `[13..14]`. The byte layout below is mandatory.

| Offset | Size | Type | Field | Description |
| :-: | :-: | :--- | :--- | :--- |
| `0` | `1` | `byte` | `ProtocolType` | Constant `0x5E` |
| `1..4` | `4` | `int32` | `SkillId` | `SkillTemplate.Id` (little-endian) |
| `5..8` | `4` | `int32` | `SkillLevel` | Effective client skill level (`BaseLevel + BonusLevel`, little-endian) |
| `9..12` | `4` | `int32` | `UnallocatedSkillPoints` | Remaining unspent skill points (`m_nLeavePoint`, little-endian) |
| `13..14` | `2` | `uint16` | `SkillExpPercent` | Proficiency percent `0..100` (`m_wSkillExpPercent`, little-endian) |

- **Test Vector — Allocating `0 -> 1` on Shaolin *Hàng Long Bất Vũ* (`SkillId = 10`, `SkillLevel = 1`, `UnallocatedSkillPoints = 9`, `SkillExpPercent = 0`)**:
  ```text
  5E 0A 00 00 00 01 00 00 00 09 00 00 00 00 00
  ```
- **Test Vector — Training Level 90 Skill *Bạo Vũ Lê Hoa* (`SkillId = 302` = `0x012E`, `SkillLevel = 3`, `UnallocatedSkillPoints = 12`, `SkillExpPercent = 65` = `0x0041`)**:
  ```text
  5E 2E 01 00 00 03 00 00 00 0C 00 00 00 41 00
  ```

---

### 2.5 `0x45` — `s2c_synccurplayerskill` (`483` Bytes, `S2C`)
Full 80-slot skill book synchronization sent on world entry, sect induction, and `WashSkills`.

| Offset | Size | Type | Field | Description |
| :-: | :-: | :--- | :--- | :--- |
| `0` | `1` | `byte` | `ProtocolType` | Constant `0x45` |
| `1..2` | `2` | `uint16` | `PacketLength` | Constant `482` (`0x01E2` — excludes opcode byte, or `483` per existing framing convention verified in `SyncCurPlayerSkillS2CPacket.cs`) |
| `3 + 6*i` | `2` | `uint16` | `Slot[i].SkillId` | `SkillTemplate.Id` (`0` for unused trailing slots, `i = 0..79`) |
| `5 + 6*i` | `1` | `byte` | `Slot[i].BaseLevel` | Point-allocated or book-granted base level (`0` for newly granted/washed sect skills!) |
| `6 + 6*i` | `1` | `byte` | `Slot[i].BonusLevel` | Equipment `+AllSkills` bonus (`> 0` only when `BaseLevel > 0` or `CanEquipAdd` applies) |
| `7 + 6*i` | `2` | `uint16` | `Slot[i].ExpPercent` | Proficiency percentage (`0..100`) |

- **Slot Encoding Example (`6` Bytes for `SkillId = 351` *Loạn Hoàn Kích* granted at `BaseLevel = 0`, `BonusLevel = 0`, `ExpPercent = 0`)**:
  ```text
  5F 01 00 00 00 00
  ```

---

### 2.6 `0x5D` — `s2c_playerattributesync` (`14` Bytes, `S2C`)
Sent `4` times (`AttributeType = 0..3`) during `WashAttributes` and after consuming *Tẩy Tủy Kinh* (`+5` unallocated attribute points).

| Offset | Size | Type | Field | Description |
| :-: | :-: | :--- | :--- | :--- |
| `0` | `1` | `byte` | `ProtocolType` | Constant `0x5D` |
| `1` | `1` | `byte` | `AttributeType` | `0` Strength, `1` Dexterity, `2` Vitality, `3` Energy |
| `2..5` | `4` | `int32` | `BaseValue` | Base attribute value (`StarterBase + Allocated + QuestBonus`) |
| `6..9` | `4` | `int32` | `CurrentValue` | Effective attribute value (`BaseValue + EquipmentBonuses`) |
| `10..13` | `4` | `int32` | `UnallocatedAttributePoints` | Remaining unallocated attribute points (`m_nRemainPoint`) |

- **Test Vector — Washed Metal Character Strength (`AttributeType = 0`, `Base = 35` = `0x23`, `Current = 35`, `Unallocated = 410` = `0x019A`)**:
  ```text
  5D 00 23 00 00 00 23 00 00 00 9A 01 00 00
  ```

---

## 3. End-to-End Packet Choreography

| Gameplay Event | Unicast to Owning Player | 3x3 AOI Broadcast (Including Self) |
| :--- | :--- | :--- |
| **World Entry / Zone Attach** | `0x45` (full skill list incl. `BaseLevel == 0`) → `0x7B` (`PlayerFactionDataS2CPacket`) | Existing `NpcSync` carries derived `CurrentCamp` |
| **Client Sends `0x55`** | `0x7B` (`PlayerFactionDataS2CPacket`) | None |
| **Sect Induction (`JoinFaction`)** | `0x7B` → `0x45` (includes newly granted `BaseLevel == 0` sect skills) | `0x59` (`BaseCamp`) + `0x58` (`CurrentCamp`) |
| **Sect Graduation (`GraduateFaction`)** | `0x7B` (`CurFaction = 0xFF`, `BaseCamp = 4`) | `0x59` (`BaseCamp = 4`) + `0x58` (`CurrentCamp = 4`) |
| **Allocate Skill Point (`0x58`)** | `0x5E` (`SkillId`, `EffectiveLevel`, `UnallocatedSkillPoints`, `ExpPercent`) | None (unless passive affects public vitals) |
| **Consume 90x / 120x Skill Book** | Inventory remove sync + `0x5E` (or `0x45` when `IniLevel == 0`) | None |
| **Consume *Võ Lâm Mật Tịch*** | Inventory remove sync + `0x5E` (refreshing `UnallocatedSkillPoints` via first known skill or `0x45`) | None |
| **Consume *Tẩy Tủy Kinh*** | Inventory remove sync + `0x5D` (`Strength` sync carrying `+5` `UnallocatedAttributePoints`) | None |
| **`WashAttributes`** | `4x` `0x5D` (`AttributeType = 0..3`) + vitals sync (`0x46`) | None |
| **`WashSkills`** | `0x45` (washed skills at `BaseLevel = 0`) + `0x5E` (updated `UnallocatedSkillPoints`) | None |
