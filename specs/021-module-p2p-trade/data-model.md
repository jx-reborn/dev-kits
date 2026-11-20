# Data Model: Spec 021 Zero-Dupe P2P Player Trade

## 1. Trade Constants & Enums

### TradeConsts
```csharp
public static class TradeConsts
{
    public const byte TradeRoomWidthCells = 10;
    public const byte TradeRoomHeightCells = 4;
    public const int MaxTradeItems = 40;
    public const ushort ApplyAntiCheatToken = 0x451F;
    public const ushort ReplyAntiCheatToken = 0x67CA;
}
```

### TradeDecisionKind
```csharp
public enum TradeDecisionKind : byte
{
    Cancel = 0,
    Confirm = 1,
    Lock = 2,
    Unlock = 3,
    CancelConfirm = 4,
}
```

### TradeSessionStateKind
```csharp
public enum TradeSessionStateKind : byte
{
    None = 0,
    Applying = 1,
    Trading = 2,
    Locked = 3,
    Confirmed = 4,
}
```

## 2. Session Feature Slots

### PlayerTradeSessionSlot
```csharp
public sealed class PlayerTradeSessionSlot : IPlayerFeatureSlot
{
    public static readonly PlayerFeatureSlotKey<PlayerTradeSessionSlot> SlotKey = new("PlayerTradeSession");

    public Guid TargetCharacterId { get; set; }
    public uint TargetNpcId { get; set; }
    public TradeSessionStateKind State { get; set; }
    public bool IsSelfLocked { get; set; }
    public bool IsSelfConfirmed { get; set; }
    public int OfferedMoney { get; set; }
    public List<uint> OfferedItemRuntimeIds { get; } = [];

    public void Reset()
    {
        TargetCharacterId = Guid.Empty;
        TargetNpcId = 0;
        State = TradeSessionStateKind.None;
        IsSelfLocked = false;
        IsSelfConfirmed = false;
        OfferedMoney = 0;
        OfferedItemRuntimeIds.Clear();
    }

    public void OnPlayerDetached(PlayerZoneState player, ZoneExecutionContext context, PlayerDetachReason reason)
    {
        Reset();
    }
}
```

## 3. Domain & Persistence Entities

### TradeAuditLog
```csharp
public sealed class TradeAuditLog
{
    public Guid Id { get; private set; }
    public Guid CharacterAId { get; private set; }
    public Guid CharacterBId { get; private set; }
    public int MoneyAtoB { get; private set; }
    public int MoneyBtoA { get; private set; }
    public string ItemIdsAtoB { get; private set; }
    public string ItemIdsBtoA { get; private set; }
    public long LeaseEpochA { get; private set; }
    public long LeaseEpochB { get; private set; }
    public DateTimeOffset TimestampUtc { get; private set; }
}
```

## 4. Zone Commands

- `ApplyTradeZoneCommand(Guid CharacterId, long LeaseEpoch, uint TargetNpcId)`
- `ReplyTradeZoneCommand(Guid CharacterId, long LeaseEpoch, int TargetIndex, bool Accepted)`
- `MoveTradeMoneyZoneCommand(Guid CharacterId, long LeaseEpoch, int OfferedMoney)`
- `DecisionTradeZoneCommand(Guid CharacterId, long LeaseEpoch, TradeDecisionKind Decision)`
