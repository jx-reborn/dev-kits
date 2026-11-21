using System.Collections.Concurrent;
using JXReborn.ClientDriver;
using JXReborn.DualParityServer.Configuration;
using JXReborn.DualParityServer.Diffing;
using JXReborn.DualParityServer.Proxy;

namespace JXReborn.DualParityServer.Session;

/// <summary>
/// Central coordinator owning the Legacy &amp; Reborn client sessions, optional TCP proxy relays,
/// semantic diff history, findings ledger, and NPC parity scorecard.
/// </summary>
public sealed class DualSessionManager : IAsyncDisposable
{
    private readonly DualParityOptions _options;
    private readonly SemanticDiffEngine _diffEngine;
    private readonly Lock _stateLock = new();
    private readonly List<StepComparisonResult> _steps = [];
    private readonly List<ParityFinding> _findings = [];
    private readonly ConcurrentDictionary<string, NpcParityScoreEntry> _npcScores = new(StringComparer.Ordinal);

    private DualTcpProxyRelay? _legacyProxy;
    private DualTcpProxyRelay? _rebornProxy;
    private IDualTargetClient? _legacyClient;
    private IDualTargetClient? _rebornClient;
    private WorldDomSnapshot? _lastLegacySnapshot;
    private WorldDomSnapshot? _lastRebornSnapshot;
    private int _stepCounter;

