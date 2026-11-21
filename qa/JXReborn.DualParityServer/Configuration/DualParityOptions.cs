namespace JXReborn.DualParityServer.Configuration;

/// <summary>
/// Runtime configuration for the Dual-Run Parity Server, TCP proxy relays, client drivers, and dashboard.
/// </summary>
public sealed class DualParityOptions
{
    public string LegacyHost { get; set; } = "127.0.0.1";

    public int LegacyPort { get; set; } = 15622;

    public string RebornHost { get; set; } = "127.0.0.1";

    public int RebornPort { get; set; } = 5622;

    /// <summary>
    /// Optional local TCP proxy listen port forwarding to <see cref="LegacyHost"/>:<see cref="LegacyPort"/>.
    /// Set to 0 to disable the TCP proxy relay for legacy.
    /// </summary>
    public int LegacyProxyPort { get; set; } = 16622;

    /// <summary>
    /// Optional local TCP proxy listen port forwarding to <see cref="RebornHost"/>:<see cref="RebornPort"/>.
    /// Set to 0 to disable the TCP proxy relay for reborn.
    /// </summary>
    public int RebornProxyPort { get; set; } = 26622;

    public int DashboardPort { get; set; } = 5099;

    public string ReportOutputDir { get; set; } = Path.Combine("artifacts", "dual-parity-reports");

    public string Mode { get; set; } = "dashboard";

    public string? ScenarioPath { get; set; }

    public bool SpawnClients { get; set; }

    public bool Standalone { get; set; }

    public bool ReuseLegacyContainer { get; set; }

    public bool KeepContainers { get; set; }

    public bool OpenBrowserOnStandalone { get; set; } = true;

    public string LegacyProfile { get; set; } = "v1";

    public string LegacyContainerName { get; set; } = "legacy-oracle-test";

    public int LegacyGameServerPort { get; set; } = 16666;

    public int RebornGameServerPort { get; set; } = 16667;

    public string? RebornHostProjectPath { get; set; }

    public string? RebornPackDirectory { get; set; }

    public string? RunParityScriptPath { get; set; }

    public bool InteractiveWindows { get; set; } = true;

    public string? LegacyPipeName { get; set; }

    public int? LegacyPid { get; set; }

    public string? RebornPipeName { get; set; }

    public int? RebornPid { get; set; }

    public string Account { get; set; } = "gm001";

    public string Password { get; set; } = "123456";

    public string RoleName { get; set; } = "ParityHero01";

    public int RoleSeries { get; set; }

    public int RoleGender { get; set; }

    public int PositionToleranceMps { get; set; } = 64;

    public int CriticalPositionDriftMps { get; set; } = 256;

    public int SettleDelayMs { get; set; } = 350;

    public int MirrorPollIntervalMs { get; set; } = 120;

    public static DualParityOptions ParseArgs(string[] args)
    {
        ArgumentNullException.ThrowIfNull(args);
        var options = new DualParityOptions();
        bool rebornPortExplicit = false;

        for (int i = 0; i < args.Length; i++)
        {
            string arg = args[i];
            string? Next() => i + 1 < args.Length ? args[++i] : null;

            switch (arg)
            {
                case "--standalone":
                    options.Standalone = true;
                    options.SpawnClients = true;
                    break;
                case "--reuse-legacy":
                    options.ReuseLegacyContainer = true;
                    break;
                case "--keep-containers":
                    options.KeepContainers = true;
                    break;
                case "--no-open-browser":
                    options.OpenBrowserOnStandalone = false;
                    break;
                case "--legacy-profile":
                    options.LegacyProfile = Next() ?? options.LegacyProfile;
                    break;
                case "--legacy-container":
                    options.LegacyContainerName = Next() ?? options.LegacyContainerName;
                    break;
                case "--legacy-gs-port":
                    if (int.TryParse(Next(), out int lgp))
                    {
                        options.LegacyGameServerPort = lgp;
                    }

                    break;
                case "--reborn-gs-port":
                    if (int.TryParse(Next(), out int rgp))
                    {
                        options.RebornGameServerPort = rgp;
                    }

                    break;
                case "--reborn-project":
                    options.RebornHostProjectPath = Next();
                    break;
                case "--pack-dir":
                    options.RebornPackDirectory = Next();
                    break;
                case "--run-parity-script":
                    options.RunParityScriptPath = Next();
                    break;
                case "--legacy-host":
                    options.LegacyHost = Next() ?? options.LegacyHost;
                    break;
                case "--legacy-port":
                    if (int.TryParse(Next(), out int lp))
                    {
                        options.LegacyPort = lp;
                    }

                    break;
                case "--reborn-host":
                    options.RebornHost = Next() ?? options.RebornHost;
                    break;
                case "--reborn-port":
                    if (int.TryParse(Next(), out int rp))
                    {
                        options.RebornPort = rp;
                        rebornPortExplicit = true;
                    }

                    break;
                case "--legacy-proxy-port":
                    if (int.TryParse(Next(), out int lpp))
                    {
                        options.LegacyProxyPort = lpp;
                    }

                    break;
                case "--reborn-proxy-port":
                    if (int.TryParse(Next(), out int rpp))
                    {
                        options.RebornProxyPort = rpp;
                    }

                    break;
                case "--dashboard-port":
                    if (int.TryParse(Next(), out int dp))
                    {
                        options.DashboardPort = dp;
                    }

                    break;
                case "--report-dir":
                    options.ReportOutputDir = Next() ?? options.ReportOutputDir;
                    break;
                case "--mode":
                    options.Mode = (Next() ?? options.Mode).Trim().ToLowerInvariant();
                    break;
                case "--scenario":
                    options.ScenarioPath = Next();
                    break;
                case "--spawn":
                    options.SpawnClients = true;
                    break;
                case "--headless":
                    options.InteractiveWindows = false;
                    break;
                case "--legacy-pipe":
                    options.LegacyPipeName = Next();
                    break;
                case "--legacy-pid":
                    if (int.TryParse(Next(), out int lpid))
                    {
                        options.LegacyPid = lpid;
                    }

                    break;
                case "--reborn-pipe":
                    options.RebornPipeName = Next();
                    break;
                case "--reborn-pid":
                    if (int.TryParse(Next(), out int rpid))
                    {
                        options.RebornPid = rpid;
                    }

                    break;
                case "--account":
                    options.Account = Next() ?? options.Account;
                    break;
                case "--password":
                    options.Password = Next() ?? options.Password;
                    break;
                case "--role":
                    options.RoleName = Next() ?? options.RoleName;
                    break;
                case "--no-proxy":
                    options.LegacyProxyPort = 0;
                    options.RebornProxyPort = 0;
                    break;
            }
        }

        if (options.Standalone && !rebornPortExplicit)
        {
            // Isolate from dev JXReborn.Host (5622) by default in Standalone mode.
            options.RebornPort = 15623;
        }

        return options;
    }
}
