using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using JXReborn.ClientDriver;

Console.OutputEncoding = Encoding.UTF8;

JsonSerializerOptions jsonOutOptions = new()
{
    WriteIndented = true,
    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
};

if (args.Length == 0 || args[0] is "-h" or "--help" or "help")
{
    PrintUsage();
    return 0;
}

string command = args[0].ToLowerInvariant();
Dictionary<string, string?> opts = ParseFlags(args[1..]);

try
{
    object payload = command switch
    {
        "spawn" => await HandleSpawnAsync(opts),
        "ping" => await HandlePingAsync(opts),
        "state" => await HandleStateAsync(opts),
        "login" => await HandleLoginAsync(opts),
        "move" => await HandleMoveAsync(opts),
        "interact-npc" => await HandleInteractNpcAsync(opts),
        "select-dialog" => await HandleSelectDialogAsync(opts),
        "attack" => await HandleAttackAsync(opts),
        "switch-pk" => await HandleSwitchPkAsync(opts),
        "pk-target" => await HandlePkTargetAsync(opts),
        "begin-capture" => await HandleBeginCaptureAsync(opts),
        "end-capture" => await HandleEndCaptureAsync(opts),
        "raw-op" => await HandleRawOpAsync(opts),
        "stop" => await HandleStopAsync(opts),
        _ => throw new ArgumentException($"Unknown subcommand '{command}'.")
    };

    Console.WriteLine(JsonSerializer.Serialize(new { ok = true, command, result = payload }, jsonOutOptions));
    return 0;
}
catch (Exception ex)
{
    Console.Error.WriteLine(JsonSerializer.Serialize(
        new
        {
            ok = false,
            command,
            error = ex.Message,
            exception = ex.GetType().Name,
            crash_dump_path = (ex as GameExeCrashException)?.CrashDumpPath,
            crash_dump = (ex as GameExeCrashException)?.CrashDumpJson
        },
        jsonOutOptions));
    return 1;
}

static void PrintUsage()
{
    Console.WriteLine(
        """
        JXReborn.ClientDriver.Cli - Headless game.exe JSON-RPC Driver CLI

        Commands:
          spawn          [--ip <127.0.0.1>] [--port <5622>] [--pipe <name>] [--interactive] [--template <dir>] [--sandbox-root <dir>]
          ping           --pipe <name>
          state          --pipe <name>
          login          --pipe <name> --account <acct> --password <pwd> [--ip <ip>] [--port <port>] [--role <name>] [--gender <0|1>] [--series <0..4>] [--native-place <53>]
          move           --pipe <name> --x <mpsX> --y <mpsY> [--mode <0|1|2>]
          interact-npc   --pipe <name> (--index <arrayIndex> | --dw-id <dwId>)
          select-dialog  --pipe <name> --option <index>
          attack         --pipe <name> --skill <skillId> (--target <dwId> | --x <mpsX> --y <mpsY>)
          switch-pk      --pipe <name> --mode <pkMode>
          pk-target      --pipe <name> --target <dwId>
          begin-capture  --pipe <name>
          end-capture    --pipe <name> [--out <path>]
          raw-op         --pipe <name> --oper <uOper> [--param <uParam>] [--nparam <nParam>]
          stop           --pipe <name>
        """);
}

static Dictionary<string, string?> ParseFlags(string[] flagArgs)
{
    var dict = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
    for (int i = 0; i < flagArgs.Length; i++)
    {
        string token = flagArgs[i];
        if (!token.StartsWith("--", StringComparison.Ordinal))
        {
            throw new ArgumentException($"Unexpected positional argument '{token}'. Expected --flag value.");
        }

        string key = token[2..];
        int eqIdx = key.IndexOf('=');
        if (eqIdx >= 0)
        {
            dict[key[..eqIdx]] = key[(eqIdx + 1)..];
            continue;
        }

        if (i + 1 < flagArgs.Length && !flagArgs[i + 1].StartsWith("--", StringComparison.Ordinal))
        {
            dict[key] = flagArgs[++i];
        }
        else
        {
            dict[key] = "true";
        }
    }

    return dict;
}

static string RequireString(Dictionary<string, string?> opts, string key)
{
    if (!opts.TryGetValue(key, out string? val) || string.IsNullOrWhiteSpace(val))
    {
        throw new ArgumentException($"Missing required option --{key}.");
    }

    return val.Trim();
}

