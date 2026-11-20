namespace JXReborn.ClientDriver;

/// <summary>
/// Configuration for spawning and driving an isolated headless <c>game.exe</c> instance.
/// </summary>
public sealed class JxHeadlessOptions
{
    /// <summary>
    /// Provisioned headless client template directory (defaults to <c>artifacts/headless-client</c>).
    /// </summary>
    public string TemplateDirectory { get; init; } = DiscoverDefaultTemplateDirectory();

    /// <summary>
    /// Parent directory where per-instance sandboxes are created (defaults to <c>artifacts/headless-client/sandboxes</c>).
    /// </summary>
    public string SandboxRootDirectory { get; init; } = DiscoverDefaultSandboxRoot();

    /// <summary>
    /// Optional explicit pipe name (without <c>\\.\pipe\</c> prefix). When omitted, a unique pipe name is generated.
    /// </summary>
    public string? PipeName { get; init; }

    /// <summary>
    /// Optional crash dump path override passed to <c>LEGACY_CRASH_DUMP_PATH</c>. Defaults inside the sandbox directory.
    /// </summary>
    public string? CrashDumpPath { get; init; }

    /// <summary>
    /// Target Gateway/Bishop IPv4 written to <c>config.ini</c> (and usable by helper calls).
    /// </summary>
    public string ServerIp { get; init; } = "127.0.0.1";

    /// <summary>
    /// Target Gateway/Bishop TCP port written to <c>config.ini</c>.
    /// </summary>
    public int GatewayPort { get; init; } = 5622;

    /// <summary>
    /// Timeout waiting for the native pipe server and initial <c>ping</c> handshake after spawning <c>game.exe</c>.
    /// </summary>
    public TimeSpan SpawnTimeout { get; init; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Default timeout for individual Named Pipe JSON-RPC requests.
    /// </summary>
    public TimeSpan RpcTimeout { get; init; } = TimeSpan.FromSeconds(15);

    /// <summary>
    /// Keeps the sandbox directory on disk when disposing normally (default <see langword="false"/>).
    /// </summary>
    public bool PreserveSandboxOnDispose { get; init; }

    /// <summary>
    /// Keeps the sandbox directory on disk if a native crash dump was generated (default <see langword="true"/>).
    /// </summary>
    public bool PreserveSandboxOnCrash { get; init; } = true;

    /// <summary>
    /// When <see langword="true"/>, keeps the real Win32 window visible and uses the real <c>represent2.dll</c>
    /// (if available alongside the client asset junctions) so a human operator can click directly inside <c>game.exe</c>
    /// while <c>JxHeadlessAgent</c> captures wire packets over the Named Pipe.
    /// </summary>
    public bool InteractiveWindow { get; init; }

    /// <summary>
    /// Discovers the repository root directory by walking upward for <c>JXReborn.slnx</c>, or returns <see langword="null"/>.
    /// </summary>
    internal static string? TryDiscoverSolutionRoot()
    {
        foreach (string probe in new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory })
        {
            DirectoryInfo? dir = new(probe);
            while (dir is not null)
            {
                if (File.Exists(Path.Combine(dir.FullName, "JXReborn.Workbench.slnx")) ||
                    File.Exists(Path.Combine(dir.FullName, "JXReborn.slnx")))
                {
                    return dir.FullName;
                }

                dir = dir.Parent;
            }
        }

        return null;
    }

    private static string DiscoverDefaultTemplateDirectory()
    {
        string? configured = Environment.GetEnvironmentVariable("LEGACY_HEADLESS_TEMPLATE_DIR");
        if (!string.IsNullOrWhiteSpace(configured))
        {
            return Path.GetFullPath(configured);
        }

        string? root = TryDiscoverSolutionRoot();
        return root is not null
            ? Path.Combine(root, "artifacts", "headless-client")
            : Path.GetFullPath(Path.Combine("artifacts", "headless-client"));
    }

    private static string DiscoverDefaultSandboxRoot()
    {
        string? configured = Environment.GetEnvironmentVariable("LEGACY_HEADLESS_SANDBOX_ROOT");
        if (!string.IsNullOrWhiteSpace(configured))
        {
            return Path.GetFullPath(configured);
        }

        return Path.Combine(DiscoverDefaultTemplateDirectory(), "sandboxes");
    }
}
