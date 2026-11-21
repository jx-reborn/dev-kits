using System.Globalization;
using JXReborn.ClientDriver;
using JXReborn.DualParityServer.Diffing;
using JXReborn.DualParityServer.Session;

namespace JXReborn.DualParityServer.Mirroring;

/// <summary>
/// Watches player actions performed manually on <c>game.exe #1</c> (<c>legacy-linux</c>), translates them
/// into semantic intents (move, click NPC by name, select dialog option, confirm talk, switch PK),
/// replays them onto <c>game.exe #2</c> (<c>reborn-server</c>), and triggers live parity diffs.
/// </summary>
public sealed class InteractiveMirrorEngine : IAsyncDisposable
{
    private const int OpcodeSelectDialog = 0x5F;
    private const int OpcodeNpcDialog = 0x5E;
    private const int OpcodeUseItem = 0x59;

    private readonly DualSessionManager _session;
    private readonly Lock _runLock = new();
    private CancellationTokenSource? _cts;
    private Task? _loopTask;
    private string _statusMessage = "Idle";

    public InteractiveMirrorEngine(DualSessionManager session)
    {
        ArgumentNullException.ThrowIfNull(session);
        _session = session;
    }

    public bool IsRunning => _loopTask is { IsCompleted: false };

    public string StatusMessage => _statusMessage;

    public void Start()
    {
        lock (_runLock)
        {
            if (IsRunning)
            {
                return;
            }

            if (!_session.BothClientsConnected)
            {
                throw new InvalidOperationException("Connect both Legacy and Reborn clients before starting Interactive Mirror.");
            }

            _cts = new CancellationTokenSource();
            _statusMessage = "Mirroring actions from Legacy -> Reborn";
            _loopTask = Task.Run(() => RunLoopAsync(_cts.Token), _cts.Token);
        }
    }

    public async Task StopAsync()
    {
        CancellationTokenSource? cts;
        Task? loopTask;
        lock (_runLock)
        {
            cts = _cts;
            loopTask = _loopTask;
            _cts = null;
            _loopTask = null;
            _statusMessage = "Stopped";
        }

        if (cts is not null)
        {
            await cts.CancelAsync();
            if (loopTask is not null)
            {
                try
                {
                    await loopTask;
                }
                catch (OperationCanceledException)
                {
                    // Expected on stop.
                }
            }

            cts.Dispose();
        }
    }

