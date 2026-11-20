# Headless Legacy Client Reverse-Engineering Notes (`game.exe`)

Reverse-engineered via Ghidra 12.2 (`artifacts/ghidra-jxclient/JxClientProj`) against a local read-only legacy client (`LEGACY_CLIENT_DIR`).

## 1. Binary Architecture & Clean Dependencies

- **`game.exe`** (`ImageBase = 0x00400000`, non-ASLR): statically contains Core + UI + Login logic (`KMyApp`, `KLogin`, `KNetConnectAgent`, `KCoreShell`, `KProtocolProcess`).
- **Required clean runtime DLLs**:
  - `engine.dll`: Win32 window loop (`KWin32App`), INI/PAK VFS (`KIniFile`), DirectDraw/DirectSound wrappers, and `?KSG_StringToMD5String@@YAHQADQBD@Z`.
  - `rainbow.dll`: TCP client transport (`CreateClientManager`) imported dynamically by `game.exe`.
  - `lualibdll.dll`, `zlib.dll`.
  - `filtertext_orig.dll -> filtertext.dll`: clean `CreateTextFilter @1` with zero mod dependencies (unlike modded `filtertext.dll` which pulls extra third-party UI/replay DLLs).
  - `represent2.dll`: replaced by our Zero-GPU `NullRepresent2.dll` exporting ordinal `@1 CreateRepresentShell`.
  - `winmm.dll`: replaced by our `JxHeadlessAgent` proxy DLL (forwards all 193 exports of `C:\Windows\SysWOW64\winmm.dll` and activates headless hooks + Named Pipe JSON-RPC when `LEGACY_HEADLESS=1`).

## 2. Critical Boot & Isolation Findings

1. **`Config.ini` (`KeepOutSide=1`, `FullScreen=0`, `Represent=2`, `Plugin=0`)**:
   - `WinMain` (`0x00525950`) calls recursive directory deletion (`0x00525750`) on `\Settings` unless `KeepOutSide` is non-zero in `Config.ini`!
2. **`\UserData\UiCommon.ini`**:
   - Required by `KLogin::GetServerList` (`0x0052b350`) when `SelServerRegion == 0`; omitting it silently breaks region/server initialization.
3. **Single-instance Mutex/Semaphore bypass (`Hooks.cpp`)**:
   - `0x00528340` opens/creates `"CLIENT{24C3984E-0B02-4391-A08C-48BB106E5643}"` and `"JXONLINE{24C3984E-0B02-4391-A08C-48BB106E5643}"`.
   - `0x006155c0` checks `"ApolloDetourSignature"`.
   - IAT hooks on `CreateSemaphoreA`, `OpenSemaphoreA`, `CreateMutexA`, `OpenMutexA` suffix `_<PID>` onto these names so 2+ `game.exe` instances run concurrently without collision.
4. **Windowless / Zero-GUI**:
   - `KWin32App::Run` (`engine.dll` `0x1001fa20`) requires an active message loop (`m_bActive` `+0x48 = 1`, `m_bMultiGame` `+0x4c = 1`) and valid `HWND` from `CreateWindowExA`. Our `CreateWindowExA` hook clears `WS_VISIBLE` and places the window offscreen without showing it, while `ShowWindow`, `SetWindowPos` (`SWP_SHOWWINDOW`), `MessageBoxA/W`, and `ShellExecuteA/W` are neutralized.
   - `DirectSoundCreate` returns `DSERR_NODRIVER` (`0x88780078`), skipping audio device initialization.
   - `0x00526750` (exit ad handler) is patched to `RET 4` (`C2 04 00`).
5. **Preventing Registry Pollution**:
   - `0x005279d0` writes `HKCR\jxr_file\...` keys on startup; `ADVAPI32.dll` hooks return `ERROR_ACCESS_DENIED` for `jxr_file`.

## 3. `iRepresentShell` 38-Slot Vtable (`NullRepresent2`)

- Slot `13` (`+0x34`) `GetImageParam(const char*, uint16_t param[7], int nType)`: writes `{1, 0, 32, 32, 1, 0, 0}` and returns `1`.
- Slot `14` (`+0x38`) `GetImageFrameParam(...)`: guarded pointer writes `{0, 0}` / `{32, 32}`, returns `1`.
- Slot `28` (`+0x70`) `GetOption(int nOption, int* pnValue)`: **must** return `100` (and set `*pnValue = 100` when `nOption == 0`), because `KUiGameSpace` divides by `*pnValue`.
- Slot `36` (`+0x90`) `GetViewport(RECT* pRect)`: writes `{0, 0, 800, 600}` AND returns `pRect` in `EAX` (callers dereference `EAX`).

