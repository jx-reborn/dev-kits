# Wire Protocol Contract: Repository Box & Secondary Password

## 1. `c2s_storemoney` (`0x73`, 14 Bytes, C2S)
```
+0x00: byte   ProtocolType = 0x73
+0x01: byte   Direction (0 = Deposit Bag -> Repo, 1 = Withdraw Repo -> Bag)
+0x02..0x05: int32  Amount    (Silver amount to transfer)
+0x06..0x09: uint32 CheckToken (Anti-cheat token)
+0x0A..0x0D: uint32 Reserved   (Padding = 0)
```

## 2. `c2s_boxoperate` (`0x84`, 14 Bytes, C2S)
```
+0x00: byte   ProtocolType = 0x84
+0x01: byte   SubOpcode (0 = SetPassword, 1 = Unlock, 2 = Lock, 3 = ChangePassword, 4 = CancelPassword)
+0x02..0x05: uint32 Password (PIN numeric candidate / old PIN)
+0x06..0x09: uint32 NewPassword (New PIN for ChangePassword)
+0x0A..0x0D: uint32 Reserved (Padding = 0)
```

## 3. `s2c_openstorebox` (`0x88`, 3 Bytes, S2C)
```
+0x00: byte   ProtocolType = 0x88
+0x01..0x02: uint16 LengthMinusOne = 2
```
