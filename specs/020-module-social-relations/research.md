# Research & Ground Truth: Social Relations (Spec 020)

## Authoritative References
- **Compatibility Baseline**: Legacy 6.0 friend-list, social status, and killer tracking protocol contracts (`0x61`–`0x72`).

## Packet Structures
- `c2s_chatapplyaddfriend`: Opcode `0x61` (variable length, includes 256-byte max message).
- `c2s_chataddfriend`: Opcode `0x62` (5 bytes fixed: opcode + target player index).
- `c2s_chatrefusefriend`: Opcode `0x63` (5 bytes fixed: opcode + target player index).
- `c2s_chatapplyresendallfriendname`: Opcode `0x65` (1 byte fixed).
- `c2s_chatapplysendonefriendname`: Opcode `0x66` (5 bytes fixed: opcode + target ID).
- `c2s_chatdeletefriend`: Opcode `0x67` (5 bytes fixed: opcode + target ID).
- `c2s_chatredeletefriend`: Opcode `0x68` (5 bytes fixed: opcode + target ID).
- `s2c_chatapplyaddfriend`: Opcode `0x6A` (293 bytes fixed).
- `s2c_chataddfriend`: Opcode `0x6B` (41 bytes fixed).
- `s2c_chatonefrienddata`: Opcode `0x70` (41 bytes fixed).
- `s2c_chatfriendonline`: Opcode `0x71` (9 bytes fixed).
- `s2c_chatfriendoffline`: Opcode `0x72` (5 bytes fixed).
