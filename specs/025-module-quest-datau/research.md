# Research: 8,000 Dã Tẩu Quest Engine (Spec 025)

## Authoritative Ground-Truth Sources

1. **Linux Server Scripts (`$LEGACY_SERVER_DIR\server1\script\task\` and `global\`)**:
   - `tasklink_head.lua`:
     - 3-tier hierarchy: 20 tasks per link, 20 links per loop, 20 loops = 8,000 tasks.
     - Streak formula: $\text{Streak} = \text{Loops} \times 400 + \text{Links} \times 20 + \text{Tasks} + 1$.
     - Task acceptance: `Task_Accept_01` through `Task_Accept_06`.
     - Baseline tracking variables: EXP in 1033 & 1034, Repute in 1026, FuYuan in 1026, PK in 1026, Song-Jin in 1026.
   - `seasonnpc.lua`:
     - Daily quota default: `SLNV_DaTau = 40`.
     - Daily streak rewards: 10, 20, 30, 40 (500k to 2M EXP + Báo rương Dã Tẩu).
     - Cancellation (`Task_Cancel`):
       - If `myTaskCancel >= 1`: consumes 1 cancellation chance and **preserves streak**.
       - If `nType == 2` (100 Sơn Hà Xã Tắc fragments): deducts 100 fragments and **preserves streak**.
       - If cancelled without privilege: resets `tl_settaskstate(1..6)` to 0, `nt_setTask(1044, 0)` (resets 8000 streak to 0), and inflicts 10-minute cooldown penalty.
     - Fast-complete (`Task_CancelConfirm_Accept`): Consumes Thủy Tinh or Tinh Hồng Bảo Thạch or silver, up to 30 times daily, advances streak.
     - Three-way reward presentation: Calls `Prise("...", text1, text2, text3)` presenting 3 random choices.
   - `random\treasure_head.lua`:
     - `MISS_DIS_X = 6`, `MISS_DIS_Y = 9`.
     - Digging position tolerance: $|X_{\text{trea}} - X_{\text{player}}| \le 6$ and $|Y_{\text{trea}} - Y_{\text{player}}| \le 9$.

2. **Server Settings & Catalogs (`$LEGACY_SERVER_DIR\server1\settings\task\`)**:
   - `random\trea_pos.txt`: 245 canonical positions with MapID, WorldX, WorldY, MapX, MapY.
   - `award_link.txt`: Declarative milestone rewards from 40 up to 8000:
     - 40: Sách kỹ năng cấp 90 (`6, 1, 2426`)
     - 1000..3000: Complete Định Quốc Set (Hộ Uyển 162, Yêu Đái 163, Nhuyễn Ngoa 161, Trường Sam 159, Phát Quán 160)
     - 3280..3800: Complete An Bang Set (213, 214, 215, 216)
     - 8000: Càn Khôn Liên Hoa (`6, 1, 5123`) and global server announcement broadcast.
   - `tasklink_findgoods.txt`: Green equipment criteria with exact `MagicEnName`, min value, max value.
   - `tasklink_upground.txt`: Progress Delta tasks with delta amount and numeric type.

3. **Legacy 6.0 Wire Framing (`0x63`, `0x5F`, `0xB6`)**:
   - 3-choice `Prise` reward prompt emits `s2c_scriptaction` (`0x63`, SubType `0x0D`).
   - Client responds with `c2s_playerselui` (`0x5F`, 17 bytes: `<Biiii`).
   - Task order summary frame sends a 65-byte net message `<0xB6 + 64B text>`.
