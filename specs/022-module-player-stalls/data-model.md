# Data Model: Spec 022 Personal City Stalls & Market Buyout

## 1. Stall Constants & Enums

### StallConsts
```csharp
public static class StallConsts
{
    public const int MaxStallItemCount = 60;
    public const int MinStallPlayerLevel = 10;
    public const int MaxStallAdvLength = 32;
    public const byte StallMenuState = 5;
    public const int DefaultCityTaxPercent = 10;
}
```

### StallSubProtocolKind
```csharp
public enum StallSubProtocolKind : byte
{
    MarkPrice = 0,
    MakeAdv = 1,
    BuyItem = 2,
    Exit = 3,
    RequestAdv = 4,
    RequestItemInfo = 5,
    RequestTaxRate = 6,
    TaxRateSync = 7,
}
```

### StallBuyFailureReason
```csharp
public enum StallBuyFailureReason : byte
{
    None = 0,
    Closed = 1,
    AlreadySold = 2,
    PriceHigh = 3,
    NotForSale = 4,
    PriceLow = 5,
    InventoryFull = 6,
    InsufficientMoney = 7,
}
```

### PlayerMenuStateKind
```csharp
public enum PlayerMenuStateKind : byte
{
    Normal = 0,
    Trade = 1,
    Stall = 5,
}
```

## 2. Session Feature Slots

### PlayerStallSessionSlot
```csharp
public sealed class PlayerStallSessionSlot : IPlayerFeatureSlot
{
    public static readonly PlayerFeatureSlotKey<PlayerStallSessionSlot> SlotKey = new("PlayerStallSession");

    public bool IsActive { get; set; }
    public string BannerText { get; set; } = string.Empty;
    public Dictionary<uint, int> ItemPricesByRuntimeId { get; } = [];

    public void Close()
    {
        IsActive = false;
        BannerText = string.Empty;
        ItemPricesByRuntimeId.Clear();
    }

    public void OnPlayerDetached(PlayerZoneState player, ZoneExecutionContext context, PlayerDetachReason reason)
    {
        Close();
    }
}
```

## 3. Catalogs

### IStallTaxCatalog
```csharp
public interface IStallTaxCatalog
{
    int GetTaxRatePercent(ushort mapId);
}
```

## 4. Zone Commands

- `MarkStallPriceZoneCommand(Guid CharacterId, long LeaseEpoch, IReadOnlyList<(uint RuntimeItemId, int Price)> Items)`
- `SetStallBannerZoneCommand(Guid CharacterId, long LeaseEpoch, string BannerText)`
- `BuyStallItemZoneCommand(Guid BuyerCharacterId, long LeaseEpoch, uint SellerNpcId, uint RuntimeItemId, int ExpectedPrice)`
- `ExitStallZoneCommand(Guid CharacterId, long LeaseEpoch)`
- `RequestStallAdvZoneCommand(Guid CharacterId, long LeaseEpoch, uint TargetNpcId)`
- `RequestStallCatalogZoneCommand(Guid CharacterId, long LeaseEpoch, uint TargetNpcId)`
- `RequestStallTaxRateZoneCommand(Guid CharacterId, long LeaseEpoch)`
