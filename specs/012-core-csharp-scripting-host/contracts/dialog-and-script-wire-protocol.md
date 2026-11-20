# Contract: Legacy 6.0 Dialog & Script Wire Protocol (`0x6E`, `0x63`, `0x5F`, `0x59`, `0x84`, `0x88`)

**Feature Branch**: `012-core-csharp-scripting-host`  
**Adapter Project**: `JXReborn.Adapter.Legacy`  
**Compatibility Sources**:
- Legacy 6.0 dialog & script binary wire specifications (`0x6E`, `0x63`, `0x5F`, `0x59`, `0x84`, `0x88`).
- `jx-swarm`: `C2S_DialogNpcPacket.cs`, `C2S_PlayerSelUiPacket.cs`, `TagPlayerScriptActionSync.cs`, `docs/02-game-systems/npc-dialog-inventory-and-economy.md` §1.1–§1.3, §3.1.

---

## 1. Framing Table Updates (`ProtocolSizeTable.cs`)

`ProtocolSizeTable.IsValidGameServerC2SPayload` (`src/JXReborn.Adapter.Legacy/Framing/ProtocolSizeTable.cs`) MUST include:

| Opcode | Symbol | Framing Mode | Payload Validation Rule | Status |
| :---: | :--- | :---: | :--- | :--- |
| `0x6E` | `c2s_dialognpc` | Fixed | `payload.Length == 9` | Present (`line 63`) |
| `0x5F` | `c2s_playerselui` | Fixed | `payload.Length == 17` | **ADD IN `012`** |
| `0x59` | `c2s_playereatitem` | Fixed | `payload.Length == 16` | Present (`line 58`) |

---

## 2. Packet Layouts & Verified Hex Test Vectors

### 2.1 `DialogNpcC2SPacket` (`c2s_dialognpc` = `0x6E`, Fixed `9` Bytes)

- **Direction**: Client $\rightarrow$ GameServer (`6666`)
- **Binary Struct**: `<BII` (`Pack = 1`, Little-Endian)

| Offset | Size | C# Type | Field | Constraints & Semantics |
| :---: | :---: | :--- | :--- | :--- |
| `0x00` | `1B` | `byte` | `ProtocolType` | Constant `0x6E` (`110`) |
| `0x01` | `4B` | `uint` | `TargetNpcId` | Runtime `NpcZoneState.NpcId` (`1..9999`) |
| `0x05` | `4B` | `uint` | `CheckValue` | Validated via `MovementAntiCheatConsts.IsValidCheckValue(CheckValue, token1, token2, 0x506Cu)` |

- **Spatial & Session Preconditions**:
  1. `CheckValue != 0` and `((CheckValue ^ SyncEndToken1 ^ SyncEndToken2) % 0x506Cu) == 0`.
  2. Euclidean distance `dx*dx + dy*dy <= maxRadiusMps * maxRadiusMps` where `maxRadiusMps = (npc.DialogRadiusCells * 32) * 2` (`256` Mps when `DialogRadiusCells == 4`).
  3. Player is alive, sync-completed, not actively trading, and in the 3x3 region AOI of `TargetNpcId`.

- **Verified Hex Test Vector (`9` Bytes)**:
  - Inputs: `TargetNpcId = 42` (`0x0000002A`), `SyncEndToken1 = 0x11223344`, `SyncEndToken2 = 0x55667788`, `multiplier = 1`, `key = 0x506C` $\Rightarrow$ `CheckValue = 0x0000506C ^ 0x11223344 ^ 0x55667788 = 0x444414A0`.
  - Wire Payload (`9B`):
    ```text
    6E 2A 00 00 00 A0 14 44 44
    ```

---

### 2.2 `PlayerSelectUiC2SPacket` (`c2s_playerselui` = `0x5F`, Fixed `17` Bytes)

- **Direction**: Client $\rightarrow$ GameServer (`6666`)
- **Binary Struct**: `PLAYER_SELECTUI_COMMAND` (`<Biiii`, `Pack = 1`, Little-Endian, Linux 6.0 `g_nProtocolSize[0x1E] = 17`)

