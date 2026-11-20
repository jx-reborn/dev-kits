# Contract 04: GameServer World Entry & Heartbeat Wire Protocol (`TCP Port 6666`)

**Feature**: [spec.md](../spec.md) | **Data Model**: [data-model.md](../data-model.md)  
**Compatibility Reference**: `jx-swarm/docs/01-session-lifecycle/gameserver-session-and-sync.md` (Legacy 6.0 GameServer session & initial sync burst)  

---

## 1. World Attach & Initial Synchronization Handshake Sequence

Upon connecting to GameServer port `6666`, the server issues the unencrypted 44-byte `AccountBeginS2CPacket` (`0x20`) handshake frame. The client and server then execute the following byte-exact protocol handshake:

1. **Client $\rightarrow$ Server: `LogicLoginC2SPacket` (`0x42`, Fixed 17 Bytes Payload)**:
   - `+0x00` (`1B`): `0x42`
   - `+0x01` (`16B`): `Guid` (the 16-byte single-use handoff ticket issued in `0x34`).
   - The server validates the ticket against `ISessionLeaseAppService`, binds the connection to `MapZoneWorker`, and dispatches the initial synchronization burst.

2. **Server $\rightarrow$ Client: Initial World Synchronization Burst**:
   - **`SyncWorldS2CPacket` (`0x49`, Fixed 34B)**:
     - `+0x00`: `0x49`
     - `+0x01` (`int32`): `SubWorldId` (e.g. `53` Ba Lăng Huyện)
     - `+0x05` (`int32`): `MapCopyIndex = 0`
     - `+0x09` (`int32`): `PackedRegionId = (RegionY << 16) | RegionX`
     - `+0x0D` (`byte`): `Weather = 0`
     - `+0x0E` (`uint32`): `Frame` (current 18-Hz simulation tick)
     - `+0x12` (`byte[16]`): `MapName` (null-terminated TCVN3)
   - **`SyncNpcFullS2CPacket` (`0x4C`, Variable $\ge 34$B, 33B header + NameBytes + 1B null)**:
     - Visual entity appearance, coordinates (`MapMpsX`, `MapMpsY`), `MasterId = -1` (non-partner), and character name in TCVN3.
   - **`SyncPlayerS2CPacket` (`0x4A`, Variable $\ge 24$B, 24B when `TongNameID == 0`)**:
     - Player equipment resource indices (`ArmorType`, `HelmType`, `WeaponType`, `HorseType`) from `settings/item/004/*res.txt` (`val - 2`; naked defaults: `ArmorType = 18`, `HelmType = 18`, `WeaponType = 0`, `HorseType = 0xFF`).
   - **`SyncCurPlayerS2CPacket` (`0x44`, Fixed 68B)**:
     - Full base statistics (`RuntimeNpcId`, `Level`, `MaxLife`, `MaxStamina`, `Gender`, `Series`, `MaxMana`, `AttributePoint`, `Energy`, `SkillPoint`, `Strength`, `Dexterity`, `Vitality`, `MoneyInventory`, `MoneyRepository`).
   - **`SyncCurPlayerSkillS2CPacket` (`0x45`, Variable 483B)**:
     - `+0x00`: `0x45`, `+0x01` (`uint16`): `Length = 482`, `+0x03`: 80 skill slots (`uint16 SkillId`, `byte Level`, `byte AddLevel`, `uint16 ExpPercent`).
   - **`SyncCurPlayerNormalS2CPacket` (`0x46`, Fixed 14B)**:
     - Current vitals (`CurrentStamina`, `CurrentMana`, `CurrentLife`).
   - **`SyncMoneyS2CPacket` (`0x61`, Fixed 13B)** & **`SyncItemS2CPacket` (`0x5F`, Fixed 80B $\times N$)**:
     - Synchronizes personal/repository gold and all inventory/belt/equipped items prior to `SyncEnd` (`0x43`) matching legacy 6.0 initial world-entry order.
   - **`PkSyncNormalFlagS2CPacket` (`0x90`, Fixed 5B)** & **`PkSyncPkValueS2CPacket` (`0x93`, Fixed 5B)**:
     - Emitted in `GameServerClientSession.WorldSink.cs` between `0x5F` (`SyncItem`) and `0xA4` (`DisabledFuncFlag`) on initial world entry, synchronizing the player's initial PK normal flag mode (`PkState`) and PK value (`PkValue`).
   - **`DisabledFuncFlagS2CPacket` (`0xA4`, Fixed 5B)**:
     - Feature flag bitmask.
   - **`SyncEndS2CPacket` (`0x43`, Fixed 1B)**:
     - Single opcode byte `0x43` signaling end of initial sync burst.

3. **Client $\rightarrow$ Server: `SyncEndC2SPacket` (`0x43`, Fixed 9 Bytes Payload)**:
   - `+0x00`: `0x43`, `+0x01` (`uint32`): `ClientToken`, `+0x05` (`uint32`): `ClientTick`.

4. **Server $\rightarrow$ Client: Active Confirmation**:
   - Server marks `PlayerZoneState.IsSyncCompleted = true` and sends `SyncCurPlayerNormalS2CPacket` (`0x46`, `14B`) and `SyncNpcMinPlayerS2CPacket` (`0x4E`, `13B`).

---

## 2. Continuous Heartbeat & Vitality Clock Synchronization (`0x82` / `0x70` / `0x99` & `0x46`)

- **Server Ping (`0x82`, Fixed 5B)**: Emitted periodically every 5 seconds with `ServerTimestamp` (`uint32`).
- **Client Ping Reply (`0x70`, Fixed 9B)**: Echoes `ServerTimestamp` and appends `ClientTimestamp` (`uint32`). Updates `LastPingReceivedTick`.
- **Server Ping Ack (`0x99`, Fixed 5B)**: Echoes `ClientTimestamp` back to client.
- **Heartbeat Timeout**: If `LastPingReceivedTick` is not updated within 15 seconds (`DefaultHeartbeatTimeoutSeconds`), the server evicts the session, saves character state snapshot, and releases the session lease.
- **Continuous 18 TPS Vitality Pulse (`0x46`, Fixed 14B)**:
  - In `MapZoneWorker` (`MapZoneWorker.cs`), every 18-Hz simulation tick emits `0x46` (`SyncCurPlayerNormalS2CPacket`) to all active synchronized players (`IsSyncCompleted = true`).
  - This 18 TPS pulse drives the legacy client's internal 18-Hz frame clock so its main simulation loop remains continuously active rather than stalling.

---

## 3. Clean Disconnection Contract (`0x9B`)

- **`CloseConnectionC2SPacket` (`0x9B`, Fixed 1B)**:
  - Sent by client upon exit.
  - Server immediately dispatches `DetachPlayerZoneCommand`, saves latest character state snapshot via `ICharacterAppService`, and releases `SessionLease`.

---

## 4. Verification Infrastructure

World entry handshake sequencing, PK flag synchronization, and continuous 18 TPS vitality clock pulses are verified using:
- **`qa/JXReborn.ClientDriver`**: Headless client automation harness executing live end-to-end `0x42` $\rightarrow$ `0x43` world entry handshakes and validating frame timings against live server.
- **`tests/JXReborn.TestBase/WireSimulators/GameExeProtocolVerifier.cs`**: Protocol verifier validating byte-exact wire serialization, opcode order, payload sizes, and reference golden vectors.
