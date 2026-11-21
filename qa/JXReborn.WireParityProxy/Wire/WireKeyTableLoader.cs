using JXReborn.Adapter.Legacy.Ciphers;
using JXReborn.WireParityProxy.Configuration;

namespace JXReborn.WireParityProxy.Wire;

/// <summary>
/// Resolves and loads the 5,679-entry <see cref="LegacyKeyTable"/> from environment, explicit option,
/// workspace artifacts, or deterministic synthetic fallback for isolated automated tests.
/// </summary>
public static class WireKeyTableLoader
{
    public static LegacyKeyTable Load(WireParityProxyOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        string? envPath = Environment.GetEnvironmentVariable(LegacyKeyTable.KeyTablePathEnvVar);
        if (!string.IsNullOrWhiteSpace(envPath) && File.Exists(envPath))
        {
            return LegacyKeyTable.LoadFromFile(envPath);
        }

        if (!string.IsNullOrWhiteSpace(options.KeyTablePath) && File.Exists(options.KeyTablePath))
        {
            return LegacyKeyTable.LoadFromFile(options.KeyTablePath);
        }

        string? discovered = TryDiscoverWorkspaceKeyTable();
        if (discovered is not null)
        {
            return LegacyKeyTable.LoadFromFile(discovered);
        }

        if (options.AllowSyntheticKeyTableFallback)
        {
            return LegacyKeyTable.CreateSynthetic();
        }

        throw new FileNotFoundException(
            $"Legacy cipher key table was not found. Set {LegacyKeyTable.KeyTablePathEnvVar}, pass --keytable, " +
            "or pass --allow-synthetic-keys for isolated tests.");
    }

    private static string? TryDiscoverWorkspaceKeyTable()
    {
        string[] searchRoots = [Environment.CurrentDirectory, AppContext.BaseDirectory];
        foreach (string root in searchRoots)
        {
            var dir = new DirectoryInfo(root);
            while (dir is not null)
            {
                string candidate = Path.Combine(dir.FullName, "artifacts", "keys", "legacy-public-keys.bin");
                if (File.Exists(candidate))
                {
                    return candidate;
                }

                string workbenchCandidate = Path.Combine(dir.FullName, "workbench", "artifacts", "keys", "legacy-public-keys.bin");
                if (File.Exists(workbenchCandidate))
                {
                    return workbenchCandidate;
                }

                dir = dir.Parent;
            }
        }

        return null;
    }
}
