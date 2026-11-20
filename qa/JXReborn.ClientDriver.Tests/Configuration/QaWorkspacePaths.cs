using System.Runtime.CompilerServices;
using JXReborn.GameAssets.Packs;
using JXReborn.Packs.TieuDaoTu.Hosting;

namespace JXReborn.ClientDriver.Tests.Configuration;

internal static class QaWorkspacePaths
{
    public static string WorkbenchRootPath { get; } = DiscoverWorkbenchRoot();

    public static string ServerRootPath { get; } = Path.GetFullPath(Path.Combine(WorkbenchRootPath, "..", "server"));

    public static string PackRootPath { get; } = ResolvePackRoot(WorkbenchRootPath);

    public static string PackServerDistDirectory => Path.Combine(PackRootPath, "dist", "server");

    public static string PackDataDirectory => Path.Combine(PackRootPath, "data");

    public const string PackSnapshotFileName = "server.pack";

    public static string KeyTablePath => Path.Combine(WorkbenchRootPath, "artifacts", "keys", "legacy-public-keys.bin");

    public static string HeadlessClientTemplateDirectory => Path.Combine(WorkbenchRootPath, "artifacts", "headless-client");

    public static string WireCapturesDirectory => Path.Combine(WorkbenchRootPath, "tests", "JXReborn.Workbench.ParityTests", "WireCaptures", "Baselines");

    [ModuleInitializer]
    internal static void Initialize()
    {
        ScriptCodeResolverRegistry.SetResolver(new TieuDaoTuScriptCodeResolver());

        if (Directory.Exists(ServerRootPath))
        {
            Environment.SetEnvironmentVariable("JXREBORN_SERVER_ROOT", ServerRootPath);
        }

        if (Directory.Exists(PackDataDirectory))
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

        throw new DirectoryNotFoundException($"Unable to locate JXReborn.Workbench.slnx from '{AppContext.BaseDirectory}'.");
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
}
