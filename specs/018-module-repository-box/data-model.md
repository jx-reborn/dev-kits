# Data Model: Spec 018 Repository Box, Secondary Password & Revival Binding

## 1. Storage Constants & Enums

### StorageBoxConsts
```csharp
public static class StorageBoxConsts
{
    public const byte RepositoryRoomWidthCells = 6;
    public const byte RepositoryRoomHeightCells = 10;
    public const int MaxDepositLimit = 2_000_000_000;
}
```

### StorageBoxOperationKind
```csharp
public enum StorageBoxOperationKind : byte
{
    SetPassword = 0,
    Unlock = 1,
    Lock = 2,
    ChangePassword = 3,
    CancelPassword = 4,
}
```

## 2. Session Feature Slots

### PlayerRepositorySessionSlot
```csharp
public sealed class PlayerRepositorySessionSlot : IPlayerFeatureSlot
{
    public static readonly PlayerFeatureSlotKey<PlayerRepositorySessionSlot> SlotKey = new("RepositorySession");

    public bool IsOpen { get; set; }
    public bool IsUnlocked { get; set; }

    public void OnPlayerDetached(PlayerZoneState player, ZoneExecutionContext context, PlayerDetachReason reason)
    {
        IsOpen = false;
        IsUnlocked = false;
    }
}
```

## 3. Zone Commands

- `DepositWithdrawMoneyZoneCommand(Guid CharacterId, long LeaseEpoch, byte Direction, int Amount)`
- `OperateStorageBoxZoneCommand(Guid CharacterId, long LeaseEpoch, StorageBoxOperationKind Operation, uint Password, uint NewPassword)`
- `BindRevivalPointZoneCommand(Guid CharacterId, long LeaseEpoch, ushort MapId, int PointId)`

## 4. Wire Packets

- C2S `StoreMoneyC2SPacket` (`0x73`, 14B)
- C2S `BoxOperateC2SPacket` (`0x84`, 14B)
- S2C `OpenStoreBoxS2CPacket` (`0x88`, 3B)
