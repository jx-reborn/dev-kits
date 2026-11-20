# Research & Ground Truth: Guilds/Tong System (Spec 024)

## Authoritative References
- **Compatibility Baseline**: Legacy 6.0 Tong/Guild multiplexed wire protocol (`0x9A`, `0xA0`, `0xA5`/`0xA8`, `0xC3`, `0xFD`) and hierarchy/upkeep rules.
- **Configuration**: `tongset.ini`, `tong_setting.ini`, `questkey.txt:196`.

## Packet Formats
- `c2s_tongextend`: Opcode `0x9A` (variable length, includes `uOper = 154`, `uParam`, `nParam`).
- `c2s_tongexmsg`: Opcode `0xA5` / `0xA8` (variable length, length-minus-one framing).
- `s2c_tongcreate`: Opcode `0xA0` (10 bytes fixed: opcode + camp byte + 8-byte name).
- `s2c_tongexmsg`: Opcode `0xC3` (variable length, length-minus-one framing).

## Upkeep Ground Truth Configuration
- Source: `$LEGACY_SERVER_DIR\server1\settings\tong\tong_setting.ini`
  ```ini
  [MoneyToExp]
  TimeLong=750
  MoneyLimit=1000000
  NormalMoney=5000
  NormalExp=120
  NormalMemberLimit=100
  NormalMemberLimitMoney=50
  LimitMoney=0
  LimitExp=0
  ```
