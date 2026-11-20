# Wire Protocol Contract: P2P Player Trade

## 1. `c2s_tradeapplystart` (`0x6B`, 9 Bytes, C2S)
```
+0x00: byte   ProtocolType = 0x6B
+0x01..0x04: uint32 TargetNpcId
+0x05..0x08: uint32 AntiCheatToken = 0x451F
```

## 2. `s2c_tradeapplystart` (`0x71`, 9 Bytes, S2C)
```
+0x00: byte   ProtocolType = 0x71
+0x01..0x04: int32  DestIndex
+0x05..0x08: uint32 ApplicantNpcId
```

## 3. `c2s_tradereplystart` (`0x75`, 10 Bytes, C2S)
```
+0x00: byte   ProtocolType = 0x75
+0x01: byte   Decision (1 = Accept, 0 = Decline)
+0x02..0x05: int32  DestIndex
+0x06..0x09: uint32 AntiCheatToken = 0x67CA
```

## 4. `s2c_tradechangestate` (`0x75`, 6 Bytes, S2C)
```
+0x00: byte   ProtocolType = 0x75
+0x01: byte   State = 2 (Trading)
+0x02..0x05: uint32 PartnerNpcId
```

## 5. `c2s_trademovemoney` (`0x6C`, 5 Bytes, C2S)
```
+0x00: byte   ProtocolType = 0x6C
+0x01..0x04: int32  OfferedMoney
```

## 6. `s2c_trademoneysync` (`0x72`, 5 Bytes, S2C)
```
+0x00: byte   ProtocolType = 0x72
+0x01..0x04: int32  PartnerOfferedMoney
```

## 7. `c2s_tradedecision` (`0x6D`, 2 Bytes, C2S)
```
+0x00: byte   ProtocolType = 0x6D
+0x01: byte   Decision (0 = Cancel, 1 = OK, 2 = Lock, 3 = Unlock, 4 = Cancel OK)
```

## 8. `s2c_tradedecision` (`0x78`, 2 Bytes, S2C)
```
+0x00: byte   ProtocolType = 0x78
+0x01: byte   PartnerDecision
```

## 9. `s2c_tradepressoksync` (`0x77`, 5 Bytes, S2C)
```
+0x00: byte   ProtocolType = 0x77
+0x01: byte   SelfLock
+0x02: byte   DestLock
+0x03: byte   SelfOk
+0x04: byte   DestOk
```

## 10. `s2c_itemexchangefinish` (`0x8E`, 1 Byte, S2C)
```
+0x00: byte   ProtocolType = 0x8E
```
