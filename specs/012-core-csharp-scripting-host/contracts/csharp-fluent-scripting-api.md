# Contract: C# 14 Fluent Gameplay Scripting API (`JXReborn.ZoneEngine.Scripting` & `JXReborn.Scripts`)

**Feature Branch**: `012-core-csharp-scripting-host`  
**Target Framework**: `.NET 10` (`net10.0`) / `C# 14` (`Nullable=enable`, `TreatWarningsAsErrors=true`, `1 type per file`, `sealed` concrete classes)

---

## 1. Architectural Pinning Rule (Collectible Hot-Reload Safety)

1. **Contract Layer (`JXReborn.ZoneEngine`)**: All interfaces (`IZoneScriptHost`, `INpcDialogScript`, `IItemUsageScript`, `ITrapScript`, `IMissionController`, `IScriptDialogContext`, `IScriptItemContext`, `IScriptTrapContext`, `IMissionContext`, `IDialogOutboundSink`) and value types (`DialogOption`, `ItemUsageResult`) reside in the **default non-collectible** `JXReborn.ZoneEngine` assembly.
2. **Implementation & Collectible Script Layer (`JXReborn.Scripts` + Dynamic Script `.dll`s)**: Concrete host types (`ZoneScriptHost`, `FluentDialogBuilder`, `CollectibleScriptAssemblyLoader`) and concrete gameplay scripts (`TownTeleporterDialogScript`, etc.) reside in `JXReborn.Scripts` or collectible `AssemblyLoadContext(isCollectible: true)` assemblies loaded exclusively from `MemoryStream` (`byte[]`).
3. **Zero Residual References**: `PlayerDialogFeatureSlot` clears all `Func<IScriptDialogContext, ValueTask>` delegates *prior* to invoking any selected option callback and inside `OnPlayerDetached(...)`, ensuring `PlayerZoneState` never pins an unloaded collectible `AssemblyLoadContext`.

---

## 2. Non-Collectible Scripting Contracts (`src/JXReborn.ZoneEngine/Scripting/`)

### 2.1 Value Types & Enumerations

```csharp
namespace JXReborn.ZoneEngine.Scripting;

/// <summary>
/// Explicit return outcome for <see cref="IItemUsageScript.OnUseItemAsync"/>, replacing legacy Lua's inverted 0/1 convention.
/// </summary>
public enum ItemUsageResult : byte
{
    KeepItem = 0,
    ConsumeOne = 1,
}
```

```csharp
using System;
using System.Threading.Tasks;

namespace JXReborn.ZoneEngine.Scripting;

/// <summary>
/// Represents a single selectable option in an NPC or item dialog menu; <see cref="Callback"/> stays strictly server-side.
/// </summary>
public readonly record struct DialogOption(
    string TextUtf8,
    Func<IScriptDialogContext, ValueTask>? Callback = null);
```

---

### 2.2 Four Gameplay Script Entry-Point Interfaces

```csharp
using System.Threading;
using System.Threading.Tasks;

namespace JXReborn.ZoneEngine.Scripting;

public interface INpcDialogScript
{
    string ScriptCode { get; }

    ValueTask OnDialogAsync(
        IScriptDialogContext context,
        CancellationToken cancellationToken = default);
}
```

```csharp
using System.Threading;
using System.Threading.Tasks;

namespace JXReborn.ZoneEngine.Scripting;

public interface IItemUsageScript
{
    string ScriptCode { get; }

    ValueTask<ItemUsageResult> OnUseItemAsync(
        IScriptItemContext context,
        CancellationToken cancellationToken = default);
}
```

```csharp
using System.Threading;
using System.Threading.Tasks;

namespace JXReborn.ZoneEngine.Scripting;

public interface ITrapScript
{
    string ScriptCode { get; }

    ValueTask OnStepTrapAsync(
        IScriptTrapContext context,
        CancellationToken cancellationToken = default);
}
```

