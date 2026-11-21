using JXReborn.ClientDriver;
using JXReborn.DualParityServer.Diffing;
using JXReborn.DualParityServer.Mirroring;
using JXReborn.DualParityServer.Session;

namespace JXReborn.DualParityServer.Automation;

/// <summary>
/// Runs declarative <see cref="ParityScenario"/> journeys in lockstep across Legacy and Reborn
/// with barrier synchronization and per-step semantic diffing.
/// </summary>
public sealed class ScenarioRunner
{
    private readonly DualSessionManager _session;
    private readonly NpcDialogCrawler _crawler;

    public ScenarioRunner(DualSessionManager session, NpcDialogCrawler crawler)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(crawler);
        _session = session;
        _crawler = crawler;
    }

    public async Task<IReadOnlyList<StepComparisonResult>> RunAsync(
        ParityScenario scenario,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(scenario);
        if (!_session.BothClientsConnected)
        {
            throw new InvalidOperationException("Both Legacy and Reborn clients must be connected before running a scenario.");
        }

        IDualTargetClient legacy = _session.LegacyClient!;
        IDualTargetClient reborn = _session.RebornClient!;
        var results = new List<StepComparisonResult>();
        var actionHistory = new List<string>();
        string? currentNpc = null;

        foreach (ScenarioStep step in scenario.Steps)
        {
            cancellationToken.ThrowIfCancellationRequested();
            (WorldDomSnapshot legacyBefore, WorldDomSnapshot rebornBefore) = await _session.RefreshSnapshotsAsync(cancellationToken);

            string type = step.Type.Trim().ToLowerInvariant();
            string summary = step.Name;

            switch (type)
            {
                case "moveto":
                    int mx = step.X ?? throw new InvalidOperationException($"Step '{step.Name}' requires 'x'.");
                    int my = step.Y ?? throw new InvalidOperationException($"Step '{step.Name}' requires 'y'.");
                    await Task.WhenAll(
                        legacy.MoveToAsync(mx, my, run: true, cancellationToken),
                        reborn.MoveToAsync(mx, my, run: true, cancellationToken));
                    summary = $"MoveTo({mx}, {my})";
                    break;

                case "interactnpc":
                    string npcName = step.NpcName ?? throw new InvalidOperationException($"Step '{step.Name}' requires 'npcName'.");
                    currentNpc = npcName;
                    NpcSnapshot? legacyNpc = InteractiveMirrorEngine.FindMatchingNpc(legacyBefore.Npcs, npcName, legacyBefore.Player.MpsX, legacyBefore.Player.MpsY);
                    NpcSnapshot? rebornNpc = InteractiveMirrorEngine.FindMatchingNpc(rebornBefore.Npcs, npcName, rebornBefore.Player.MpsX, rebornBefore.Player.MpsY);

                    var interactTasks = new List<Task>();
                    if (legacyNpc is not null)
                    {
                        interactTasks.Add(legacy.InteractNpcByDwIdAsync(legacyNpc.DwId, cancellationToken));
                    }

                    if (rebornNpc is not null)
                    {
                        interactTasks.Add(reborn.InteractNpcByDwIdAsync(rebornNpc.DwId, cancellationToken));
                    }

                    if (interactTasks.Count > 0)
                    {
                        await Task.WhenAll(interactTasks);
                    }

                    summary = $"InteractNpc('{npcName}')";
                    break;

                case "selectdialog":
                    int optIdx = step.OptionIndex ?? 0;
                    await Task.WhenAll(
                        legacy.SelectDialogAsync(optIdx, cancellationToken),
                        reborn.SelectDialogAsync(optIdx, cancellationToken));
                    summary = $"SelectDialog({optIdx})";
                    break;

                case "confirmtalk":
                    await Task.WhenAll(
                        legacy.ConfirmTalkAsync(cancellationToken),
                        reborn.ConfirmTalkAsync(cancellationToken));
                    summary = "ConfirmTalk()";
                    break;

                case "useitem":
                    int g = step.Genre ?? throw new InvalidOperationException($"Step '{step.Name}' requires 'genre'.");
                    int d = step.DetailType ?? throw new InvalidOperationException($"Step '{step.Name}' requires 'detailType'.");
                    int p = step.ParticularType ?? throw new InvalidOperationException($"Step '{step.Name}' requires 'particularType'.");

                    InventoryItemSnapshot? lItem = legacyBefore.Items.FirstOrDefault(i => i.Genre == g && i.DetailType == d && i.ParticularType == p);
                    InventoryItemSnapshot? rItem = rebornBefore.Items.FirstOrDefault(i => i.Genre == g && i.DetailType == d && i.ParticularType == p);

                    var useTasks = new List<Task>();
                    if (lItem is not null)
                    {
                        useTasks.Add(legacy.UseItemAsync(lItem.ArrayIndex, lItem.Place, lItem.GridX, lItem.GridY, cancellationToken));
                    }

                    if (rItem is not null)
                    {
                        useTasks.Add(reborn.UseItemAsync(rItem.ArrayIndex, rItem.Place, rItem.GridX, rItem.GridY, cancellationToken));
                    }

                    if (useTasks.Count > 0)
                    {
                        await Task.WhenAll(useTasks);
                    }

                    summary = $"UseItem({g},{d},{p})";
                    break;

                case "switchpk":
                    int pkMode = step.PkMode ?? 0;
                    await Task.WhenAll(
                        legacy.SwitchPkAsync(pkMode, cancellationToken),
                        reborn.SwitchPkAsync(pkMode, cancellationToken));
                    summary = $"SwitchPk({pkMode})";
                    break;

                case "sendchat":
                    int channel = step.Channel ?? 0;
                    string msg = step.Message ?? string.Empty;
                    await Task.WhenAll(
                        legacy.SendChatAsync(channel, msg, cancellationToken),
                        reborn.SendChatAsync(channel, msg, cancellationToken));
                    summary = $"SendChat(ch={channel}, '{msg}')";
                    break;

                case "wait":
                    await Task.Delay(step.WaitMs ?? _session.Options.SettleDelayMs, cancellationToken);
                    summary = $"Wait({step.WaitMs ?? _session.Options.SettleDelayMs}ms)";
                    break;

                case "crawlnearbynpcs":
                    IReadOnlyList<NpcParityScoreEntry> scores = await _crawler.CrawlNearbyNpcsAsync(
                        maxBranchesPerNpc: step.MaxBranches ?? 0,
                        npcNameFilter: step.NpcName,
                        cancellationToken: cancellationToken);
                    summary = $"CrawlNearbyNpcs(count={scores.Count})";
                    actionHistory.Add(summary);
                    continue;

                case "assertparity":
                    summary = "AssertParity()";
                    break;

                default:
                    throw new InvalidOperationException($"Unsupported scenario step type '{step.Type}'.");
            }

            actionHistory.Add(summary);
            int settleMs = step.WaitMs ?? _session.Options.SettleDelayMs;
            if (settleMs > 0)
            {
                await Task.Delay(settleMs, cancellationToken);
            }

            StepComparisonResult stepResult = await _session.CompareCurrentStateAsync(
                stepName: $"{scenario.Name} :: {step.Name}",
                actionSummary: summary,
                legacyBefore: legacyBefore,
                rebornBefore: rebornBefore,
                npcName: currentNpc,
                actions: [.. actionHistory],
                cancellationToken: cancellationToken);
            results.Add(stepResult);
        }

        return results;
    }
}