static string GetStringOrDefault(Dictionary<string, string?> opts, string key, string fallback)
    => opts.TryGetValue(key, out string? val) && !string.IsNullOrWhiteSpace(val) ? val.Trim() : fallback;

static int GetInt(Dictionary<string, string?> opts, string key, int? defaultValue = null)
{
    if (!opts.TryGetValue(key, out string? raw) || string.IsNullOrWhiteSpace(raw))
    {
        return defaultValue ?? throw new ArgumentException($"Missing required integer option --{key}.");
    }

    return int.Parse(raw, CultureInfo.InvariantCulture);
}

static uint GetUInt(Dictionary<string, string?> opts, string key, uint? defaultValue = null)
{
    if (!opts.TryGetValue(key, out string? raw) || string.IsNullOrWhiteSpace(raw))
    {
        return defaultValue ?? throw new ArgumentException($"Missing required uint option --{key}.");
    }

    return raw.StartsWith("0x", StringComparison.OrdinalIgnoreCase)
        ? uint.Parse(raw[2..], NumberStyles.HexNumber, CultureInfo.InvariantCulture)
        : uint.Parse(raw, CultureInfo.InvariantCulture);
}

static string GetCliMetadataPath(string sandboxRoot, string pipeName)
    => Path.Combine(Path.GetFullPath(sandboxRoot), $"cli-{pipeName}.json");

static CliInstanceMetadata? TryReadCliMetadata(Dictionary<string, string?> opts, string pipeName)
{
    string sandboxRoot = opts.TryGetValue("sandbox-root", out string? root) && !string.IsNullOrWhiteSpace(root)
        ? root
        : new JxHeadlessOptions().SandboxRootDirectory;
    string path = GetCliMetadataPath(sandboxRoot, pipeName);
    if (!File.Exists(path))
    {
        return null;
    }

    return JsonSerializer.Deserialize<CliInstanceMetadata>(File.ReadAllText(path));
}

static async Task<object> HandleSpawnAsync(Dictionary<string, string?> opts)
{
    var defaults = new JxHeadlessOptions();
    string templateDir = GetStringOrDefault(opts, "template", defaults.TemplateDirectory);
    string sandboxRoot = GetStringOrDefault(opts, "sandbox-root", defaults.SandboxRootDirectory);
    string ip = GetStringOrDefault(opts, "ip", defaults.ServerIp);
    int port = GetInt(opts, "port", defaults.GatewayPort);
    string pipeName = GetStringOrDefault(opts, "pipe", $"jx-cli-{Guid.NewGuid():N}"[..16]);
    bool interactive = opts.TryGetValue("interactive", out string? rawInteractive) &&
                       string.Equals(rawInteractive, "true", StringComparison.OrdinalIgnoreCase);

    using JxHeadlessSandbox sandbox = JxHeadlessSandbox.Create(
        templateDir,
        sandboxRoot,
        pipeName,
        port,
        preserveOnDispose: true,
        interactiveWindow: interactive);

    var psi = new ProcessStartInfo
    {
        FileName = sandbox.ExecutablePath,
        WorkingDirectory = sandbox.SandboxDirectory,
        UseShellExecute = false,
        CreateNoWindow = !interactive,
        WindowStyle = interactive ? ProcessWindowStyle.Normal : ProcessWindowStyle.Hidden,
        RedirectStandardInput = true,
        RedirectStandardOutput = true,
        RedirectStandardError = true
    };
    psi.Environment["LEGACY_HEADLESS"] = "1";
    psi.Environment["LEGACY_PIPE_NAME"] = pipeName;
    psi.Environment["LEGACY_CRASH_DUMP_PATH"] = sandbox.CrashDumpPath;
    if (interactive)
    {
        psi.Environment["LEGACY_INTERACTIVE_WINDOW"] = "1";
    }

    using Process proc = Process.Start(psi)
        ?? throw new InvalidOperationException($"Failed to start '{sandbox.ExecutablePath}'.");

    var deadline = DateTime.UtcNow + defaults.SpawnTimeout;
    PingResult? ping = null;
    Exception? lastEx = null;
    while (DateTime.UtcNow < deadline)
    {
        if (proc.HasExited)
        {
            string? dump = File.Exists(sandbox.CrashDumpPath) ? File.ReadAllText(sandbox.CrashDumpPath) : null;
            sandbox.PreserveOnDispose = dump is not null;
            throw new GameExeCrashException(
                $"Spawned game.exe exited prematurely during startup (ExitCode={proc.ExitCode}).",
                proc.ExitCode,
                sandbox.CrashDumpPath,
                dump);
        }

        try
        {
            await using JxHeadlessClient client = await JxHeadlessClient.ConnectExistingAsync(
                pipeName,
                sandbox.CrashDumpPath,
                TimeSpan.FromSeconds(2));
            ping = await client.PingAsync();
            if (ping.Pong)
            {
                break;
            }
        }
        catch (Exception ex) when (ex is IOException or TimeoutException)
        {
            lastEx = ex;
            await Task.Delay(50);
        }
    }

    if (ping is null || !ping.Pong)
    {
        try
        {
            if (!proc.HasExited)
            {
                proc.Kill(entireProcessTree: true);
            }
        }
        catch
        {
        }

        sandbox.PreserveOnDispose = false;
        throw new TimeoutException($"Timed out waiting for spawned game.exe pipe '{pipeName}'.", lastEx);
    }

    var meta = new CliInstanceMetadata(pipeName, proc.Id, sandbox.SandboxDirectory, sandbox.CrashDumpPath, ip, port);
    string metaPath = GetCliMetadataPath(sandboxRoot, pipeName);
    File.WriteAllText(metaPath, JsonSerializer.Serialize(meta));

    return new
    {
        pipe = pipeName,
        pid = proc.Id,
        sandbox_dir = sandbox.SandboxDirectory,
        crash_dump_path = sandbox.CrashDumpPath,
        metadata_path = metaPath,
        server_ip = ip,
        gateway_port = port,
        ping
    };
}

