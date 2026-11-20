# Quickstart & Verification Guide: Gameplay Scripting Host (`012`)

**Feature Branch**: `012-core-csharp-scripting-host`  
**Date**: 2026-11-11  
**Spec**: [spec.md](spec.md) | **Plan**: [plan.md](plan.md)

---

## 1. Build & Zero-Warning Gate

From the repository root, compile the entire solution (including `JXReborn.Scripts` and `JXReborn.DataExporter`) with warnings-as-errors enabled:

```powershell
dotnet build JXReborn.slnx -c Release -warnaserror
```

Expected outcome: `0 Warning(s)`, `0 Error(s)`.

---

## 2. Three-Tier Automated Verification Suite

### Tier 1 — Symmetric Wire Protocol & Framing Gate (`JXReborn.Protocol.Tests`)

Verifies `ProtocolSizeTable` (`c2s_playerselui` = `17` bytes), `DialogNpcC2SPacket` (`0x6E`, `9` bytes + `0x506C` check key), `PlayerSelectUiC2SPacket` (`0x5F`, `17` bytes), `ScriptActionS2CPacket` (`0x63` `Say`, `Describe`, and `Talk` modes with Vietnamese UTF-8 $\leftrightarrow$ `TCVN3` transcoding), and `PlayerEatItemC2SPacket` (`0x59`, `16` bytes `TargetId` preservation) against the exact byte vectors in [dialog-and-script-wire-protocol.md](contracts/dialog-and-script-wire-protocol.md):

```powershell
dotnet test tests/JXReborn.Protocol.Tests/JXReborn.Protocol.Tests.csproj --filter "FullyQualifiedName~ScriptingDialogPacketTests"
```

---

### Tier 2 — Deterministic Zone Simulation & Collectible Hot-Reload Gate (`JXReborn.ZoneEngine.Tests`)

#### A. Multi-Step Nested NPC Dialog, `MagicScript` Items, Custom Traps & Fault Containment

`ScriptingZoneHostTests` drives `ZoneSimulationHarness` with `ScriptingZoneModule` and `ZoneScriptHost`:
1. Spawns *Xa Phu* (`ScriptCode = "npc-town-teleporter"`, `DialogRadiusCells = 4` $\Rightarrow$ `256` Mps gate) on Ba Lăng Huyện (`MapId = 53`).
2. Verifies a player at `300` Mps is rejected, then walks within `180` Mps and dispatches `DialogNpcZoneCommand`.
3. Selects a destination in Menu 1 $\rightarrow$ verifies `PlayerDialogFeatureSlot` clears Menu 1 **before** running the callback so Menu 2 (confirmation sub-menu) remains active $\rightarrow$ selects Confirm (`SelectIndex = 0`) $\rightarrow$ verifies `500` gold deduction and cross-town relocation.
4. Exercises `Talk` multi-page monologues with `onCompleted` continuation, `ItemUsageResult.ConsumeOne` vs. `KeepItem` (and exception preservation), `TaskValues` vs. `TempTaskValues` detach wipe, `PortalTrapActionKind.CustomScript`, `MissionZoneTickSystem` timer intervals, and Player A script exception isolation while Player B ticks normally.

```powershell
dotnet test tests/JXReborn.ZoneEngine.Tests/JXReborn.ZoneEngine.Tests.csproj --filter "FullyQualifiedName~ScriptingZoneHostTests|FullyQualifiedName~TestGmTokenItemScriptTests"
```

#### B. Collectible `AssemblyLoadContext` Zero-Leak Unload Proof

`ScriptHotReloadUnloadabilityTests` compiles an in-memory v1 script `.dll` into a `byte[]`, loads it via `CollectibleScriptAssemblyLoader` (`MemoryStream`), executes a player dialog to completion, hot-swaps a v2 script `.dll` via `ZoneScriptHost.ReloadFromBytes(...)`, and asserts deterministic garbage collection of the v1 `AssemblyLoadContext`:

