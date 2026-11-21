using System.Text;
using JXReborn.DualParityServer.Configuration;

namespace JXReborn.DualParityServer.Standalone;

/// <summary>
/// Orchestrates booting, monitoring, log collection, and clean teardown of both
/// <c>legacy-linux</c> (container) and <c>reborn-server</c> (<c>JXReborn.Host</c> child process) in Standalone mode.
/// </summary>
public sealed class StandaloneStackOrchestrator : IAsyncDisposable
{
    private readonly LegacyContainerLauncher _legacyLauncher;
    private readonly RebornHostLauncher _rebornLauncher;

    public StandaloneStackOrchestrator(DualParityOptions options, string sessionId)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);
        _legacyLauncher = new LegacyContainerLauncher(options);
        _rebornLauncher = new RebornHostLauncher(options, sessionId);
    }

    public bool IsReady => _legacyLauncher.IsReady && _rebornLauncher.IsReady;

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        await Task.WhenAll(
            _legacyLauncher.StartAsync(cancellationToken),
            _rebornLauncher.StartAsync(cancellationToken));
    }

    public (ManagedServerStatus Legacy, ManagedServerStatus Reborn) GetStatuses() =>
        (_legacyLauncher.GetStatus(), _rebornLauncher.GetStatus());

    public async Task WriteServerLogsAsync(string reportOutputDirectory, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reportOutputDirectory);
        string logsDir = Path.Combine(reportOutputDirectory, "logs");
        Directory.CreateDirectory(logsDir);

        string legacyLogs = await _legacyLauncher.ReadContainerLogsAsync(tailLines: 200, cancellationToken);
        string rebornLogs = _rebornLauncher.GetAllLogs();

        await File.WriteAllTextAsync(Path.Combine(logsDir, "legacy-server.log"), legacyLogs, Encoding.UTF8, cancellationToken);
        await File.WriteAllTextAsync(Path.Combine(logsDir, "reborn-server.log"), rebornLogs, Encoding.UTF8, cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        await _rebornLauncher.DisposeAsync();
        await _legacyLauncher.DisposeAsync();
    }
}