| Offset | Size | C# Type | Field | Constraints & Semantics |
| :---: | :---: | :--- | :--- | :--- |
| `0x00` | `1B` | `byte` | `ProtocolType` | Constant `0x5F` (`95`) |
| `0x01` | `4B` | `int` | `SelectIndex` | `0..OptionCount - 1` for option selection (`0` for `Talk` completion), or `< 0` (`-1`) to cancel/close |
| `0x05` | `4B` | `int` | `SelectType` | `0` = Standard `Say`/`Describe`/`Talk` dialog UI (`1` = DiceGame, ignored in `012`) |
| `0x09` | `4B` | `int` | `Param1` | `0` for standard dialog UI |
| `0x0D` | `4B` | `int` | `Param2` | `0` for standard dialog UI |

- **Verified Hex Test Vectors (`17` Bytes)**:
  - **Select Option Index `1`**:
    ```text
    5F 01 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00
    ```
  - **Cancel / Close Dialog (`SelectIndex = -1`)**:
    ```text
    5F FF FF FF FF 00 00 00 00 00 00 00 00 00 00 00 00
    ```

---

### 2.3 `ScriptActionS2CPacket` (`s2c_scriptaction` = `0x63`, Variable `16 + m_nBufferLen` Bytes)

- **Direction**: GameServer (`6666`) $\rightarrow$ Client
- **Binary Struct**: `tagPLAYER_SCRIPTACTION_SYNC` (`<BHBBBBBii` + `byte[m_nBufferLen]`, `Pack = 1`, Little-Endian)
- **Limits**: `MAX_ANSWERNUM = 50`, `MaxScriptActionContentBytes = 900` (`TotalPayloadSize <= 916` bytes)

| Offset | Size | C# Type | Field | `Say` Mode | `Describe` Mode | `Talk` Mode |
| :---: | :---: | :--- | :--- | :--- | :--- | :--- |
| `0x00` | `1B` | `byte` | `ProtocolType` | `0x63` | `0x63` | `0x63` |
| `0x01` | `2B` | `ushort` | `ProtocolLengthMinusOne` | `15 + BufferLen` | `15 + BufferLen` | `15 + BufferLen` |
| `0x03` | `1B` | `byte` | `OperateType` | `0x00` | `0x02` | `0x00` |
| `0x04` | `1B` | `byte` | `UiId` | `0x00` (`UI_SELECTDIALOG`) | `0x00` (`UI_SELECTDIALOG`) | `0x02` (`UI_TALKDIALOG`) |
| `0x05` | `1B` | `byte` | `OptionOrPageCount` | `0..50` options | `0..50` options | `1..50` pages |
| `0x06` | `1B` | `byte` | `Param1` | `0x00` (inline string) | `0x00` (inline string) | `0x00` (inline string) |
| `0x07` | `1B` | `byte` | `Param2` | `0x01` (server UI) | `0x01` (server UI) | `0x01` (server UI) |
| `0x08` | `4B` | `int` | `Param` | `-1` (`0xFFFFFFFF`) | `-1` (`0xFFFFFFFF`) | `1` if `onComplete != null` else `0` |
| `0x0C` | `4B` | `int` | `BufferLen` | `Content.Length` (`<= 900`) | `Content.Length` (`<= 900`) | `Content.Length` (`<= 900`) |
| `0x10` | `Var` | `byte[]` | `Content` | `[Prompt][0x00][Opt0][0x00]...` (TCVN3) | `[RichPrompt][0x00][Opt0][0x00]...` (TCVN3) | `[Page0]\| \|[Page1]\| \|...` (TCVN3, `0x7C 0x20 0x7C`) |

#### Verified Hex Test Vector A — `Say` Menu (`Prompt = "Xa Phu:"`, Options = `["Ba Lang", "Thoat"]`, `38` Bytes)
- `Content` (`22` bytes): `"Xa Phu:\0"` (`58 61 20 50 68 75 3A 00`, 8B) + `"Ba Lang\0"` (`42 61 20 4C 61 6E 67 00`, 8B) + `"Thoat\0"` (`54 68 6F 61 74 00`, 6B).
- `ProtocolLengthMinusOne = 15 + 22 = 37` (`0x0025`).
```text
63 25 00 00 00 02 00 01 FF FF FF FF 16 00 00 00
58 61 20 50 68 75 3A 00 42 61 20 4C 61 6E 67 00
54 68 6F 61 74 00
```

