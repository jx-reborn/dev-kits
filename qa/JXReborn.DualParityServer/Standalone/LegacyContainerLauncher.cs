using System.Diagnostics;
using JXReborn.DualParityServer.Configuration;

namespace JXReborn.DualParityServer.Standalone;

/// <summary>
/// Manages the lifecycle of the containerized <c>legacy-linux</c> reference server via <c>qa/scripts/run-parity.ps1 -StartOnly</c>
/// and cleans up the container on shutdown unless <c>--keep-containers</c> is set.
/// </summary>
public sealed class LegacyContainerLauncher : IAsyncDisposable
{
    private readonly DualParityOptions _options;
    private readonly RingLogBuffer _logs = new(300);
    private string? _containerCli;
    private string _state = "Not started";
    private bool _isReady;
    private bool _startedByUs;

    public LegacyContainerLauncher(DualParityOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _options = options;
    }

    public bool IsReady => _isReady;

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        _state = "Starting legacy-linux container...";
        string scriptPath = ResolveRunParityScriptPath(_options.RunParityScriptPath);
        _containerCli = ResolveContainerCli();

        var args = new List<string>
        {
            "-NoProfile",
            "-ExecutionPolicy", "Bypass",
            "-File", scriptPath,
            "-Profile", _options.LegacyProfile,
            "-StartOnly",
            "-BishopPort", _options.LegacyPort.ToString(System.Globalization.CultureInfo.InvariantCulture),
            "-GameServerPort", _options.LegacyGameServerPort.ToString(System.Globalization.CultureInfo.InvariantCulture),
            "-ContainerName", _options.LegacyContainerName
        };

        if (_options.ReuseLegacyContainer)
        {
            args.Add("-ReuseRunning");
        }

        _logs.Append($"Running: powershell {string.Join(' ', args)}");

        var psi = new ProcessStartInfo
        {
            FileName = "powershell",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };
        foreach (string arg in args)
        {
            psi.ArgumentList.Add(arg);
        }

        using Process proc = Process.Start(psi)
            ?? throw new InvalidOperationException("Failed to launch PowerShell for run-parity.ps1.");

        proc.OutputDataReceived += (_, e) => { if (e.Data is not null) _logs.Append(e.Data); };
        proc.ErrorDataReceived += (_, e) => { if (e.Data is not null) _logs.Append($"[ERR] {e.Data}"); };
        proc.BeginOutputReadLine();
        proc.BeginErrorReadLine();

        await proc.WaitForExitAsync(cancellationToken);

        if (proc.ExitCode != 0)
        {
            _state = $"Failed (exit {proc.ExitCode})";
            string tail = string.Join(Environment.NewLine, _logs.GetLines(25));
            throw new InvalidOperationException(
                $"legacy-linux container startup failed (exit code {proc.ExitCode}). Recent output:{Environment.NewLine}{tail}");
        }

        _startedByUs = true;
        _isReady = true;
        _state = "Running";
    }

    public async Task<string> ReadContainerLogsAsync(int tailLines = 150, CancellationToken cancellationToken = default)
    {
        string launcherLogs = _logs.GetAllText();
        if (string.IsNullOrWhiteSpace(_containerCli))
        {
            return launcherLogs;
        }

        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = _containerCli,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            psi.ArgumentList.Add("logs");
            psi.ArgumentList.Add("--tail");
            psi.ArgumentList.Add(tailLines.ToString(System.Globalization.CultureInfo.InvariantCulture));
            psi.ArgumentList.Add(_options.LegacyContainerName);

            using Process? proc = Process.Start(psi);
            if (proc is null)
            {
                return launcherLogs;
            }

            string stdout = await proc.StandardOutput.ReadToEndAsync(cancellationToken);
            string stderr = await proc.StandardError.ReadToEndAsync(cancellationToken);
            await proc.WaitForExitAsync(cancellationToken);

            return $"{launcherLogs}{Environment.NewLine}--- Container Logs ({_options.LegacyContainerName}) ---{Environment.NewLine}{stdout}{stderr}";
        }
        catch (Exception)
        {
            return launcherLogs;
        }
    }

    public ManagedServerStatus GetStatus() => new(
        Name: "legacy-linux",
        Enabled: true,
        IsReady: _isReady,
        State: _state,
        Endpoint: $"{_options.LegacyHost}:{_options.LegacyPort} (GS {_options.LegacyGameServerPort})",
        Identifier: _options.LegacyContainerName,
        RecentLogs: _logs.GetLines(60));

    public static string ResolveRunParityScriptPath(string? configuredPath)
    {
        if (!string.IsNullOrWhiteSpace(configuredPath))
        {
            string full = Path.GetFullPath(configuredPath);
            if (File.Exists(full))
            {
                return full;
            }

            throw new FileNotFoundException($"Configured run-parity.ps1 was not found at '{full}'.", full);
        }

        foreach (string root in EnumerateSearchRoots())
        {
            string candidate = Path.Combine(root, "qa", "scripts", "run-parity.ps1");
            if (File.Exists(candidate))
            {
                return candidate;
            }

            string workbenchCandidate = Path.Combine(root, "workbench", "qa", "scripts", "run-parity.ps1");
            if (File.Exists(workbenchCandidate))
            {
                return workbenchCandidate;
            }
        }

        throw new FileNotFoundException("Could not locate qa/scripts/run-parity.ps1 automatically. Specify --run-parity-script <path>.");
    }

    private static IEnumerable<string> EnumerateSearchRoots()
    {
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (string start in new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory })
        {
            DirectoryInfo? dir = new(start);
            while (dir is not null)
            {
                if (visited.Add(dir.FullName))
                {
                    yield return dir.FullName;
                }

                dir = dir.Parent;
            }
        }
    }

    private static string? ResolveContainerCli()
    {
        foreach (string candidate in new[] { "wslc", "docker" })
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "where",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                };
                psi.ArgumentList.Add(candidate);
                using Process? p = Process.Start(psi);
                if (p is not null)
                {
                    p.WaitForExit(2000);
                    if (p.ExitCode == 0)
                    {
                        return candidate;
                    }
                }
            }
            catch (Exception)
            {
                // Try next CLI.
            }
        }

        return null;
    }

    public async ValueTask DisposeAsync()
    {
        if (!_startedByUs || _options.KeepContainers || _options.ReuseLegacyContainer)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(_containerCli))
        {
            return;
        }

        _state = "Stopping container...";
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = _containerCli,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            psi.ArgumentList.Add("rm");
            psi.ArgumentList.Add("-f");
            psi.ArgumentList.Add(_options.LegacyContainerName);

            using Process? proc = Process.Start(psi);
            if (proc is not null)
            {
                await proc.WaitForExitAsync();
            }
        }
        catch (Exception)
        {
            // Best-effort cleanup.
        }

        _isReady = false;
        _state = "Stopped";
    }
}