static async Task<object> HandlePingAsync(Dictionary<string, string?> opts)
{
    string pipeName = RequireString(opts, "pipe");
    CliInstanceMetadata? meta = TryReadCliMetadata(opts, pipeName);
    await using JxHeadlessClient client = await JxHeadlessClient.ConnectExistingAsync(pipeName, meta?.CrashDumpPath);
    return await client.PingAsync();
}

static async Task<object> HandleStateAsync(Dictionary<string, string?> opts)
{
    string pipeName = RequireString(opts, "pipe");
    CliInstanceMetadata? meta = TryReadCliMetadata(opts, pipeName);
    await using JxHeadlessClient client = await JxHeadlessClient.ConnectExistingAsync(pipeName, meta?.CrashDumpPath);
    WorldDomSnapshot state = await client.GetStateAsync();

    return new
    {
        state.Pid,
        state.TickCount,
        state.WindowVisible,
        state.Net,
        login = new
        {
            state.Login.Status,
            state.Login.Result,
            state.Login.NumRoles,
            selected_role = state.Login.SelectedRoleName,
            roles = state.Login.Roles.Select(r => new { r.Index, name = r.Name, r.Gender, r.Series, r.NativePlaceId })
        },
        player = new
        {
            state.Player.InWorld,
            state.Player.NpcIndex,
            state.Player.DwId,
            name = state.Player.Name,
            state.Player.Level,
            state.Player.Series,
            state.Player.Life,
            state.Player.LifeMax,
            state.Player.Mana,
            state.Player.ManaMax,
            state.Player.Money,
            state.Player.PkFlag,
            state.Player.FightMode,
            state.Player.MapId,
            state.Player.MapX,
            state.Player.MapY,
            state.Player.MpsX,
            state.Player.MpsY,
            state.Player.SyncReady,
            state.Player.RegionLoaded
        },
        npcs = state.Npcs.Select(n => new
        {
            n.ArrayIndex,
            n.IsSelf,
            n.DwId,
            name = n.Name,
            n.Kind,
            n.Level,
            n.Life,
            n.LifeMax,
            n.PkFlag,
            n.FightMode,
            n.MpsX,
            n.MpsY
        }),
        dialog = new
        {
            state.Dialog.IsOpen,
            state.Dialog.Sequence,
            prompt = state.Dialog.Prompt,
            options = state.Dialog.Options
        },
        system_messages = state.SystemMessages.Select(m => new { m.Seq, m.TickCount, text = m.Text }),
        state.MessageBoxes,
        state.RecentS2c
    };
}