```csharp
using System;
using System.Threading;
using System.Threading.Tasks;

namespace JXReborn.ZoneEngine.Scripting;

public interface IMissionController
{
    string MissionCode { get; }

    int TimerIntervalFrames { get; }

    ValueTask OnMissionStartAsync(IMissionContext context, CancellationToken cancellationToken = default);

    ValueTask OnMissionTimerTickAsync(IMissionContext context, CancellationToken cancellationToken = default);

    ValueTask OnPlayerJoinedAsync(IMissionContext context, Guid characterId, byte groupCampId, CancellationToken cancellationToken = default);

    ValueTask OnPlayerLeftAsync(IMissionContext context, Guid characterId, CancellationToken cancellationToken = default);

    ValueTask OnMissionStopAsync(IMissionContext context, CancellationToken cancellationToken = default);
}
```

---

### 2.3 Per-Invocation Context Interfaces (UTF-8 & Engine-First Thin Helpers)

```csharp
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using JXReborn.Primitives.Characters;

namespace JXReborn.ZoneEngine.Scripting;

public interface IScriptDialogContext
{
    // Identity & Spatial Read-Only Properties (UTF-8 + Unit-Explicit)
    Guid CharacterId { get; }
    uint PlayerRuntimeNpcId { get; }
    string RoleNameUtf8 { get; }
    byte Level { get; }
    CharacterGender Gender { get; }
    ElementSeries Series { get; }
    byte CurrentFactionId { get; }
    ushort MapId { get; }
    int CurrentMpsX { get; }
    int CurrentMpsY { get; }
    int InventoryMoney { get; }
    int FreeBagCellCount { get; }
    uint? SourceNpcId { get; }
    string? SourceNpcNameUtf8 { get; }

    // Persistent & Session-Volatile Quest Variables (0..65535 -> int32)
    int GetTask(ushort taskId);
    void SetTask(ushort taskId, int value);
    int GetTempTask(ushort tempTaskId);
    void SetTempTask(ushort tempTaskId, int value);

    // Interactive Dialog Primitives (0x63 s2c_scriptaction)
    void Say(string promptUtf8, params ReadOnlySpan<DialogOption> options);
    void Describe(string richHeaderAndPromptUtf8, params ReadOnlySpan<DialogOption> options);
    void Talk(IReadOnlyList<string> pagesUtf8, Func<IScriptDialogContext, ValueTask>? onCompleted = null);
    void SendSystemMessage(string messageUtf8);

    // Engine-First Thin Core Helpers & Phase 2 UI Triggers
    bool TryPayMoney(int amountGold);
    void GrantMoney(int amountGold);
    void GrantExperience(long amountExp);
    bool CanFitBagItem(byte gridWidthCells, byte gridHeightCells);
    bool TryGrantItemByCode(string itemCode, int stackCount = 1);
    bool TryConsumeItemByCode(string itemCode, int count = 1);
    ValueTask<bool> TeleportPlayerAsync(
        ushort targetMapId,
        int targetMpsX,
        int targetMpsY,
        byte targetFightMode = 0,
        CancellationToken cancellationToken = default);
    void OpenShop(int shopId, byte currencyType = 0, int scalePercent = 100);
    void OpenStoreBox();
}
```

```csharp
using JXReborn.ZoneEngine.Items;

namespace JXReborn.ZoneEngine.Scripting;

public interface IScriptItemContext : IScriptDialogContext
{
    uint RuntimeItemId { get; }
    int ItemTemplateId { get; }
    string ItemCode { get; }
    int CurrentStackCount { get; }
    uint TargetNpcId { get; }
}
```

```csharp
namespace JXReborn.ZoneEngine.Scripting;

public interface IScriptTrapContext : IScriptDialogContext
{
    uint TrapId { get; }
    string TrapScriptCode { get; }
    byte CurrentFightMode { get; }
    void SetFightMode(byte fightMode);
}
```

```csharp
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace JXReborn.ZoneEngine.Scripting;

public interface IMissionContext
{
    ushort MapId { get; }
    long CurrentFrame { get; }
    int PhaseId { get; set; }
    IReadOnlyDictionary<Guid, byte> Participants { get; }

    int GetMissionValue(ushort key);
    void SetMissionValue(ushort key, int value);

    bool TryAddParticipant(Guid characterId, byte groupCampId);
    bool RemoveParticipant(Guid characterId);
    void BroadcastMissionMessage(string messageUtf8, byte? groupCampFilter = null);
    ValueTask TeleportParticipantAsync(
        Guid characterId,
        ushort targetMapId,
        int targetMpsX,
        int targetMpsY,
        byte targetFightMode = 0,
        CancellationToken cancellationToken = default);
}
```

