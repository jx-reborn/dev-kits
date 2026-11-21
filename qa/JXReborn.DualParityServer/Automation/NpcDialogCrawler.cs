using JXReborn.ClientDriver;
using JXReborn.DualParityServer.Diffing;
using JXReborn.DualParityServer.Mirroring;
using JXReborn.DualParityServer.Session;

namespace JXReborn.DualParityServer.Automation;

/// <summary>
/// Automated NPC dialog crawler that iterates visible dialog NPCs (<c>Kind == 3</c>), interacts with each
/// on both <c>legacy-linux</c> and <c>reborn-server</c>, diffs prompt + menu options + optional branches,
/// resyncs dialogs between NPCs, and populates the per-NPC parity scorecard.
/// </summary>
public sealed class NpcDialogCrawler
{
    private readonly DualSessionManager _session;

    public NpcDialogCrawler(DualSessionManager session)
    {
        ArgumentNullException.ThrowIfNull(session);
        _session = session;
    }

    public async Task<IReadOnlyList<NpcParityScoreEntry>> CrawlNearbyNpcsAsync(
        int maxBranchesPerNpc = 0,
        string? npcNameFilter = null,
        CancellationToken cancellationToken = default)
    {
        if (!_session.BothClientsConnected)
        {
            throw new InvalidOperationException("Both Legacy and Reborn clients must be connected before crawling NPCs.");
        }

        IDualTargetClient legacy = _session.LegacyClient!;
        IDualTargetClient reborn = _session.RebornClient!;

        (WorldDomSnapshot initialLegacy, _) = await _session.RefreshSnapshotsAsync(cancellationToken);

        List<NpcSnapshot> targetNpcs = initialLegacy.Npcs
            .Where(n => !n.IsSelf && n.Kind == 3 && !string.IsNullOrWhiteSpace(n.Name))
            .Where(n => string.IsNullOrWhiteSpace(npcNameFilter) || n.Name.Contains(npcNameFilter, StringComparison.OrdinalIgnoreCase))
            .GroupBy(n => n.Name.Trim(), StringComparer.Ordinal)
            .Select(g => g.First())
            .OrderBy(n => n.Name, StringComparer.Ordinal)
            .ToList();

        var results = new List<NpcParityScoreEntry>();

        foreach (NpcSnapshot legacyNpc in targetNpcs)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await TryCloseOpenDialogsAsync(legacy, reborn, cancellationToken);

            (WorldDomSnapshot beforeLegacy, WorldDomSnapshot beforeReborn) = await _session.RefreshSnapshotsAsync(cancellationToken);
            NpcSnapshot? rebornNpc = InteractiveMirrorEngine.FindMatchingNpc(
                beforeReborn.Npcs,
                legacyNpc.Name,
                legacyNpc.MpsX,
                legacyNpc.MpsY);

            int critTotal = 0;
            int warnTotal = 0;
            int branchesTested = 0;

            if (rebornNpc is null)
            {
                // Still click Legacy so the diff captures what Legacy would have opened
                await legacy.InteractNpcByDwIdAsync(legacyNpc.DwId, cancellationToken);
                await DelaySettleAsync(cancellationToken);

                StepComparisonResult missingDiff = await _session.CompareCurrentStateAsync(
                    stepName: $"NPC Crawl :: {legacyNpc.Name} (Open)",
                    actionSummary: $"InteractNpc('{legacyNpc.Name}' — missing on Reborn)",
                    legacyBefore: beforeLegacy,
                    rebornBefore: beforeReborn,
                    npcName: legacyNpc.Name,
                    actions: [$"InteractNpc('{legacyNpc.Name}')"],
                    cancellationToken: cancellationToken);

                critTotal += Math.Max(1, missingDiff.CriticalCount);
                warnTotal += missingDiff.WarningCount;

                var missingEntry = new NpcParityScoreEntry(
                    MapId: beforeLegacy.Player.MapId,
                    NpcName: legacyNpc.Name,
                    MpsX: legacyNpc.MpsX,
                    MpsY: legacyNpc.MpsY,
                    Matched: false,
                    BranchesTested: 0,
                    CriticalFindings: critTotal,
                    WarningFindings: warnTotal,
                    Summary: "NPC missing on Reborn");
                _session.RecordNpcScore(missingEntry);
                results.Add(missingEntry);
                await TryCloseOpenDialogsAsync(legacy, reborn, cancellationToken);
                continue;
            }

            await Task.WhenAll(
                legacy.InteractNpcByDwIdAsync(legacyNpc.DwId, cancellationToken),
                reborn.InteractNpcByDwIdAsync(rebornNpc.DwId, cancellationToken));
            await DelaySettleAsync(cancellationToken);

            StepComparisonResult openDiff = await _session.CompareCurrentStateAsync(
                stepName: $"NPC Crawl :: {legacyNpc.Name} (Root Dialog)",
                actionSummary: $"InteractNpc('{legacyNpc.Name}')",
                legacyBefore: beforeLegacy,
                rebornBefore: beforeReborn,
                npcName: legacyNpc.Name,
                actions: [$"InteractNpc('{legacyNpc.Name}')"],
                cancellationToken: cancellationToken);

            critTotal += openDiff.CriticalCount;
            warnTotal += openDiff.WarningCount;

            WorldDomSnapshot? afterOpenLegacy = _session.LastLegacySnapshot;
            WorldDomSnapshot? afterOpenReborn = _session.LastRebornSnapshot;

            if (maxBranchesPerNpc > 0 &&
                afterOpenLegacy?.Dialog.IsOpen == true &&
                afterOpenReborn?.Dialog.IsOpen == true &&
                afterOpenLegacy.Dialog.Options.Count > 0)
            {
                int branchLimit = Math.Min(maxBranchesPerNpc, Math.Min(afterOpenLegacy.Dialog.Options.Count, afterOpenReborn.Dialog.Options.Count));
                for (int branchIdx = 0; branchIdx < branchLimit; branchIdx++)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (branchIdx > 0)
                    {
                        await TryCloseOpenDialogsAsync(legacy, reborn, cancellationToken);
                        await Task.WhenAll(
                            legacy.InteractNpcByDwIdAsync(legacyNpc.DwId, cancellationToken),
                            reborn.InteractNpcByDwIdAsync(rebornNpc.DwId, cancellationToken));
                        await DelaySettleAsync(cancellationToken);
                    }

                    (WorldDomSnapshot preBranchL, WorldDomSnapshot preBranchR) = await _session.RefreshSnapshotsAsync(cancellationToken);
                    await Task.WhenAll(
                        legacy.SelectDialogAsync(branchIdx, cancellationToken),
                        reborn.SelectDialogAsync(branchIdx, cancellationToken));
                    await DelaySettleAsync(cancellationToken);

                    StepComparisonResult branchDiff = await _session.CompareCurrentStateAsync(
                        stepName: $"NPC Crawl :: {legacyNpc.Name} (Option {branchIdx})",
                        actionSummary: $"SelectDialog({branchIdx})",
                        legacyBefore: preBranchL,
                        rebornBefore: preBranchR,
                        npcName: legacyNpc.Name,
                        actions: [$"InteractNpc('{legacyNpc.Name}')", $"SelectDialog({branchIdx})"],
                        cancellationToken: cancellationToken);

                    branchesTested++;
                    critTotal += branchDiff.CriticalCount;
                    warnTotal += branchDiff.WarningCount;
                }
            }