    public DualSessionManager(
        DualParityOptions options,
        SemanticDiffEngine diffEngine,
        IDualTargetClient? legacyClient = null,
        IDualTargetClient? rebornClient = null)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(diffEngine);
        _options = options;
        _diffEngine = diffEngine;
        _legacyClient = legacyClient;
        _rebornClient = rebornClient;
        SessionId = $"dual-{DateTimeOffset.UtcNow:yyyyMMdd-HHmmss}";
        StartedAtUtc = DateTimeOffset.UtcNow;
    }

    public string SessionId { get; }

    public DateTimeOffset StartedAtUtc { get; }

    public DualParityOptions Options => _options;

    public bool BothClientsConnected => _legacyClient is not null && _rebornClient is not null;

    public IDualTargetClient? LegacyClient => _legacyClient;

    public IDualTargetClient? RebornClient => _rebornClient;

    public WorldDomSnapshot? LastLegacySnapshot => _lastLegacySnapshot;

    public WorldDomSnapshot? LastRebornSnapshot => _lastRebornSnapshot;

    public void StartProxiesIfConfigured()
    {
        if (_options.LegacyProxyPort > 0 && _legacyProxy is null)
        {
            _legacyProxy = new DualTcpProxyRelay(
                "legacy-linux",
                _options.LegacyProxyPort,
                _options.LegacyHost,
                _options.LegacyPort);
            _legacyProxy.Start();
        }

        if (_options.RebornProxyPort > 0 && _rebornProxy is null)
        {
            _rebornProxy = new DualTcpProxyRelay(
                "reborn-server",
                _options.RebornProxyPort,
                _options.RebornHost,
                _options.RebornPort);
            _rebornProxy.Start();
        }
    }

    public TcpProxyStats? GetLegacyProxyStats() => _legacyProxy?.GetStats();

    public TcpProxyStats? GetRebornProxyStats() => _rebornProxy?.GetStats();

    public async Task ConnectClientsAsync(CancellationToken cancellationToken = default)
    {
        int legacyConnectPort = _legacyProxy?.BoundPort ?? _options.LegacyPort;
        string legacyConnectHost = _legacyProxy is not null ? "127.0.0.1" : _options.LegacyHost;

        int rebornConnectPort = _rebornProxy?.BoundPort ?? _options.RebornPort;
        string rebornConnectHost = _rebornProxy is not null ? "127.0.0.1" : _options.RebornHost;

        if (_legacyClient is null)
        {
            JxHeadlessClient? legacy = await AcquireClientAsync(
                label: "legacy",
                pipeName: _options.LegacyPipeName,
                pid: _options.LegacyPid,
                spawn: _options.SpawnClients,
                gatewayHost: legacyConnectHost,
                gatewayPort: legacyConnectPort,
                cancellationToken);
            if (legacy is not null)
            {
                _legacyClient = new JxHeadlessTargetClient("legacy", legacy);
            }
        }

        if (_rebornClient is null)
        {
            JxHeadlessClient? reborn = await AcquireClientAsync(
                label: "reborn",
                pipeName: _options.RebornPipeName,
                pid: _options.RebornPid,
                spawn: _options.SpawnClients,
                gatewayHost: rebornConnectHost,
                gatewayPort: rebornConnectPort,
                cancellationToken);
            if (reborn is not null)
            {
                _rebornClient = new JxHeadlessTargetClient("reborn", reborn);
            }
        }

        if (BothClientsConnected)
        {
            await RefreshSnapshotsAsync(cancellationToken);
        }
    }

    public async Task AttachByPidAsync(int legacyPid, int rebornPid, CancellationToken cancellationToken = default)
    {
        JxHeadlessClient legacy = await JxHeadlessClient.AttachToProcessAsync(legacyPid, cancellationToken: cancellationToken);
        JxHeadlessClient reborn = await JxHeadlessClient.AttachToProcessAsync(rebornPid, cancellationToken: cancellationToken);

        await ReplaceClientsAsync(
            new JxHeadlessTargetClient("legacy", legacy),
            new JxHeadlessTargetClient("reborn", reborn));
        await RefreshSnapshotsAsync(cancellationToken);
    }

    public async Task AttachByPipeAsync(string legacyPipe, string rebornPipe, CancellationToken cancellationToken = default)
    {
        JxHeadlessClient legacy = await JxHeadlessClient.ConnectExistingAsync(legacyPipe, cancellationToken: cancellationToken);
        JxHeadlessClient reborn = await JxHeadlessClient.ConnectExistingAsync(rebornPipe, cancellationToken: cancellationToken);

        await ReplaceClientsAsync(
            new JxHeadlessTargetClient("legacy", legacy),
            new JxHeadlessTargetClient("reborn", reborn));
        await RefreshSnapshotsAsync(cancellationToken);
    }

    public async Task<(WorldDomSnapshot Legacy, WorldDomSnapshot Reborn)> RefreshSnapshotsAsync(CancellationToken cancellationToken = default)
    {
        EnsureConnected();
        Task<WorldDomSnapshot> legacyTask = _legacyClient!.GetStateAsync(cancellationToken);
        Task<WorldDomSnapshot> rebornTask = _rebornClient!.GetStateAsync(cancellationToken);
        await Task.WhenAll(legacyTask, rebornTask);

        _lastLegacySnapshot = legacyTask.Result;
        _lastRebornSnapshot = rebornTask.Result;
        return (_lastLegacySnapshot, _lastRebornSnapshot);
    }

    public async Task<StepComparisonResult> CompareCurrentStateAsync(
        string stepName,
        string actionSummary,
        WorldDomSnapshot? legacyBefore = null,
        WorldDomSnapshot? rebornBefore = null,
        string? npcName = null,
        IReadOnlyList<string>? actions = null,
        bool captureScreenshotsOnCritical = true,
        CancellationToken cancellationToken = default)
    {
        EnsureConnected();
        (WorldDomSnapshot legacyAfter, WorldDomSnapshot rebornAfter) = await RefreshSnapshotsAsync(cancellationToken);
        int stepIndex = Interlocked.Increment(ref _stepCounter);

        StepComparisonResult result = _diffEngine.Compare(
            stepIndex: stepIndex,
            stepName: stepName,
            actionSummary: actionSummary,
            legacyAfter: legacyAfter,
            rebornAfter: rebornAfter,
            legacyBefore: legacyBefore,
            rebornBefore: rebornBefore,
            npcName: npcName,
            actions: actions);

        if (captureScreenshotsOnCritical && result.CriticalCount > 0)
        {
            string imgDir = Path.Combine(_options.ReportOutputDir, SessionId, "screenshots");
            Directory.CreateDirectory(imgDir);
            string legacyPath = Path.Combine(imgDir, $"step-{stepIndex:D4}-legacy.png");
            string rebornPath = Path.Combine(imgDir, $"step-{stepIndex:D4}-reborn.png");

            Task<string?> legacyShot = _legacyClient!.TryCaptureScreenshotAsync(legacyPath, cancellationToken);
            Task<string?> rebornShot = _rebornClient!.TryCaptureScreenshotAsync(rebornPath, cancellationToken);
            await Task.WhenAll(legacyShot, rebornShot);

            if (legacyShot.Result is not null || rebornShot.Result is not null)
            {
                var artifacts = new FindingArtifacts(
                    LegacyScreenshot: legacyShot.Result,
                    RebornScreenshot: rebornShot.Result);
                List<ParityFinding> updatedFindings = result.Findings
                    .Select(f => f with { Artifacts = artifacts })
                    .ToList();
                result = result with { Findings = updatedFindings };
            }
        }

        RecordStepResult(result);
        return result;
    }

    public void RecordStepResult(StepComparisonResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        lock (_stateLock)
        {
            _steps.Add(result);
            _findings.AddRange(result.Findings);
        }
    }

    public void RecordNpcScore(NpcParityScoreEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        string key = $"{entry.MapId}::{entry.NpcName}";
        _npcScores[key] = entry;
    }

    public IReadOnlyList<StepComparisonResult> GetSteps()
    {
        lock (_stateLock)
        {
            return [.. _steps];
        }
    }

    public IReadOnlyList<ParityFinding> GetFindings()
    {
        lock (_stateLock)
        {
            return [.. _findings];
        }
    }

    public IReadOnlyList<NpcParityScoreEntry> GetNpcScorecard() =>
        _npcScores.Values
            .OrderBy(e => e.MapId)
            .ThenBy(e => e.NpcName, StringComparer.Ordinal)
            .ToArray();

    private void EnsureConnected()
    {
        if (!BothClientsConnected)
        {
            throw new InvalidOperationException("Both Legacy and Reborn clients must be connected first.");
        }
    }

    private async Task ReplaceClientsAsync(IDualTargetClient legacy, IDualTargetClient reborn)
    {
        if (_legacyClient is not null)
        {
            await _legacyClient.DisposeAsync();
        }

        if (_rebornClient is not null)
        {
            await _rebornClient.DisposeAsync();
        }

        _legacyClient = legacy;
        _rebornClient = reborn;
    }

    private async Task<JxHeadlessClient?> AcquireClientAsync(
        string label,
        string? pipeName,
        int? pid,
        bool spawn,
        string gatewayHost,
        int gatewayPort,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(pipeName))
        {
            return await JxHeadlessClient.ConnectExistingAsync(pipeName, cancellationToken: cancellationToken);
        }

        if (pid is > 0)
        {
            return await JxHeadlessClient.AttachToProcessAsync(pid.Value, cancellationToken: cancellationToken);
        }

        if (!spawn)
        {
            return null;
        }

        var spawnOptions = new JxHeadlessOptions
        {
            GatewayPort = gatewayPort,
            InteractiveWindow = _options.InteractiveWindows,
            PipeName = $"jx-dual-{label}-{Guid.NewGuid():N}"[..24]
        };

        JxHeadlessClient client = await JxHeadlessClient.SpawnAsync(spawnOptions, cancellationToken);
        await client.LoginAndEnterWorldAsync(
            ip: gatewayHost,
            port: gatewayPort,
            account: _options.Account,
            password: _options.Password,
            roleName: _options.RoleName,
            gender: _options.RoleGender,
            series: _options.RoleSeries,
            cancellationToken: cancellationToken);
        return client;
    }

    public async ValueTask DisposeAsync()
    {
        if (_legacyClient is not null)
        {
            await _legacyClient.DisposeAsync();
            _legacyClient = null;
        }

        if (_rebornClient is not null)
        {
            await _rebornClient.DisposeAsync();
            _rebornClient = null;
        }

        if (_legacyProxy is not null)
        {
            await _legacyProxy.DisposeAsync();
            _legacyProxy = null;
        }

        if (_rebornProxy is not null)
        {
            await _rebornProxy.DisposeAsync();
            _rebornProxy = null;
        }
    }
}
