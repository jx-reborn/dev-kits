using JXReborn.Adapter.Legacy;
using JXReborn.Adapter.Legacy.Bishop;
using JXReborn.Adapter.Legacy.GameServer;
using JXReborn.Application;
using JXReborn.Application.Configuration;
using JXReborn.ClientDriver.Tests.Diagnostics;
using JXReborn.GameAssets;
using JXReborn.GameAssets.Configuration;
using JXReborn.Infrastructure;
using JXReborn.Scripts.Hosting;
using JXReborn.ClientDriver.Tests.Configuration;
using JXReborn.Workbench.Scripts.Items;
using JXReborn.Packs.TieuDaoTu.Hosting;
using JXReborn.TestBase.Fixtures;
using JXReborn.ZoneEngine;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace JXReborn.ClientDriver.Tests;

/// <summary>
/// In-process Bishop + GameServer test server harness with dynamic ephemeral ports and isolated in-memory SQLite.
/// </summary>
public sealed class HeadlessTestServerHarness : IAsyncDisposable
{
    private readonly SqliteConnection _keepAliveSqliteConnection;
    private readonly IHost _host;

    private HeadlessTestServerHarness(
        SqliteConnection keepAliveSqliteConnection,
        IHost host,
        ServerLogCollector logCollector)
    {
        _keepAliveSqliteConnection = keepAliveSqliteConnection;
        _host = host;
        LogCollector = logCollector;
    }

    public int BishopPort { get; private set; }

    public int GameServerPort { get; private set; }

    public ServerLogCollector LogCollector { get; }

    public IServiceProvider Services => _host.Services;