static async Task<object> HandleLoginAsync(Dictionary<string, string?> opts)
{
    string pipeName = RequireString(opts, "pipe");
    string account = RequireString(opts, "account");
    string password = RequireString(opts, "password");
    CliInstanceMetadata? meta = TryReadCliMetadata(opts, pipeName);

    string ip = GetStringOrDefault(opts, "ip", meta?.ServerIp ?? "127.0.0.1");
    int port = opts.ContainsKey("port") ? GetInt(opts, "port") : (meta?.GatewayPort ?? 5622);
    opts.TryGetValue("role", out string? roleName);
    int gender = GetInt(opts, "gender", 0);
    int series = GetInt(opts, "series", 0);
    int nativePlace = GetInt(opts, "native-place", 53);

    await using JxHeadlessClient client = await JxHeadlessClient.ConnectExistingAsync(pipeName, meta?.CrashDumpPath);
    WorldDomSnapshot snapshot = await client.LoginAndEnterWorldAsync(
        ip,
        port,
        account,
        password,
        roleName,
        gender,
        series,
        nativePlace);

    return new
    {
        entered_world = snapshot.Player.InWorld,
        snapshot.Player.DwId,
        player_name = snapshot.Player.Name,
        snapshot.Player.MapId,
        snapshot.Player.MpsX,
        snapshot.Player.MpsY
    };
}

static async Task<object> HandleMoveAsync(Dictionary<string, string?> opts)
{
    string pipeName = RequireString(opts, "pipe");
    int x = GetInt(opts, "x");
    int y = GetInt(opts, "y");
    int mode = GetInt(opts, "mode", 1);
    CliInstanceMetadata? meta = TryReadCliMetadata(opts, pipeName);

    await using JxHeadlessClient client = await JxHeadlessClient.ConnectExistingAsync(pipeName, meta?.CrashDumpPath);
    return await client.MoveToAsync(x, y, mode);
}

static async Task<object> HandleInteractNpcAsync(Dictionary<string, string?> opts)
{
    string pipeName = RequireString(opts, "pipe");
    CliInstanceMetadata? meta = TryReadCliMetadata(opts, pipeName);

    await using JxHeadlessClient client = await JxHeadlessClient.ConnectExistingAsync(pipeName, meta?.CrashDumpPath);
    if (opts.ContainsKey("index"))
    {
        return await client.InteractNpcByArrayIndexAsync(GetInt(opts, "index"));
    }

    if (opts.ContainsKey("dw-id"))
    {
        return await client.InteractNpcByDwIdAsync(GetUInt(opts, "dw-id"));
    }

    throw new ArgumentException("interact-npc requires --index <arrayIndex> or --dw-id <dwId>.");
}

static async Task<object> HandleSelectDialogAsync(Dictionary<string, string?> opts)
{
    string pipeName = RequireString(opts, "pipe");
    int option = GetInt(opts, "option");
    CliInstanceMetadata? meta = TryReadCliMetadata(opts, pipeName);

    await using JxHeadlessClient client = await JxHeadlessClient.ConnectExistingAsync(pipeName, meta?.CrashDumpPath);
    return await client.SelectDialogAsync(option);
}

static async Task<object> HandleAttackAsync(Dictionary<string, string?> opts)
{
    string pipeName = RequireString(opts, "pipe");
    int skillId = GetInt(opts, "skill");
    CliInstanceMetadata? meta = TryReadCliMetadata(opts, pipeName);

    await using JxHeadlessClient client = await JxHeadlessClient.ConnectExistingAsync(pipeName, meta?.CrashDumpPath);
    if (opts.ContainsKey("target"))
    {
        return await client.UseSkillOnTargetAsync(skillId, GetUInt(opts, "target"));
    }

    if (opts.ContainsKey("x") && opts.ContainsKey("y"))
    {
        return await client.UseSkillAtPositionAsync(skillId, GetInt(opts, "x"), GetInt(opts, "y"));
    }

    throw new ArgumentException("attack requires either --target <dwId> or both --x <mpsX> and --y <mpsY>.");
}

static async Task<object> HandleSwitchPkAsync(Dictionary<string, string?> opts)
{
    string pipeName = RequireString(opts, "pipe");
    int mode = GetInt(opts, "mode");
    CliInstanceMetadata? meta = TryReadCliMetadata(opts, pipeName);

    await using JxHeadlessClient client = await JxHeadlessClient.ConnectExistingAsync(pipeName, meta?.CrashDumpPath);
    return await client.SwitchPkAsync(mode);
}