```csharp
// Critical pattern: keep ALC creation/execution in a [MethodImpl(MethodImplOptions.NoInlining)] helper
// so JIT stack slots do not artificially root the collectible AssemblyLoadContext during test assertions.
WeakReference v1AlcWeakRef = LoadExecuteAndSwapV1ToV2(scriptHost, harness);

for (int i = 0; i < 5 && v1AlcWeakRef.IsAlive; i++)
{
    GC.Collect();
    GC.WaitForPendingFinalizers();
}

Assert.False(v1AlcWeakRef.IsAlive);
```

Run the unloadability test:

```powershell
dotnet test tests/JXReborn.ZoneEngine.Tests/JXReborn.ZoneEngine.Tests.csproj --filter "FullyQualifiedName~ScriptHotReloadUnloadabilityTests"
```

---

### Tier 3 — Live Encrypted TCP Headless Bot Integration Gate (`JXReborn.Integration.Tests`)

`NpcDialogAndItemScriptIntegrationTests` spins up the full `Bishop` (`5622`) + `GameServer` (`6666`) pipeline with stream encryption and connects a headless `jx-swarm`-compatible bot client:
1. Completes `0x41` / `0x64` / `0x42` / `0x43` world login handshake (recording `SyncEndToken1` and `SyncEndToken2`).
2. Walks within `256` Mps of *Xa Phu*, computes `CheckValue` with key `0x506C`, and transmits encrypted 9-byte `0x6E` (`c2s_dialognpc`).
3. Decrypts incoming `0x63` (`s2c_scriptaction`), verifies TCVN3 prompt and option strings, transmits 17-byte `0x5F` (`c2s_playerselui`) through nested confirmation menus, and verifies gold sync (`0x68`) + teleport sync.
4. Activates a `MagicScript` gift item via 16-byte `0x59` (`c2s_playereatitem`), verifying reward item sync (`0x5F` `s2c_syncitem`), item consumption (`0x60` `s2c_removeitem`), and persistent `TaskValues` survival across disconnect and re-login.

```powershell
dotnet test tests/JXReborn.Integration.Tests/JXReborn.Integration.Tests.csproj --filter "FullyQualifiedName~NpcDialogAndItemScriptIntegrationTests"
```

---

### Tier 4 — Live Zero-Sim Containerized Differential Parity Gate (`JXReborn.ClientDriver.Tests` — Journey `J03`)

Verifies real `game.exe` interaction against *Xa Phu* side-by-side on the ephemeral `jx-runtime-standalone` (`--tmpfs /var/lib/mysql`) Oracle and `JXReborn` (covering out-of-range rejection, authentic `station.lua:451` `Level < 5` `Talk` guard, symmetric `Lv10` promotion via GM Token `(6, 1, 4258)`, `0x6E` → `0x63` `Say` menu, and unfunded travel rejection):

```powershell
.\tools\run-parity.ps1 -Profile minimal -Filter "DifferentialParityMovementAndDialogTests"
```

---

## 3. Authoring a New C# 14 Gameplay Script in 60 Seconds

1. Create a `sealed` class in `src/JXReborn.Scripts/Dialogs/` (one type per file) implementing `INpcDialogScript`:
   ```csharp
   namespace JXReborn.Scripts.Dialogs;

   public sealed class VillageElderDialogScript : INpcDialogScript
   {
       public string ScriptCode => "npc-village-elder";

       public ValueTask OnDialogAsync(IScriptDialogContext context, CancellationToken cancellationToken = default)
       {
           new FluentDialogBuilder(context)
               .Prompt($"Chưởng thôn: Chào mừng {context.RoleNameUtf8} bước vào giang hồ!")
               .Option("Nhận chỉ dẫn tân thủ", ctx =>
               {
                   ctx.SetTask(taskId: 100, value: 1);
                   ctx.SendSystemMessage("Đã ghi nhận nhiệm vụ tân thủ!");
                   return ValueTask.CompletedTask;
               })
               .CloseOption("Đa tạ tiền bối")
               .SendSay();

           return ValueTask.CompletedTask;
       }
   }
   ```
2. Bind `"scriptCode": "npc-village-elder"` on the NPC spawn entry in `data/maps/*.map.json` and re-export `jxreborn-data.pack` (or drop a compiled script `.dll` into the configured hot-reload directory while the server is running).