    private async Task RunLoopAsync(CancellationToken cancellationToken)
    {
        try
        {
            (WorldDomSnapshot prevLegacy, WorldDomSnapshot prevReborn) = await _session.RefreshSnapshotsAsync(cancellationToken);
            uint lastC2sSeq = prevLegacy.RecentC2s.Count > 0 ? prevLegacy.RecentC2s[^1].Seq : 0;

            while (!cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(_session.Options.MirrorPollIntervalMs, cancellationToken);

                IDualTargetClient? legacyClient = _session.LegacyClient;
                IDualTargetClient? rebornClient = _session.RebornClient;
                if (legacyClient is null || rebornClient is null)
                {
                    break;
                }

                WorldDomSnapshot curLegacy = await legacyClient.GetStateAsync(cancellationToken);
                List<C2sPacketSnapshot> newC2s = curLegacy.RecentC2s
                    .Where(p => p.Seq > lastC2sSeq)
                    .OrderBy(p => p.Seq)
                    .ToList();
                if (newC2s.Count > 0)
                {
                    lastC2sSeq = newC2s[^1].Seq;
                }

                bool mirroredSomething = false;
                string? actionSummary = null;
                string? interactedNpcName = null;

                // 1. Detect dialog option selection (C2S 0x5F)
                C2sPacketSnapshot? selectPacket = newC2s.FirstOrDefault(p => p.Opcode == OpcodeSelectDialog);
                if (selectPacket is not null && TryParseSelectDialogOption(selectPacket.HexPreview, out int optIndex))
                {
                    await rebornClient.SelectDialogAsync(optIndex, cancellationToken);
                    mirroredSomething = true;
                    actionSummary = $"SelectDialog({optIndex})";
                }
                // 2. Detect NPC interaction (C2S 0x5E or dialog newly opened)
                else if (curLegacy.Dialog.IsOpen && (!prevLegacy.Dialog.IsOpen || curLegacy.Dialog.Sequence != prevLegacy.Dialog.Sequence))
                {
                    NpcSnapshot? legacyNpc = ResolveInteractedNpc(curLegacy, newC2s);
                    if (legacyNpc is not null)
                    {
                        interactedNpcName = legacyNpc.Name;
                        WorldDomSnapshot rebornNow = await rebornClient.GetStateAsync(cancellationToken);
                        NpcSnapshot? rebornNpc = FindMatchingNpc(rebornNow.Npcs, legacyNpc.Name, legacyNpc.MpsX, legacyNpc.MpsY);
                        if (rebornNpc is not null)
                        {
                            await rebornClient.InteractNpcByDwIdAsync(rebornNpc.DwId, cancellationToken);
                            mirroredSomething = true;
                            actionSummary = $"InteractNpc('{legacyNpc.Name}')";
                        }
                        else
                        {
                            mirroredSomething = true;
                            actionSummary = $"InteractNpc('{legacyNpc.Name}' — NOT FOUND on Reborn)";
                        }
                    }
                }
                // 3. Detect Talk confirmation (IsTalk dialog closed)
                else if (prevLegacy.Dialog.IsOpen && prevLegacy.Dialog.IsTalk && !curLegacy.Dialog.IsOpen)
                {
                    await rebornClient.ConfirmTalkAsync(cancellationToken);
                    mirroredSomething = true;
                    actionSummary = "ConfirmTalk()";
                }
                // 4. Detect PK mode switch
                else if (curLegacy.Player.PkFlag != prevLegacy.Player.PkFlag)
                {
                    await rebornClient.SwitchPkAsync(curLegacy.Player.PkFlag, cancellationToken);
                    mirroredSomething = true;
                    actionSummary = $"SwitchPk({curLegacy.Player.PkFlag})";
                }
                // 5. Detect item use (C2S 0x59)
                else if (newC2s.Any(p => p.Opcode == OpcodeUseItem))
                {
                    InventoryItemSnapshot? usedItem = FindConsumedItem(prevLegacy.Items, curLegacy.Items);
                    if (usedItem is not null)
                    {
                        WorldDomSnapshot rebornNow = await rebornClient.GetStateAsync(cancellationToken);
                        InventoryItemSnapshot? rebornItem = rebornNow.Items.FirstOrDefault(i =>
                            i.Genre == usedItem.Genre &&
                            i.DetailType == usedItem.DetailType &&
                            i.ParticularType == usedItem.ParticularType);
                        if (rebornItem is not null)
                        {
                            await rebornClient.UseItemAsync(rebornItem.ArrayIndex, rebornItem.Place, rebornItem.GridX, rebornItem.GridY, cancellationToken);
                            mirroredSomething = true;
                            actionSummary = $"UseItem('{usedItem.Name}', {usedItem.Genre},{usedItem.DetailType},{usedItem.ParticularType})";
                        }
                    }
                }
                // 6. Detect significant movement
                else if (curLegacy.Player.MapId == prevLegacy.Player.MapId)
                {
                    int dx = Math.Abs(curLegacy.Player.MpsX - prevLegacy.Player.MpsX);
                    int dy = Math.Abs(curLegacy.Player.MpsY - prevLegacy.Player.MpsY);
                    if (Math.Max(dx, dy) >= 48)
                    {
                        await rebornClient.MoveToAsync(curLegacy.Player.MpsX, curLegacy.Player.MpsY, run: curLegacy.Player.RunStatus != 0, cancellationToken);
                        mirroredSomething = true;
                        actionSummary = $"MoveTo({curLegacy.Player.MpsX}, {curLegacy.Player.MpsY})";
                    }
                }

                if (mirroredSomething && actionSummary is not null)
                {
                    _statusMessage = $"Mirrored: {actionSummary}";
                    await Task.Delay(_session.Options.SettleDelayMs, cancellationToken);

                    StepComparisonResult diff = await _session.CompareCurrentStateAsync(
                        stepName: $"Mirror: {actionSummary}",
                        actionSummary: actionSummary,
                        legacyBefore: prevLegacy,
                        rebornBefore: prevReborn,
                        npcName: interactedNpcName,
                        actions: [actionSummary],
                        cancellationToken: cancellationToken);

                    prevLegacy = _session.LastLegacySnapshot ?? curLegacy;
                    prevReborn = _session.LastRebornSnapshot ?? prevReborn;
                    _statusMessage = diff.Passed
                        ? $"OK: {actionSummary}"
                        : $"DIVERGENCE ({diff.CriticalCount} crit, {diff.WarningCount} warn): {actionSummary}";
                }
                else
                {
                    prevLegacy = curLegacy;
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Normal stop.
        }
        catch (Exception ex)
        {
            _statusMessage = $"Mirror error: {ex.Message}";
        }
    }

    internal static bool TryParseSelectDialogOption(string hexPreview, out int optionIndex)
    {
        optionIndex = -1;
        if (string.IsNullOrWhiteSpace(hexPreview))
        {
            return false;
        }

        string clean = hexPreview.Replace(" ", string.Empty, StringComparison.Ordinal).Trim();
        // c2s_playerselui (0x5F): byte[0]=0x5F, byte[1]=optionIndex (or signed byte / int)
        if (clean.Length < 4)
        {
            return false;
        }

        if (byte.TryParse(clean.AsSpan(2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out byte idx))
        {
            optionIndex = idx;
            return true;
        }

        return false;
    }

    internal static NpcSnapshot? FindMatchingNpc(IReadOnlyList<NpcSnapshot> npcs, string targetName, int nearX, int nearY)
    {
        return npcs
            .Where(n => !n.IsSelf && string.Equals(n.Name.Trim(), targetName.Trim(), StringComparison.Ordinal))
            .OrderBy(n => DistanceSq(n.MpsX, n.MpsY, nearX, nearY))
            .FirstOrDefault();
    }

    private static NpcSnapshot? ResolveInteractedNpc(WorldDomSnapshot snapshot, IReadOnlyList<C2sPacketSnapshot> newC2s)
    {
        // Try parsing target dwId from C2S 0x5E (byte[0]=0x5E, bytes[1..4]=uint32 dwId little-endian)
        C2sPacketSnapshot? npcClick = newC2s.LastOrDefault(p => p.Opcode == OpcodeNpcDialog);
        if (npcClick is not null)
        {
            string clean = npcClick.HexPreview.Replace(" ", string.Empty, StringComparison.Ordinal).Trim();
            if (clean.Length >= 10 && TryReadUInt32Le(clean.AsSpan(2, 8), out uint dwId))
            {
                NpcSnapshot? byId = snapshot.Npcs.FirstOrDefault(n => !n.IsSelf && n.DwId == dwId);
                if (byId is not null)
                {
                    return byId;
                }
            }
        }

        // Fallback: closest dialog NPC (Kind == 3) to the player
        return snapshot.Npcs
            .Where(n => !n.IsSelf && n.Kind == 3)
            .OrderBy(n => DistanceSq(n.MpsX, n.MpsY, snapshot.Player.MpsX, snapshot.Player.MpsY))
            .FirstOrDefault();
    }

    private static InventoryItemSnapshot? FindConsumedItem(
        IReadOnlyList<InventoryItemSnapshot> before,
        IReadOnlyList<InventoryItemSnapshot> after)
    {
        foreach (InventoryItemSnapshot oldItem in before)
        {
            InventoryItemSnapshot? current = after.FirstOrDefault(i => i.ServerItemId == oldItem.ServerItemId);
            if (current is null || current.StackCount < oldItem.StackCount)
            {
                return oldItem;
            }
        }

        return null;
    }

    private static bool TryReadUInt32Le(ReadOnlySpan<char> hex8, out uint value)
    {
        value = 0;
        if (hex8.Length != 8)
        {
            return false;
        }

        if (!byte.TryParse(hex8[0..2], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out byte b0) ||
            !byte.TryParse(hex8[2..4], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out byte b1) ||
            !byte.TryParse(hex8[4..6], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out byte b2) ||
            !byte.TryParse(hex8[6..8], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out byte b3))
        {
            return false;
        }

        value = (uint)(b0 | (b1 << 8) | (b2 << 16) | (b3 << 24));
        return true;
    }

    private static long DistanceSq(int x1, int y1, int x2, int y2)
    {
        long dx = x1 - x2;
        long dy = y1 - y2;
        return (dx * dx) + (dy * dy);
    }

    public async ValueTask DisposeAsync() => await StopAsync();
}
