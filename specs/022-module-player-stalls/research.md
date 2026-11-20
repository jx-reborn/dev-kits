# Research & Ground-Truth Analysis: Spec 022 Personal City Stalls

## 1. Legacy 6.0 Wire Protocol & Tax Rules (`server1/settings/taxrates.ini`)

### A. Sub-Protocol Code Matrix (`0x7C` C2S / `0x9D` S2C)
Multiplexed C2S (`0x7C`) and S2C (`0x9D`) stall operations:
- `SubType 0x00`:
  - C2S `c2sstallmarkprice` (`12 + 8 * Count` bytes): Marks prices on 1..60 items in bag.
  - S2C `s2c_stallbuyresult` (8 bytes): Error failure code (`StallBuyFailureReason`: None=0, Closed=1, AlreadySold=2, PriceHigh=3, NotForSale=4, PriceLow=5).
- `SubType 0x01`:
  - C2S `c2sstallmakeadv` (`12 + AdvLen` bytes): Sets stall advertisement banner (`<= 32 bytes`).
  - S2C `s2c_stalladv`: Echoes banner.
- `SubType 0x02`:
  - C2S `c2sstallbuyitem` (28 bytes): Buyer purchases item (`SellerNpcId`, `ItemId`, `GridX`, `GridY`, `Price`, `ItemGenre`).
  - S2C `s2c_stallviewitems` (`12 + Count * Size` bytes): Full catalog of items in stall.
- `SubType 0x03`:
  - C2S `c2sstallexit` (4 bytes): Closes stall.
  - S2C `s2c_stallselfprice`: Synchronizes own stall's priced items.
- `SubType 0x04`:
  - C2S `c2sstallrequestadv` (8 bytes): Requests banner of seller.
  - S2C `s2c_stallclose`: Notifies client that stall closed.
- `SubType 0x05`:
  - C2S `c2sstallrequestiteminfo` (8 bytes): Requests catalog from seller.
  - S2C `s2c_stalllevelnotenough`: Rejection when level < 10.
- `SubType 0x06`:
  - C2S `c2sstallrequesttaxrate` (4 bytes): Queries local map tax rate.
  - S2C `s2c_stallsellsuccess`: Notifies seller when an item is bought.
- `SubType 0x07`:
  - S2C `s2c_stalltaxrate`: Map tax rate sync (`TaxRate`, `TaxScale = 100`).

### B. Menu State AOI Broadcast (`0x76` `s2c_syncmenustate`)
- Wire Format:
  - Byte 0: `0x76` (Opcode).
  - Bytes 1..2: `Length` (uint16).
  - Bytes 3..6: `NpcId` (uint32).
  - Byte 7: `State = 5 (PLAYER_MENU_STATE_STALL)`.
  - Bytes 8..: `BannerText` (null-terminated TCVN3).

### C. Tax Rates by Map (`taxrates.ini`)
- 7 Cities: 10%
- 8 Novice Villages: 6%
- 10 Sects: 3%
- Default: 10%