## 4. Login, Protocol, World Arrays, and C2S Calling Conventions

- **Login & Server Entry**:
  - `connect_gateway` calls `FN_NET_CONNECT_GATEWAY(0x006de818, inet_addr(host), port)` + `FN_KLOGIN_REGISTER_TARGETS(0x006dec30)` and sets `KLogin.m_Status = 1, m_Result = 0`.
  - `account_login` computes MD5 via `engine.dll!KSG_StringToMD5String(md5Buf, password)` and calls `FN_KLOGIN_ACCOUNT_LOGIN(0x006dec30, acct, md5Buf, 1)` with `bSavePassword = 1` (required so `KLogin + 0x720/+0x740` stores the bitwise-NOT credentials used by `0x0052a020` to validate `0x41` `s2c_login` responses!).
  - Role creation (`0x00529a50`) takes `RoleInfo36B { Name[32], Gender, Series, NativePlaceId }` (`NativePlaceId = 53` for Ba Lăng Huyện).
- **World State (`SUBWORLD`, `PLAYER`, `NPC`)**:
  - `Player[1]` (`*(uint8_t**)0x008e48f4 + 0x8b30`) owns local `m_nIndex` (`+0x114` -> `0x8c44` from array base), `m_bPauseSend` (`+0x20`), `m_nMoney` (`+0x58a4`), `m_nPKValue` (`+0x5ec0`).
  - `Npc[1..255]` (`*(uint8_t**)0x012c5540`, stride `0x113b8`) stores every entity's `m_dwID`, `m_Index`, `m_Kind` (`0`=monster, `1`=player, `3`=dialoger), `m_Level`, `m_Series`, `m_CurrentLife`/`Max`, `m_CurrentMana`/`Max`, `Name[32]` (TCVN3), `m_SubWorldIndex`, `m_RegionIndex`, `m_MapX`, `m_MapY`, `m_OffX`, `m_OffY`.
  - World pixel coordinates `(mpsX, mpsY)` are resolved directly via `SubWorld[m_SubWorldIndex]::Map2Mps` (`0x00570970`, `RET 0x1c`).
- **Gameplay C2S & Dialog Capture**:
  - `FN_KNPC_SEND_CMD_MOVE` (`0x00579700`, `__thiscall`, `RET 0x0c`): `(pMyNpc, mpsX, mpsY, mode)` (`1`=Run `0x4b`, `2`=Walk `0x4c`).
  - `FN_SEND_NPC_DIALOG` (`0x00567580`, `__cdecl`): `(npcArrayIndex)` -> sends 9B `0x6e`.
  - `0x005554b0` (`g_ProtocolProcess[0x63]`, `__stdcall`, `RET 8`): `(const uint8_t* pMsg, int nParam)` handles `s2c_scriptaction`. When `pMsg[0] == 0x63 && pMsg[3] == 0 && pMsg[4] == 0` (`Say`), `pMsg[5]` gives option count and `pMsg + 0x10` (bounded by `*(int*)(pMsg + 0x0c)`) holds TCVN3 NUL-separated prompt and options; wrapping `CoreDataChanged` (`0x006dfde0` vtable `[0]`, `__thiscall`, `RET 0x10`, `uMsgId == 0x16`) also captures already-decoded prompt & options right as `game.exe` opens `KUiMsgSel`.
  - `FN_UI_MSG_SEL_CLICK` (`0x00509180`, `__thiscall`, `RET 0x4`) or `FN_CORESHELL_OP_REQUEST` (`0x0053b770`, `__stdcall`, `RET 0x0c`, `(9, 0, optionIdx)`) emits `0x5f` `c2s_playerselui`.
  - `FN_SEND_NPC_SKILL` (`0x0058d220`, `__cdecl`): `(skillId, mpsXOrMinus1, mpsYOrTargetDwId)` emits 17B `0x4d` `c2s_npcskill`.
  - `FN_SEND_SWITCH_PK` (`0x005ac2a0`) / `FN_SEND_PK_TARGET` (`0x005ac390`) on `this = 0x013336ec` emit `0x76` / `0x77`.