---

### 2.4 Outbound Wire Sink & Host Registry Interfaces

```csharp
using System.Collections.Generic;

namespace JXReborn.ZoneEngine.PlayerSessions;

/// <summary>
/// Domain-segregated outbound wire port for NPC/item script dialogs (<c>0x63</c>) and thin core UI triggers (<c>0x84</c>, <c>0x88</c>).
/// </summary>
public interface IDialogOutboundSink
{
    void SendDialogSelectMenu(string promptUtf8, IReadOnlyList<string> optionsUtf8, bool isRichDescribe = false) { }

    void SendDialogTalkPages(IReadOnlyList<string> pagesUtf8, bool expectCompletionCallback) { }

    void SendOpenSaleBox(int shopId, int scalePercent = 100, byte currencyType = 0) { }

    void SendOpenStoreBox() { }
}
```

```csharp
using System.Diagnostics.CodeAnalysis;

namespace JXReborn.ZoneEngine.Scripting;

public interface IZoneScriptHost
{
    long CurrentGeneration { get; }

    bool TryGetNpcDialogScript(string scriptCode, [NotNullWhen(true)] out INpcDialogScript? script);

    bool TryGetItemUsageScript(string scriptCode, [NotNullWhen(true)] out IItemUsageScript? script);

    bool TryGetTrapScript(string scriptCode, [NotNullWhen(true)] out ITrapScript? script);

    bool TryCreateMissionController(string missionCode, [NotNullWhen(true)] out IMissionController? controller);
}
```

---

## 3. Fluent Authoring Showcase (`TownTeleporterDialogScript` — *Xa Phu*)

```csharp
using System.Threading;
using System.Threading.Tasks;
using JXReborn.ZoneEngine.Scripting;

namespace JXReborn.Scripts.Dialogs;

public sealed class TownTeleporterDialogScript : INpcDialogScript
{
    private const int TravelFareGold = 500;

    public string ScriptCode => "npc-town-teleporter";

    public ValueTask OnDialogAsync(IScriptDialogContext context, CancellationToken cancellationToken = default)
    {
        new FluentDialogBuilder(context)
            .Prompt("Xa Phu: Đại hiệp muốn đi đâu? Phí dịch chuyển giữa các thành trấn là 500 lượng.")
            .Option("Đi Ba Lăng Huyện (500 lượng)", ctx => ConfirmDestinationAsync(ctx, "Ba Lăng Huyện", 53, 51200, 102400))
            .Option("Đi Tương Dương Phủ (500 lượng)", ctx => ConfirmDestinationAsync(ctx, "Tương Dương Phủ", 78, 50432, 101376))
            .CloseOption("Ta chỉ ghé ngang qua thôi")
            .SendSay();

        return ValueTask.CompletedTask;
    }

    private static ValueTask ConfirmDestinationAsync(
        IScriptDialogContext context,
        string destinationNameUtf8,
        ushort targetMapId,
        int targetMpsX,
        int targetMpsY)
    {
        if (context.MapId == targetMapId)
        {
            context.SendSystemMessage($"Ngươi đang ở {destinationNameUtf8} rồi!");
            return ValueTask.CompletedTask;
        }

        // Demonstrates multi-step nested sub-menu (preserved because pending state clears BEFORE callback runs!)
        new FluentDialogBuilder(context)
            .Prompt($"Xa Phu: Xác nhận khởi hành tới {destinationNameUtf8} với giá {TravelFareGold} lượng?")
            .Option("Khởi hành ngay", async ctx =>
            {
                if (!ctx.TryPayMoney(TravelFareGold))
                {
                    ctx.SendSystemMessage("Ngươi không mang đủ 500 lượng bạc lộ phí!");
                    return;
                }

                await ctx.TeleportPlayerAsync(targetMapId, targetMpsX, targetMpsY, targetFightMode: 0);
            })
            .CloseOption("Để ta suy nghĩ lại")
            .SendSay();

        return ValueTask.CompletedTask;
    }
}
```