#### Verified Hex Test Vector B — `Describe` Menu (`Prompt = "<link>Hi"`, Options = `["OK"]`, `28` Bytes)
- `Content` (`12` bytes): `"<link>Hi\0"` (`3C 6C 69 6E 6B 3E 48 69 00`, 9B) + `"OK\0"` (`4F 4B 00`, 3B).
- `OperateType = 0x02`, `UiId = 0x00`, `OptionCount = 1`, `Param = -1`, `ProtocolLengthMinusOne = 15 + 12 = 27` (`0x001B`).
```text
63 1B 00 02 00 01 00 01 FF FF FF FF 0C 00 00 00
3C 6C 69 6E 6B 3E 48 69 00 4F 4B 00
```

#### Verified Hex Test Vector C — `Talk` 2-Page Monologue with Completion Callback (`Pages = ["Page1", "Page2"]`, `32` Bytes)
- `Content` (`16` bytes): `"Page1| |"` (`50 61 67 65 31 7C 20 7C`, 8B) + `"Page2| |"` (`50 61 67 65 32 7C 20 7C`, 8B).
- `OperateType = 0x00`, `UiId = 0x02`, `PageCount = 2`, `Param = 1` (`01 00 00 00`), `ProtocolLengthMinusOne = 15 + 16 = 31` (`0x001F`).
```text
63 1F 00 00 02 02 00 01 01 00 00 00 10 00 00 00
50 61 67 65 31 7C 20 7C 50 61 67 65 32 7C 20 7C
```

---

### 2.4 `PlayerEatItemC2SPacket` (`c2s_playereatitem` = `0x59`, Fixed `16` Bytes — `TargetId` Forwarding)

- **Direction**: Client $\rightarrow$ GameServer (`6666`)
- **Existing Parser**: `src/JXReborn.Adapter.Legacy/Packets/PlayerEatItemC2SPacket.cs`

| Offset | Size | C# Type | Field | Constraints & Semantics |
| :---: | :---: | :--- | :--- | :--- |
| `0x00` | `1B` | `byte` | `ProtocolType` | Constant `0x59` (`89`) |
| `0x01` | `1B` | `byte` | `GridX` | Bag/belt grid X coordinate |
| `0x02` | `4B` | `uint` | `RuntimeItemId` | `PlayerItemSlot.RuntimeItemId` |
| `0x06` | `1B` | `byte` | `GridY` | Bag/belt grid Y coordinate |
| `0x07` | `1B` | `ItemContainerPlace` | `ContainerPlace` | `EquipRoom = 3` or `Immediacy = 7` |
| `0x08` | `4B` | `uint` | `AntiBotToken` | `0` |
| `0x0C` | `4B` | `uint` | `TargetId` | **Forwarded to `UseInventoryItemZoneCommand.TargetNpcId`** (`0` if untargeted) |

- **Verified Hex Test Vector (`16` Bytes, `RuntimeItemId = 0x00010005`, `GridX = 2`, `GridY = 3`, `EquipRoom = 3`, `TargetId = 42`)**:
```text
59 02 05 00 01 00 03 03 00 00 00 00 2A 00 00 00
```

---

## 3. Thin Core UI Trigger Reference Contracts (Phase 2 Bridge for `017` & `018`)

`IDialogOutboundSink` exposes thin outbound triggers invoked when NPC dialog options open core merchant or repository windows:

| Opcode | Symbol | Size | Layout | Downstream Spec |
| :---: | :--- | :---: | :--- | :--- |
| `0x84` | `s2c_opensalebox` | `13B` | `<Biii`: `0x84`, `int32 ShopId`, `int32 ScalePercent` (`100`), `int32 CurrencyType` (`0` = Gold) | `017-module-npc-shops-and-economy` |
| `0x88` | `s2c_openstorebox` | `3B` | `<BBB`: `0x88`, `byte BoxPageCount` (`1`), `byte Reserved` (`0`) | `018-module-repository-storage-box` |