    public static async Task<HeadlessTestServerHarness> StartAsync(
        byte initialNewbieLevel = 1,
        bool? enableTestGmToken = null,
        bool? autoGrantGmTokenOnLogin = null,
        ushort[]? excludedWorldSetMapIds = null)
    {
        string profile = (Environment.GetEnvironmentVariable("LEGACY_ORACLE_PROFILE") ?? "minimal").Trim().ToLowerInvariant();
        bool resolvedEnableGm = enableTestGmToken ?? (profile != "vanilla");
        bool resolvedAutoGrantGm = autoGrantGmTokenOnLogin ?? (profile == "kit");

        string dbName = $"file:jxreborn_headless_{Guid.NewGuid():N}?mode=memory&cache=shared";
        var keepAliveConn = new SqliteConnection($"Data Source={dbName};Default Timeout=30;");
        await keepAliveConn.OpenAsync();

        var logCollector = new ServerLogCollector();
        var configDict = new Dictionary<string, string?>
        {
            ["Persistence:Provider"] = "Sqlite",
            ["Persistence:ConnectionString"] = $"Data Source={dbName};Default Timeout=30;",
            ["Persistence:AutoInitializeDatabase"] = "true",
            ["GameAssets:DataDirectory"] = QaWorkspacePaths.PackDataDirectory,
            ["GameAssets:ArtifactsDirectory"] = QaWorkspacePaths.PackServerDistDirectory,
            ["GameAssets:DataSnapshotFile"] = QaWorkspacePaths.PackSnapshotFileName,
            ["GameAssets:PreferBinarySnapshots"] = "true",
            ["GameAssets:DefaultFallbackStarterVillageMapId"] = "53",
            ["GameAssets:FallbackUnloadedStarterVillage"] = "true",
            ["GameAssets:EagerObstacleLoadMapIds:0"] = "53",
            ["GameAssets:EagerObstacleLoadMapIds:1"] = "54",
            ["GameAssets:EagerObstacleLoadMapIds:2"] = "70",
            ["GameAssets:EagerObstacleLoadMapIds:3"] = "74",
            ["GameAssets:EagerObstacleLoadMapIds:4"] = "199",
            ["GameAssets:EagerObstacleLoadMapIds:5"] = "101",
            ["GameAssets:EagerObstacleLoadMapIds:6"] = "78",
            ["GameAssets:EagerObstacleLoadMapIds:7"] = "81",
            ["GameAssets:EagerObstacleLoadMapIds:8"] = "103",
            ["GameAssets:EagerObstacleLoadMapIds:9"] = "115",
            ["Application:AutoProvisionAccounts"] = "true",
            ["Application:AdvertisedGameServerHost"] = "127.0.0.1",
            ["Application:AdvertisedGameServerPort"] = "0",
            ["Application:HandoffTicketTtlSeconds"] = "30",
            ["Application:HeartbeatTimeoutSeconds"] = "15",
            ["ZoneEngine:TicksPerSecond"] = "18",
            ["ZoneEngine:HeartbeatIntervalSeconds"] = "5",
            ["ZoneEngine:HeartbeatTimeoutSeconds"] = "15",
            ["ZoneEngine:InitialNewbieLevel"] = initialNewbieLevel.ToString(),
            ["ZoneEngine:EnableTestGmToken"] = resolvedEnableGm ? "true" : "false",
            ["ZoneEngine:AutoGrantGmTokenOnLogin"] = resolvedAutoGrantGm ? "true" : "false",
            ["ZoneEngine:TestGmTokenItemCode"] = TestGmTokenItemScript.GmTokenItemCode,
            ["ZoneEngine:TestGmTokenScriptCode"] = TestGmTokenItemScript.GmTokenScriptPath,
            ["LegacyGateway:BindAddress"] = "127.0.0.1",
            ["LegacyGateway:BishopPort"] = "0",
            ["LegacyGateway:GameServerPort"] = "0",
            ["LegacyGateway:KeyTablePath"] = QaWorkspacePaths.KeyTablePath,
            ["LegacyGateway:AllowSyntheticKeyTableFallback"] = "false"
        };

        HostApplicationBuilder builder = Host.CreateEmptyApplicationBuilder(new HostApplicationBuilderSettings());
        builder.Configuration.AddInMemoryCollection(configDict);
        builder.Logging.SetMinimumLevel(LogLevel.Information);
        builder.Logging.AddProvider(logCollector);

        builder.Services.AddJXRebornInfrastructure(builder.Configuration);
        if (excludedWorldSetMapIds is { Length: > 0 })
        {
            builder.Services.AddJXRebornGameAssets(builder.Configuration, opts =>
            {
                opts.ExcludedWorldSetMapIds = excludedWorldSetMapIds;
            });
        }
        else
        {
            builder.Services
                .AddOptions<GameAssetsOptions>()
                .Bind(builder.Configuration.GetSection(GameAssetsOptions.SectionName));
            SharedGameAssetsFixture.Default.RegisterSharedCatalogsInto(builder.Services);
        }

        builder.Services
            .AddJXRebornApplication(builder.Configuration)
            .AddJXRebornScripts(builder.Configuration, preloadedAssemblies: [typeof(TieuDaoTuScriptCodeResolver).Assembly, typeof(TestGmTokenItemScript).Assembly])
            .AddJXRebornZoneEngine(builder.Configuration)
            .AddZoneModule<HeadlessTestZoneModule>()
            .AddJXRebornLegacyAdapter(builder.Configuration);

        IHost host = builder.Build();
        await host.StartAsync();

        var bishopListener = host.Services.GetRequiredService<BishopTcpListenerHostedService>();
        var gsListener = host.Services.GetRequiredService<GameServerTcpListenerHostedService>();

        ApplicationOptions appOptions = host.Services.GetRequiredService<IOptions<ApplicationOptions>>().Value;
        appOptions.AdvertisedGameServerPort = (ushort)gsListener.BoundPort;

        return new HeadlessTestServerHarness(keepAliveConn, host, logCollector)
        {
            BishopPort = bishopListener.BoundPort,
            GameServerPort = gsListener.BoundPort
        };
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            await _host.StopAsync(TimeSpan.FromSeconds(3));
        }
        catch
        {
        }

        _host.Dispose();
        await _keepAliveSqliteConnection.DisposeAsync();
    }
}
