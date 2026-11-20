using System.Runtime.CompilerServices;
using JXReborn.GameAssets.Packs;
using JXReborn.Packs.TieuDaoTu.Hosting;

namespace JXReborn.Workbench.ParityTests.Configuration;

public static class WorkbenchPaths
{
    public static string WorkbenchRootPath { get; } = DiscoverWorkbenchRoot();

    public static string ServerRootPath { get; } = Path.GetFullPath(Path.Combine(WorkbenchRootPath, "..", "server"));

    public static string PackRootPath { get; } = ResolvePackRoot(WorkbenchRootPath);

    public static string PackServerDistDirectory => Path.Combine(PackRootPath, "dist", "server");

    public static string PackDataDirectory => Path.Combine(PackRootPath, "data");

    public const string PackSnapshotFileName = "server.pack";

    public static string KeyTablePath => Path.Combine(WorkbenchRootPath, "artifacts", "keys", "legacy-public-keys.bin");

    public static string WireCapturesDirectory => Path.Combine(WorkbenchRootPath, "tests", "JXReborn.Workbench.ParityTests", "WireCaptures", "Baselines");

    public static string LegacyServerRootPath { get; } = DiscoverLegacyServerRoot(WorkbenchRootPath);

    public static string Server1RootPath
    {
        get
        {
            string candidate = Path.Combine(LegacyServerRootPath, "server1");
            return Directory.Exists(candidate) ? candidate : LegacyServerRootPath;
        }
    }

    public static string GatewayRootPath => Path.Combine(LegacyServerRootPath, "gateway");

    public static bool HasPackData =>
        Directory.Exists(Path.Combine(PackDataDirectory, "maps")) &&
        Directory.Exists(Path.Combine(PackDataDirectory, "players"));

    [ModuleInitializer]
    internal static void InitializeEnvironment()
    {
        ScriptCodeResolverRegistry.SetResolver(new TieuDaoTuScriptCodeResolver());

        if (Directory.Exists(ServerRootPath))
        {
            Environment.SetEnvironmentVariable("JXREBORN_SERVER_ROOT", ServerRootPath);
        }

        if (HasPackData)
        {
            Environment.SetEnvironmentVariable("JXREBORN_USE_SYNTHETIC_DATA", null);
            Environment.SetEnvironmentVariable("JXREBORN_TEST_DATA_DIR", PackDataDirectory);
            Environment.SetEnvironmentVariable("JXREBORN_TEST_ARTIFACTS_DIR", PackServerDistDirectory);
            Environment.SetEnvironmentVariable("JXREBORN_TEST_SNAPSHOT_FILE", PackSnapshotFileName);
        }

        if (Directory.Exists(WireCapturesDirectory))
        {
            Environment.SetEnvironmentVariable("JXREBORN_WIRE_CAPTURES_DIR", WireCapturesDirectory);
        }
    }

    private static string DiscoverWorkbenchRoot()
    {
        DirectoryInfo? current = new(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "JXReborn.Workbench.slnx")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException($"Unable to locate JXReborn.Workbench.slnx walking upward from '{AppContext.BaseDirectory}'.");
    }

    private static string ResolvePackRoot(string workbenchRoot)
    {
        string? envDir = Environment.GetEnvironmentVariable(PackOptions.PackDirectoryEnvVar);
        if (string.IsNullOrWhiteSpace(envDir))
        {
            envDir = Environment.GetEnvironmentVariable(PackOptions.LegacyPackDirectoryEnvVar);
        }

        if (!string.IsNullOrWhiteSpace(envDir) && Directory.Exists(envDir))
        {
            return Path.GetFullPath(envDir);
        }

        return Path.GetFullPath(Path.Combine(workbenchRoot, "..", "pack-tieudaotu"));
    }

    private static string DiscoverLegacyServerRoot(string workbenchRoot)
    {
        string? envDir = Environment.GetEnvironmentVariable("LEGACY_SERVER_DIR");
        if (!string.IsNullOrWhiteSpace(envDir) && Directory.Exists(envDir))
        {
            return Path.GetFullPath(envDir);
        }

        DirectoryInfo? current = new(workbenchRoot);
        while (current is not null)
        {
            string sibling = Path.Combine(current.FullName, "legacy-server");
            if (Directory.Exists(sibling))
            {
                return sibling;
            }

            current = current.Parent;
        }

        return Path.GetFullPath(Path.Combine(workbenchRoot, "..", "legacy-server"));
    }
}
