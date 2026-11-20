using System.Diagnostics;
using Xunit;

namespace JXReborn.ClientDriver.Tests.Diagnostics;

/// <summary>
/// Captures recent realtime logs (<c>gameserver.log</c>, <c>bishop.log</c>) from the active
/// legacy runtime oracle container (<c>LEGACY_ORACLE_CONTAINER</c>) via <c>wslc</c> or <c>docker</c>
/// when a differential parity check fails.
/// </summary>
public static class OracleContainerDiagnostics
{
    public static void TryDumpLogsOnFailure(ITestOutputHelper output, string? containerName = null, int tailLines = 40)
    {
        string? targetContainer = containerName ?? Environment.GetEnvironmentVariable("LEGACY_ORACLE_CONTAINER");
        if (string.IsNullOrWhiteSpace(targetContainer))
        {
            return;
        }

        string? cli = ResolveContainerCli();
        if (cli is null)
        {
            return;
        }

        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = cli,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            };
            psi.ArgumentList.Add("exec");
            psi.ArgumentList.Add(targetContainer.Trim());
            psi.ArgumentList.Add("sh");
            psi.ArgumentList.Add("-c");
            psi.ArgumentList.Add(
                $"echo '=== ORACLE bishop.log (last {tailLines}) ==='; tail -n {tailLines} /opt/jxser/logs/bishop.log 2>/dev/null || true; " +
                $"echo '=== ORACLE gameserver.log (last {tailLines}) ==='; tail -n {tailLines} /opt/jxser/logs/gameserver.log 2>/dev/null || true");

            using Process? proc = Process.Start(psi);
            if (proc is null)
            {
                return;
            }

            string stdout = proc.StandardOutput.ReadToEnd();
            _ = proc.WaitForExit(5000);
            if (!string.IsNullOrWhiteSpace(stdout))
            {
                output.WriteLine(stdout.TrimEnd());
            }
        }
        catch (Exception ex)
        {
            output.WriteLine($"[OracleContainerDiagnostics] Failed to read container logs: {ex.Message}");
        }
    }

    private static string? ResolveContainerCli()
    {
        string? explicitCli = Environment.GetEnvironmentVariable("LEGACY_CONTAINER_CLI");
        if (!string.IsNullOrWhiteSpace(explicitCli))
        {
            return explicitCli.Trim();
        }

        foreach (string candidate in new[] { "wslc", "docker" })
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = candidate,
                    Arguments = "--version",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                };
                using Process? p = Process.Start(psi);
                if (p is not null && p.WaitForExit(2000) && p.ExitCode == 0)
                {
                    return candidate;
                }
            }
            catch
            {
            }
        }

        return null;
    }
}
