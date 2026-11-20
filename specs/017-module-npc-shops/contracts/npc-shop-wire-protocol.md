# Wire Protocol Contract: NPC Shops & Equipment Repair

## 1. `c2s_playerbuyitem` (`0x5D`, 12 Bytes, C2S)
```
+0x00: byte   ProtocolType = 0x5D
+0x01: byte   BuyIdx       (0-based index into shop catalog goods)
+0x02: byte   Place        (Target container place, pos_equiproom = 3)
+0x03: byte   X            (Target container grid X, 0..5)
+0x04..0x07: int32  Y / Flags (Target container grid Y, 0..9)
+0x08..0x0B: uint32 CheckToken (Anti-cheat timestamp/token)
```

## 2. `c2s_playersellitem` (`0x5C`, 13 Bytes, C2S)
```
+0x00: byte   ProtocolType = 0x5C
+0x01..0x04: uint32 RuntimeItemId (ID of item in player's bag)
+0x05..0x08: uint32 SubType / Flags (0)
+0x09..0x0C: uint32 CheckToken (Anti-cheat timestamp/token)
```

## 3. `c2s_repairitem` (`0x7A`, 9 Bytes, C2S)
```
+0x00: byte   ProtocolType = 0x7A
+0x01..0x04: uint32 RuntimeItemId (ID of equipment to repair)
+0x05..0x08: uint32 RepairType (0 = Normal, 2 = Cheap Golden, 3 = Costly Golden)
```

## 4. `s2c_opensalebox` (`0x84`, 13 Bytes, S2C)
```
+0x00: byte   ProtocolType = 0x84
+0x01..0x04: int32  ShopId
+0x05..0x08: int32  ScalePercent (default 100)
+0x09..0x0C: int32  CurrencyType (0 = Gold, 1 = FuYuan, 2 = Repute, 3 = Honor, ...)
```

## 5. `s2c_itemrepair` (`0x9B`, 13 Bytes, S2C)
```
+0x00: byte   ProtocolType = 0x9B
+0x01..0x04: uint32 RuntimeItemId
+0x05..0x08: int32  MaxDurability
+0x09..0x0C: int32  CurrentDurability
```
