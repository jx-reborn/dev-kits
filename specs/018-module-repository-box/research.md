# Research & Ground-Truth Analysis: Spec 018 Repository Box & Password

## 1. Legacy 6.0 Wire Protocol & Repository Box Rules

### A. Silver Deposit & Withdraw (`0x73`)
- **C2S Wire Packet**: `0x73` (`c2s_storemoney`), size = 14 bytes.
  - Byte 0: `0x73` (Opcode).
  - Byte 1: `Direction` (`0` = Deposit Bag -> Repo, `1` = Withdraw Repo -> Bag).
  - Bytes 2..5: `Amount` (int32 little-endian).
  - Bytes 6..9: `CheckToken` (anti-bot token).
  - Bytes 10..13: `Reserved` (0).
- **Validation**:
  - `Amount > 0`.
  - If Deposit: `player.InventoryMoney >= Amount`.
  - If Withdraw: player's box must be unlocked, and `player.RepositoryMoney >= Amount`.
  - Broadcasts `0x61` `s2c_syncmoney`.

### B. Storage Box PIN Operations (`0x84`)
- **C2S Wire Packet**: `0x84` (`c2s_boxoperate`), size = 14 bytes.
  - Byte 0: `0x84` (Opcode).
  - Byte 1: `SubOpcode` (0 = SetPassword, 1 = Unlock, 2 = Lock, 3 = ChangePassword, 4 = CancelPassword).
  - Bytes 2..5: `Password` (DWORD / PIN numeric / candidate).
  - Bytes 6..9: `NewPassword` (DWORD / new PIN for ChangePassword).
  - Bytes 10..13: `Reserved` (0).
- **Sub-Opcode Details**:
  - SubOpcode 0 (`PlayerSetPassword`): Sets secondary PIN hash, sets `IsUnlocked = true`.
  - SubOpcode 1 (`PlayerUnlock`): Validates PIN against stored hash; if matches, sets `IsUnlocked = true`.
  - SubOpcode 2 (`PlayerLock`): Sets `IsUnlocked = false`.
  - SubOpcode 3 (`PlayerChangePassword`): Verifies old PIN; updates to new PIN.
  - SubOpcode 4 (`PlayerCanclePassword`): Verifies PIN; clears password hash to null.

### C. Storage Box Open (`0x88`)
- **S2C Wire Packet**: `0x88` (`s2c_openstorebox`), size = 3 bytes.
  - Byte 0: `0x88` (Opcode).
  - Bytes 1..2: `LengthMinusOne = 2` (uint16 little-endian).

---

## 2. Existing Codebase Assets

- `Character.cs`:
  - `InventoryMoney`, `RepositoryMoney`, `RevivalMapId`, `RevivalPointId`, `RepositoryPasswordHash`.
  - `SetRepositoryPasswordFromPlaintext`, `VerifyRepositoryPassword`.
- `LegacyPasswordHasher.cs`:
  - Standard MD5 hex digest generator and constant-time validator.
- `PlayerZoneState.cs`:
  - Mirrors repository money and PIN hash.
