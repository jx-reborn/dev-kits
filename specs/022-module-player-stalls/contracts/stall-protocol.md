# Wire Protocol Contract: Player Personal Stalls

## 1. `c2s_stallextend` (`0x7C`, Variable Length, C2S)
Dispatches all client stall operations via `SubType` byte (`0x00..0x06`).

### SubType 0x00: Mark Price
```
+0x00: byte   ProtocolType = 0x7C
+0x01: byte   SubType = 0x00
+0x02..0x03: uint16 ItemCount
+0x04..0x0B: Reserved
ForEach item:
  +0x00..0x03: uint32 RuntimeItemId
  +0x04..0x07: int32  Price
```

### SubType 0x01: Set Stall Banner
```
+0x00: byte   ProtocolType = 0x7C
+0x01: byte   SubType = 0x01
+0x02..0x03: uint16 AdvLength
+0x04..0x0B: Reserved
+0x0C..:     char[] AdvText (null-terminated TCVN3, <= 32 bytes)
```

### SubType 0x02: Buy Stall Item
```
+0x00: byte   ProtocolType = 0x7C
+0x01: byte   SubType = 0x02
+0x02..0x05: uint32 SellerNpcId
+0x06..0x09: uint32 RuntimeItemId
+0x0A..0x0D: int32  GridX
+0x0E..0x11: int32  GridY
+0x12..0x15: int32  ExpectedPrice
+0x16..0x19: int32  ItemGenre
+0x1A..0x1B: uint16 Reserved
```

### SubType 0x03: Exit Stall
```
+0x00: byte   ProtocolType = 0x7C
+0x01: byte   SubType = 0x03
+0x02..0x03: uint16 Reserved
```

### SubType 0x04: Request Stall Banner
```
+0x00: byte   ProtocolType = 0x7C
+0x01: byte   SubType = 0x04
+0x02..0x05: uint32 TargetNpcId
+0x06..0x07: uint16 Reserved
```

### SubType 0x05: Request Stall Catalog
```
+0x00: byte   ProtocolType = 0x7C
+0x01: byte   SubType = 0x05
+0x02..0x05: uint32 TargetNpcId
+0x06..0x07: uint16 Reserved
```

### SubType 0x06: Request Tax Rate
```
+0x00: byte   ProtocolType = 0x7C
+0x01: byte   SubType = 0x06
+0x02..0x03: uint16 Reserved
```

---

## 2. `s2c_syncmenustate` (`0x76`, Variable Length, S2C)
Broadcasts stall banner & `PLAYER_MENU_STATE_STALL` to nearby observers in 3x3 AOI:
```
+0x00: byte   ProtocolType = 0x76
+0x01..0x02: uint16 Length
+0x03..0x06: uint32 NpcId
+0x07: byte   MenuState = 5 (Stall)
+0x08..:     char[] BannerText (null-terminated TCVN3)
```

---

## 3. `s2c_stallextend` (`0x9D`, Variable Length, S2C)
Server responses across sub-types:
- `SubType 0x00`: Buy Result error code (`uint32 FailureReason`).
- `SubType 0x01`: Echo Banner.
- `SubType 0x02`: View items catalog.
- `SubType 0x03`: Self priced items sync.
- `SubType 0x04`: Stall closed notification.
- `SubType 0x05`: Level not enough rejection.
- `SubType 0x06`: Sell success notification (`BuyerName`, `SoldItemName`).
- `SubType 0x07`: Map tax rate sync (`int32 TaxRate`, `int32 TaxScale = 100`).
