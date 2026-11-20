# Data Model: Spec 017 Multi-Currency NPC Shops & Equipment Repair

## 1. Domain Entities & Enums

### ShopCurrencyType
```csharp
public enum ShopCurrencyType : byte
{
    Gold = 0,
    FuYuan = 1,
    Repute = 2,
    Honor = 3,
    BattleScore = 4,
    Coin = 5,
}
```

### ShopConsts
```csharp
public static class ShopConsts
{
    public const int BuySellScale = 4;
    public const int MaxShopGoods = 120;
    public const int DefaultPriceScalePercent = 100;
}
```

## 2. Session Feature Slots

### PlayerShopSessionSlot
```csharp
public sealed class PlayerShopSessionSlot : IPlayerFeatureSlot
{
    public static readonly PlayerFeatureSlotKey<PlayerShopSessionSlot> SlotKey = new("NpcShopSession");

    public int ActiveShopId { get; set; }
    public ShopCurrencyType CurrencyType { get; set; }
    public int PriceScalePercent { get; set; } = ShopConsts.DefaultPriceScalePercent;
    public bool IsOpen => ActiveShopId > 0;

    public void Close()
    {
        ActiveShopId = 0;
        PriceScalePercent = ShopConsts.DefaultPriceScalePercent;
    }

    public void OnPlayerDetached(PlayerZoneState player, ZoneExecutionContext context, PlayerDetachReason reason)
    {
        Close();
    }
}
```

## 3. Zone Commands

- `BuyShopGoodZoneCommand(Guid CharacterId, long LeaseEpoch, byte BuyIdx, byte TargetPlace, byte TargetX, byte TargetY)`
- `SellItemBackZoneCommand(Guid CharacterId, long LeaseEpoch, uint RuntimeItemId)`
- `RepairEquipmentItemZoneCommand(Guid CharacterId, long LeaseEpoch, uint RuntimeItemId, bool IsCheapGoldenRepair = false)`

## 4. Wire Packets

- C2S `PlayerBuyItemC2SPacket` (`0x5D`, 12B)
- C2S `PlayerSellItemC2SPacket` (`0x5C`, 13B)
- C2S `RepairEquipmentItemC2SPacket` (`0x7A`, 9B)
- S2C `OpenSaleBoxS2CPacket` (`0x84`, 13B)
- S2C `ItemRepairS2CPacket` (`0x9B`, 13B)