static async Task<object> HandlePkTargetAsync(Dictionary<string, string?> opts)
{
    string pipeName = RequireString(opts, "pipe");
    uint target = GetUInt(opts, "target");
    CliInstanceMetadata? meta = TryReadCliMetadata(opts, pipeName);

    await using JxHeadlessClient client = await JxHeadlessClient.ConnectExistingAsync(pipeName, meta?.CrashDumpPath);
    return await client.PkTargetAsync(target);
}

static async Task<object> HandleRawOpAsync(Dictionary<string, string?> opts)
{
    string pipeName = RequireString(opts, "pipe");
    uint oper = GetUInt(opts, "oper");
    uint param = GetUInt(opts, "param", 0);
    int nparam = GetInt(opts, "nparam", 0);
    CliInstanceMetadata? meta = TryReadCliMetadata(opts, pipeName);

    await using JxHeadlessClient client = await JxHeadlessClient.ConnectExistingAsync(pipeName, meta?.CrashDumpPath);
    return await client.RawOperationAsync(oper, param, nparam);
}

static async Task<object> HandleStopAsync(Dictionary<string, string?> opts)
{
    string pipeName = RequireString(opts, "pipe");
    string sandboxRoot = GetStringOrDefault(opts, "sandbox-root", new JxHeadlessOptions().SandboxRootDirectory);
    string metaPath = GetCliMetadataPath(sandboxRoot, pipeName);
    CliInstanceMetadata? meta = TryReadCliMetadata(opts, pipeName);

    bool quitSent = false;
    try
    {
        await using JxHeadlessClient client = await JxHeadlessClient.ConnectExistingAsync(
            pipeName,
            meta?.CrashDumpPath,
            TimeSpan.FromSeconds(2));
        _ = await client.QuitAsync();
        quitSent = true;
    }
    catch
    {
    }

    if (meta is not null)
    {
        try
        {
            using Process proc = Process.GetProcessById(meta.Pid);
            if (!proc.HasExited && !proc.WaitForExit(1500))
            {
                proc.Kill(entireProcessTree: true);
                proc.WaitForExit(2000);
            }
        }
        catch
        {
        }

        JxHeadlessSandbox.DeleteDirectoryWithJunctionSafety(meta.SandboxDir);
        if (File.Exists(metaPath))
        {
            File.Delete(metaPath);
        }
    }

    return new
    {
        stopped = true,
        pipe = pipeName,
        quit_sent = quitSent,
        cleaned_sandbox = meta?.SandboxDir
    };
}

static async Task<object> HandleBeginCaptureAsync(Dictionary<string, string?> opts)
{
    string pipeName = RequireString(opts, "pipe");
    CliInstanceMetadata? meta = TryReadCliMetadata(opts, pipeName);
    await using JxHeadlessClient client = await JxHeadlessClient.ConnectExistingAsync(pipeName, meta?.CrashDumpPath);
    return await client.BeginWireCaptureAsync();
}

static async Task<object> HandleEndCaptureAsync(Dictionary<string, string?> opts)
{
    string pipeName = RequireString(opts, "pipe");
    CliInstanceMetadata? meta = TryReadCliMetadata(opts, pipeName);
    await using JxHeadlessClient client = await JxHeadlessClient.ConnectExistingAsync(pipeName, meta?.CrashDumpPath);
    EndWireCaptureResult res = await client.EndWireCaptureAsync();
    if (opts.TryGetValue("out", out string? outPath) && !string.IsNullOrWhiteSpace(outPath))
    {
        string fullOut = Path.GetFullPath(outPath.Trim());
        string? parentDir = Path.GetDirectoryName(fullOut);
        if (!string.IsNullOrEmpty(parentDir))
        {
            Directory.CreateDirectory(parentDir);
        }

        File.WriteAllText(
            fullOut,
            JsonSerializer.Serialize(res, new JsonSerializerOptions { WriteIndented = true }),
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }

    return res;
}

internal sealed record CliInstanceMetadata(
    string PipeName,
    int Pid,
    string SandboxDir,
    string CrashDumpPath,
    string ServerIp,
    int GatewayPort);