            await TryCloseOpenDialogsAsync(legacy, reborn, cancellationToken);

            var entry = new NpcParityScoreEntry(
                MapId: beforeLegacy.Player.MapId,
                NpcName: legacyNpc.Name,
                MpsX: legacyNpc.MpsX,
                MpsY: legacyNpc.MpsY,
                Matched: critTotal == 0,
                BranchesTested: branchesTested,
                CriticalFindings: critTotal,
                WarningFindings: warnTotal,
                Summary: critTotal == 0
                    ? (warnTotal == 0 ? "100% Match" : $"Match ({warnTotal} warnings)")
                    : $"Diverged ({critTotal} critical, {warnTotal} warnings)");

            _session.RecordNpcScore(entry);
            results.Add(entry);
        }

        return results;
    }

    private async Task TryCloseOpenDialogsAsync(
        IDualTargetClient legacy,
        IDualTargetClient reborn,
        CancellationToken cancellationToken)
    {
        (WorldDomSnapshot lSnap, WorldDomSnapshot rSnap) = await _session.RefreshSnapshotsAsync(cancellationToken);
        var tasks = new List<Task>();

        if (lSnap.Dialog.IsOpen)
        {
            tasks.Add(lSnap.Dialog.IsTalk
                ? legacy.ConfirmTalkAsync(cancellationToken)
                : legacy.SelectDialogAsync(Math.Max(0, lSnap.Dialog.Options.Count - 1), cancellationToken));
        }

        if (rSnap.Dialog.IsOpen)
        {
            tasks.Add(rSnap.Dialog.IsTalk
                ? reborn.ConfirmTalkAsync(cancellationToken)
                : reborn.SelectDialogAsync(Math.Max(0, rSnap.Dialog.Options.Count - 1), cancellationToken));
        }

        if (tasks.Count > 0)
        {
            await Task.WhenAll(tasks);
            await DelaySettleAsync(cancellationToken);
        }
    }

    private Task DelaySettleAsync(CancellationToken cancellationToken) =>
        _session.Options.SettleDelayMs > 0
            ? Task.Delay(_session.Options.SettleDelayMs, cancellationToken)
            : Task.CompletedTask;
}
