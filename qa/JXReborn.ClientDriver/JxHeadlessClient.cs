using System.Diagnostics;
using System.IO.Pipes;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using JXReborn.Primitives.Encodings;

namespace JXReborn.ClientDriver;

/// <summary>
/// High-level .NET driver and Named Pipe JSON-RPC client for a headless <c>game.exe</c> process.
/// </summary>
public sealed class JxHeadlessClient : IAsyncDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly SemaphoreSlim _rpcLock = new(1, 1);
    private readonly bool _ownsProcess;
    private readonly Win32JobObject? _jobObject;
    private readonly JxHeadlessSandbox? _sandbox;
    private readonly Process? _process;
    private readonly TimeSpan _rpcTimeout;
    private readonly bool _preserveSandboxOnCrash;

    private NamedPipeClientStream? _pipe;
    private StreamReader? _reader;
    private StreamWriter? _writer;
    private long _nextRequestId;
    private bool _disposed;

    private JxHeadlessClient(
        string pipeName,
        string? crashDumpPath,
        TimeSpan rpcTimeout,
        bool ownsProcess,
        Win32JobObject? jobObject,
        JxHeadlessSandbox? sandbox,
        Process? process,
        bool preserveSandboxOnCrash)
    {
        PipeName = pipeName;
        CrashDumpPath = crashDumpPath;
        _rpcTimeout = rpcTimeout;
        _ownsProcess = ownsProcess;
        _jobObject = jobObject;
        _sandbox = sandbox;
        _process = process;
        _preserveSandboxOnCrash = preserveSandboxOnCrash;
    }

    /// <summary>
    /// Named Pipe name (without <c>\\.\pipe\</c> prefix) used to talk to <c>winmm.dll</c>.
    /// </summary>
    public string PipeName { get; }

    /// <summary>
    /// Path where native VEH crash dumps are written if <c>game.exe</c> faults.
    /// </summary>
    public string? CrashDumpPath { get; }

    /// <summary>
    /// Operating system process ID when spawned/known, or <see langword="null"/> when attached externally by pipe only.
    /// </summary>
    public int? ProcessId => _process?.Id;

    /// <summary>
    /// Sandbox directory path when spawned by <see cref="SpawnAsync"/>.
    /// </summary>
    public string? SandboxDirectory => _sandbox?.SandboxDirectory;

    /// <summary>
    /// Launches an isolated headless <c>game.exe</c> inside a Win32 Job Object and waits until the native pipe responds to <c>ping</c>.
    /// </summary>
    public static async Task<JxHeadlessClient> SpawnAsync(
        JxHeadlessOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        options ??= new JxHeadlessOptions();
        string instanceId = string.IsNullOrWhiteSpace(options.PipeName)
            ? $"jx-headless-{Guid.NewGuid():N}"[..24]
            : options.PipeName.Trim();
        string pipeName = instanceId;

        Win32JobObject? jobObject = null;
        JxHeadlessSandbox? sandbox = null;
        Process? process = null;
        JxHeadlessClient? client = null;

        try
        {
            jobObject = new Win32JobObject();
            sandbox = JxHeadlessSandbox.Create(
                options.TemplateDirectory,
                options.SandboxRootDirectory,
                instanceId,
                options.GatewayPort,
                options.CrashDumpPath,
                options.PreserveSandboxOnDispose,
                options.InteractiveWindow);

            var psi = new ProcessStartInfo
            {
                FileName = sandbox.ExecutablePath,
                WorkingDirectory = sandbox.SandboxDirectory,
                UseShellExecute = false,
                CreateNoWindow = !options.InteractiveWindow,
                WindowStyle = options.InteractiveWindow ? ProcessWindowStyle.Normal : ProcessWindowStyle.Hidden
            };
            psi.Environment["LEGACY_HEADLESS"] = "1";
            psi.Environment["LEGACY_PIPE_NAME"] = pipeName;
            psi.Environment["LEGACY_CRASH_DUMP_PATH"] = sandbox.CrashDumpPath;
            if (options.InteractiveWindow)
            {
                psi.Environment["LEGACY_INTERACTIVE_WINDOW"] = "1";
            }

            process = Process.Start(psi)
                ?? throw new InvalidOperationException($"Failed to start headless process '{sandbox.ExecutablePath}'.");

            jobObject.AssignProcess(process);

            client = new JxHeadlessClient(
                pipeName,
                sandbox.CrashDumpPath,
                options.RpcTimeout,
                ownsProcess: true,
                jobObject,
                sandbox,
                process,
                options.PreserveSandboxOnCrash);

            using var spawnCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            spawnCts.CancelAfter(options.SpawnTimeout);

            Exception? lastError = null;
            while (!spawnCts.IsCancellationRequested)
            {
                if (process.HasExited)
                {
                    throw client.CreateCrashOrExitException("Headless game.exe exited during startup before answering ping.");
                }

                try
                {
                    await client.EnsurePipeConnectedAsync(TimeSpan.FromMilliseconds(500), spawnCts.Token);
                    PingResult ping = await client.PingAsync(spawnCts.Token);
                    if (ping.Pong)
                    {
                        return client;
                    }
                }
                catch (OperationCanceledException) when (spawnCts.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex) when (ex is IOException or TimeoutException or InvalidOperationException)
                {
                    lastError = ex;
                    client.ResetPipeConnection();
                    await Task.Delay(50, spawnCts.Token);
                }
            }

            if (process.HasExited)
            {
                throw client.CreateCrashOrExitException("Headless game.exe exited during startup.", lastError);
            }

            throw new TimeoutException(
                $"Timed out after {options.SpawnTimeout.TotalSeconds:F1}s waiting for headless game.exe pipe '{pipeName}'.",
                lastError);
        }
        catch
        {
            if (client is not null)
            {
                await client.DisposeAsync();
            }
            else
            {
                process?.Dispose();
                sandbox?.Dispose();
                jobObject?.Dispose();
            }

            throw;
        }
    }

    /// <summary>
    /// Attaches transiently to an already-running headless <c>game.exe</c> Named Pipe without owning process lifetime.
    /// </summary>
    public static async Task<JxHeadlessClient> ConnectExistingAsync(
        string pipeName,
        string? crashDumpPath = null,
        TimeSpan? rpcTimeout = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pipeName);
        var client = new JxHeadlessClient(
            pipeName.Trim(),
            crashDumpPath,
            rpcTimeout ?? TimeSpan.FromSeconds(15),
            ownsProcess: false,
            jobObject: null,
            sandbox: null,
            process: null,
            preserveSandboxOnCrash: true);

        try
        {
            await client.EnsurePipeConnectedAsync(client._rpcTimeout, cancellationToken);
            return client;
        }
        catch
        {
            await client.DisposeAsync();
            throw;
        }
    }

    /// <summary>
    /// Calls <c>ping</c> to verify native DLL health and read the current <c>GameLoop</c> tick counter.
    /// </summary>
    public Task<PingResult> PingAsync(CancellationToken cancellationToken = default)
        => CallRpcAsync<PingResult>("ping", parameters: null, cancellationToken);

    /// <summary>
    /// Captures the full structured World DOM snapshot (<c>get_state</c>).
    /// </summary>
    public Task<WorldDomSnapshot> GetStateAsync(CancellationToken cancellationToken = default)
        => CallRpcAsync<WorldDomSnapshot>("get_state", parameters: null, cancellationToken);

    /// <summary>
    /// Instructs <c>KNetConnectAgent</c> inside <c>game.exe</c> to connect to Bishop (<c>connect_gateway</c>).
    /// </summary>
    public Task<ConnectGatewayResult> ConnectGatewayAsync(
        string ip,
        int port,
        CancellationToken cancellationToken = default)
        => CallRpcAsync<ConnectGatewayResult>("connect_gateway", new { ip, port }, cancellationToken);

    /// <summary>
    /// Submits Bishop account credentials (<c>account_login</c>); MD5 hashing is performed natively via <c>engine.dll</c>.
    /// </summary>
    public Task<LoginActionResult> AccountLoginAsync(
        string account,
        string password,
        CancellationToken cancellationToken = default)
        => CallRpcAsync<LoginActionResult>("account_login", new { account, password }, cancellationToken);

    /// <summary>
    /// Creates a new character slot (<c>create_role</c>) with Unicode name encoded to TCVN3 hex.
    /// </summary>
    public Task<LoginActionResult> CreateRoleAsync(
        string roleNameUnicode,
        int gender = 0,
        int series = 0,
        int nativePlaceId = 53,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(roleNameUnicode);
        byte[] tcvn3Bytes = Tcvn3Encoding.EncodeBytes(roleNameUnicode);
        string nameHex = Convert.ToHexString(tcvn3Bytes);
        return CallRpcAsync<LoginActionResult>(
            "create_role",
            new
            {
                name_tcvn3_hex = nameHex,
                gender,
                series,
                native_place_id = nativePlaceId
            },
            cancellationToken);
    }

    /// <summary>
    /// Selects character slot <paramref name="index"/> from the Bishop roster (<c>select_role</c>).
    /// </summary>
    public Task<LoginActionResult> SelectRoleAsync(
        int index,
        CancellationToken cancellationToken = default)
        => CallRpcAsync<LoginActionResult>("select_role", new { index }, cancellationToken);

    /// <summary>
    /// Walks or runs to map world pixel coordinates (<c>move_to</c>; <paramref name="mode"/>: <c>0</c> = auto, <c>1</c> = walk, <c>2</c> = run).
    /// </summary>
    public Task<JsonElement> MoveToAsync(
        int mpsX,
        int mpsY,
        int mode = 1,
        CancellationToken cancellationToken = default)
        => CallRpcAsync<JsonElement>(
            "move_to",
            new { mps_x = mpsX, mps_y = mpsY, mode },
            cancellationToken);

    /// <summary>
    /// Opens dialog with an NPC identified by client array index (<c>interact_npc</c>).
    /// </summary>
    public Task<JsonElement> InteractNpcByArrayIndexAsync(
        int arrayIndex,
        CancellationToken cancellationToken = default)
        => CallRpcAsync<JsonElement>("interact_npc", new { array_index = arrayIndex }, cancellationToken);

    /// <summary>
    /// Opens dialog with an NPC identified by network <c>dw_id</c> (<c>interact_npc</c>).
    /// </summary>
    public Task<JsonElement> InteractNpcByDwIdAsync(
        uint dwId,
        CancellationToken cancellationToken = default)
        => CallRpcAsync<JsonElement>("interact_npc", new { dw_id = dwId }, cancellationToken);

    /// <summary>
    /// Selects an option index on the active <c>Say</c> dialog (<c>select_dialog</c>).
    /// </summary>
    public Task<SelectDialogResult> SelectDialogAsync(
        int optionIndex,
        CancellationToken cancellationToken = default)
        => CallRpcAsync<SelectDialogResult>("select_dialog", new { option_index = optionIndex }, cancellationToken);

    /// <summary>
    /// Confirms/advances an active <c>Talk</c> dialog (<c>confirm_talk</c>), invoking server completion callback (<c>0x5F</c> <c>SelectType=0</c>) when requested.
    /// </summary>
    public Task<JsonElement> ConfirmTalkAsync(
        bool forceCallback = false,
        CancellationToken cancellationToken = default)
        => CallRpcAsync<JsonElement>(
            "confirm_talk",
            new { force_callback = forceCallback ? 1 : 0 },
            cancellationToken);

    /// <summary>
    /// Casts a skill at map coordinates (<c>use_skill</c>).
    /// </summary>
    public Task<JsonElement> UseSkillAtPositionAsync(
        int skillId,
        int mpsX,
        int mpsY,
        CancellationToken cancellationToken = default)
        => CallRpcAsync<JsonElement>(
            "use_skill",
            new { skill_id = skillId, mps_x = mpsX, mps_y = mpsY },
            cancellationToken);

    /// <summary>
    /// Casts a skill targeting a specific NPC by <c>dw_id</c> (<c>use_skill</c>).
    /// </summary>
    public Task<JsonElement> UseSkillOnTargetAsync(
        int skillId,
        uint targetNpcDwId,
        CancellationToken cancellationToken = default)
        => CallRpcAsync<JsonElement>(
            "use_skill",
            new { skill_id = skillId, target_npc_dw_id = targetNpcDwId },
            cancellationToken);

    /// <summary>
    /// Switches PK mode (<c>switch_pk</c>).
    /// </summary>
    public Task<JsonElement> SwitchPkAsync(int pkMode, CancellationToken cancellationToken = default)
        => CallRpcAsync<JsonElement>("switch_pk", new { pk_mode = pkMode }, cancellationToken);

    /// <summary>
    /// Designates a PK target by <c>dw_id</c> (<c>pk_target</c>).
    /// </summary>
    public Task<JsonElement> PkTargetAsync(uint targetNpcDwId, CancellationToken cancellationToken = default)
        => CallRpcAsync<JsonElement>("pk_target", new { target_npc_dw_id = targetNpcDwId }, cancellationToken);

    /// <summary>
    /// Picks up or clicks a ground object by client <paramref name="arrayIndex"/> (<c>pickup_obj</c>).
    /// </summary>
    public Task<JsonElement> PickupObjectByArrayIndexAsync(int arrayIndex, CancellationToken cancellationToken = default)
        => CallRpcAsync<JsonElement>("pickup_obj", new { array_index = arrayIndex }, cancellationToken);

    /// <summary>
    /// Picks up or clicks a ground object by server <paramref name="objectId"/> (<c>pickup_obj</c>).
    /// </summary>
    public Task<JsonElement> PickupObjectByIdAsync(int objectId, CancellationToken cancellationToken = default)
        => CallRpcAsync<JsonElement>("pickup_obj", new { object_id = objectId }, cancellationToken);

    /// <summary>
    /// Moves or swaps an item between inventory/equipment/hand slots (<c>move_item</c> / <c>0x5B</c>).
    /// </summary>
    public Task<JsonElement> MoveItemAsync(
        int srcPlace,
        int srcX,
        int srcY,
        int dstPlace,
        int dstX,
        int dstY,
        CancellationToken cancellationToken = default)
        => CallRpcAsync<JsonElement>(
            "move_item",
            new
            {
                src_place = srcPlace,
                src_x = srcX,
                src_y = srcY,
                dst_place = dstPlace,
                dst_x = dstX,
                dst_y = dstY
            },
            cancellationToken);

    /// <summary>
    /// Uses/consumes or auto-equips an item via <c>KPlayer::ApplyUseItem</c> (<c>use_item</c> / <c>0x59</c>).
    /// </summary>
    public Task<JsonElement> UseItemAsync(
        int arrayIndex,
        int place,
        int gridX,
        int gridY,
        CancellationToken cancellationToken = default)
        => CallRpcAsync<JsonElement>(
            "use_item",
            new { array_index = arrayIndex, place, grid_x = gridX, grid_y = gridY },
            cancellationToken);

    /// <summary>
    /// Throws away the item currently held on <c>pos_hand</c> via <c>KPlayer::ThrowAwayItem</c> (<c>throw_item</c> / <c>0x5E</c>).
    /// </summary>
    public Task<JsonElement> ThrowAwayHandItemAsync(CancellationToken cancellationToken = default)
        => CallRpcAsync<JsonElement>("throw_item", parameters: null, cancellationToken);

    /// <summary>
    /// Allocates potential points to a base attribute (<c>0=Strength, 1=Dexterity, 2=Vitality, 3=Energy</c>) via <c>0x57</c>.
    /// </summary>
    public Task<JsonElement> AddBaseAttributeAsync(
        int attribute,
        int points = 1,
        CancellationToken cancellationToken = default)
        => CallRpcAsync<JsonElement>("add_base_attr", new { attribute, points }, cancellationToken);

    /// <summary>
    /// Allocates skill points to a learned skill via <c>KPlayer::AddSkillPoint</c> (<c>0x58</c>).
    /// </summary>
    public Task<JsonElement> AddSkillPointAsync(
        int skillId,
        int points = 1,
        CancellationToken cancellationToken = default)
        => CallRpcAsync<JsonElement>("add_skill_point", new { skill_id = skillId, points }, cancellationToken);

    /// <summary>
    /// Sends a chat message (Unicode encoded to TCVN3) on the specified channel (<c>send_chat</c> / <c>0x56</c>).
    /// </summary>
    public Task<JsonElement> SendChatAsync(
        int channel,
        string messageUnicode,
        uint targetDwId = 0,
        int targetNpcIdx = 0,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(messageUnicode);
        byte[] tcvn3Bytes = Tcvn3Encoding.EncodeBytes(messageUnicode);
        string messageHex = Convert.ToHexString(tcvn3Bytes);
        return CallRpcAsync<JsonElement>(
            "send_chat",
            new
            {
                channel,
                message_tcvn3_hex = messageHex,
                target_dw_id = targetDwId,
                target_npc_idx = targetNpcIdx
            },
            cancellationToken);
    }

    /// <summary>
    /// Updates subscribed chat channel mask (<c>set_chat_channel</c> / <c>0x60</c>).
    /// </summary>
    public Task<JsonElement> SetChatChannelMaskAsync(
        uint channelMask,
        CancellationToken cancellationToken = default)
        => CallRpcAsync<JsonElement>("set_chat_channel", new { channel_mask = channelMask }, cancellationToken);

    /// <summary>
    /// Calls <c>game.exe</c>'s native <c>SendBuyItem</c> (<c>0x0058d620</c>, C2S <c>0x5D</c> 12B:
    /// <c>[0x5D, gridY, gridX, place, buyIndex(int32 LE), pad(4B)]</c>).
    /// </summary>
    public Task<JsonElement> BuyShopItemAsync(
        int buyIndex,
        int place = 3,
        int gridX = 0,
        int gridY = 0,
        CancellationToken cancellationToken = default)
        => CallRpcAsync<JsonElement>(
            "buy_shop_item",
            new { buy_index = buyIndex, place, grid_x = gridX, grid_y = gridY },
            cancellationToken);

    /// <summary>
    /// Calls <c>game.exe</c>'s native <c>SendSellItem</c> (<c>0x0058d5b0</c>, C2S <c>0x5C</c> 13B with client anti-bot token).
    /// </summary>
    public Task<JsonElement> SellShopItemAsync(
        uint serverItemId,
        CancellationToken cancellationToken = default)
        => CallRpcAsync<JsonElement>("sell_shop_item", new { server_item_id = serverItemId }, cancellationToken);

    /// <summary>
    /// Calls <c>game.exe</c>'s native <c>SendRepairItem</c> (<c>0x0058d8c0</c>, C2S <c>0x7A</c> 9B).
    /// </summary>
    public Task<JsonElement> RepairItemAsync(
        uint serverItemId,
        CancellationToken cancellationToken = default)
        => CallRpcAsync<JsonElement>("repair_item", new { server_item_id = serverItemId }, cancellationToken);

    /// <summary>
    /// Calls <c>game.exe</c>'s native <c>SendStoreMoney</c> (<c>0x0058d6e0</c>, C2S <c>0x73</c> 14B:
    /// <paramref name="direction"/> <c>0</c> = deposit bag to repository, <c>1</c> = withdraw repository to bag).
    /// </summary>
    public Task<JsonElement> StoreMoneyAsync(
        byte direction,
        int amount,
        CancellationToken cancellationToken = default)
        => CallRpcAsync<JsonElement>("store_money", new { direction, amount }, cancellationToken);

    /// <summary>
    /// Calls <c>game.exe</c>'s native <c>KItemList</c> box password methods (<c>0x0054ad20..0x0054ae20</c>, C2S <c>0x84</c> 14B:
    /// <c>0</c>=SetPassword, <c>1</c>=CancelPassword, <c>2</c>=ChangePassword, <c>3</c>=Lock, <c>4</c>=Unlock).
    /// </summary>
    public Task<JsonElement> BoxOperateAsync(
        byte subOpcode,
        uint password = 0,
        uint newPassword = 0,
        CancellationToken cancellationToken = default)
        => CallRpcAsync<JsonElement>(
            "box_operate",
            new { sub_opcode = subOpcode, password, new_password = newPassword },
            cancellationToken);

    /// <summary>
    /// Calls <c>game.exe</c>'s native <c>KPlayer::CreateTeam</c> (<c>0x00566ab0</c>, C2S <c>0x53</c> sub <c>0x02</c> 8B).
    /// </summary>
    public Task<JsonElement> CreateTeamAsync(CancellationToken cancellationToken = default)
        => CallRpcAsync<JsonElement>("team_create", parameters: null, cancellationToken);

    /// <summary>
    /// Calls <c>game.exe</c>'s native <c>KPlayer::SetTeamState</c> (<c>0x00569990</c>, C2S <c>0x53</c> sub <c>0x03</c> 8B).
    /// </summary>
    public Task<JsonElement> SetTeamOpenAsync(bool open, CancellationToken cancellationToken = default)
        => CallRpcAsync<JsonElement>("team_open_close", new { open = open ? 1 : 0 }, cancellationToken);

    /// <summary>
    /// Calls <c>game.exe</c>'s native <c>SendInviteAdd</c> (<c>0x0058d960</c>, C2S <c>0x53</c> sub <c>0x0A</c> 8B).
    /// </summary>
    public Task<JsonElement> InviteToTeamAsync(uint targetNpcDwId, CancellationToken cancellationToken = default)
        => CallRpcAsync<JsonElement>("team_invite", new { target_npc_dw_id = targetNpcDwId }, cancellationToken);

    /// <summary>
    /// Calls <c>game.exe</c>'s native <c>SendReplyInvite</c> (<c>0x0058d9a0</c>, C2S <c>0x53</c> sub <c>0x0B</c> 9B).
    /// When <paramref name="inviterPlayerIdx"/> is <c>-1</c>, uses the latest pending invitation captured from <c>0x69</c> sub <c>0x0C</c>.
    /// </summary>
    public Task<JsonElement> ReplyTeamInviteAsync(
        bool accept,
        int inviterPlayerIdx = -1,
        CancellationToken cancellationToken = default)
        => CallRpcAsync<JsonElement>(
            "team_reply_invite",
            new { inviter_player_idx = inviterPlayerIdx, accept = accept ? 1 : 0 },
            cancellationToken);

    /// <summary>
    /// Calls <c>game.exe</c>'s native <c>KPlayer::ApplyAddTeam</c> (<c>0x00568fe0</c>, C2S <c>0x53</c> sub <c>0x04</c> 8B).
    /// </summary>
    public Task<JsonElement> ApplyJoinTeamAsync(uint captainNpcDwId, CancellationToken cancellationToken = default)
        => CallRpcAsync<JsonElement>("team_apply_join", new { target_npc_dw_id = captainNpcDwId }, cancellationToken);

    /// <summary>
    /// Calls <c>game.exe</c>'s native <c>KPlayer::AcceptTeamMember</c> (<c>0x00566ae0</c>, C2S <c>0x53</c> sub <c>0x05</c> 8B).
    /// </summary>
    public Task<JsonElement> AcceptTeamMemberAsync(uint applicantNpcDwId = 0, CancellationToken cancellationToken = default)
        => CallRpcAsync<JsonElement>("team_accept_member", new { applicant_npc_dw_id = applicantNpcDwId }, cancellationToken);

    /// <summary>
    /// Calls <c>game.exe</c>'s native <c>SendLeaveTeam</c> (<c>0x00566b80</c>, C2S <c>0x53</c> sub <c>0x06</c> 8B).
    /// </summary>
    public Task<JsonElement> LeaveTeamAsync(CancellationToken cancellationToken = default)
        => CallRpcAsync<JsonElement>("team_leave", parameters: null, cancellationToken);

    /// <summary>
    /// Calls <c>game.exe</c>'s native <c>KPlayer::TeamDropMember</c> (<c>0x00566bc0</c>, C2S <c>0x53</c> sub <c>0x07</c> 8B).
    /// </summary>
    public Task<JsonElement> KickTeamMemberAsync(uint memberNpcDwId, CancellationToken cancellationToken = default)
        => CallRpcAsync<JsonElement>("team_kick", new { member_npc_dw_id = memberNpcDwId }, cancellationToken);

    /// <summary>
    /// Calls <c>game.exe</c>'s native <c>KPlayer::TeamChangeCaptain</c> (<c>0x00566c10</c>, C2S <c>0x53</c> sub <c>0x08</c> 8B).
    /// </summary>
    public Task<JsonElement> AppointTeamCaptainAsync(uint newCaptainNpcDwId, CancellationToken cancellationToken = default)
        => CallRpcAsync<JsonElement>("team_appoint", new { new_captain_npc_dw_id = newCaptainNpcDwId }, cancellationToken);

    /// <summary>
    /// Calls <c>game.exe</c>'s native <c>KNpc::SetAuraSkill</c> (<c>0x00577d90</c>, C2S <c>0x6F</c> 5B).
    /// </summary>
    public Task<JsonElement> ChangeAuraSkillAsync(int skillId, CancellationToken cancellationToken = default)
        => CallRpcAsync<JsonElement>("change_aura_skill", new { skill_id = skillId }, cancellationToken);

    /// <summary>
    /// Dispatches <c>GOI_FRIEND_INVITE</c> (<c>0x25</c>, C2S <c>0xFC</c> <c>pf_friend=7, id=0</c> 38B) via <c>CoreShell::OperationRequest</c>.
    /// </summary>
    public Task<JsonElement> InviteFriendAsync(string targetRoleNameUnicode, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(targetRoleNameUnicode);
        string nameHex = Convert.ToHexString(Tcvn3Encoding.EncodeBytes(targetRoleNameUnicode));
        return CallRpcAsync<JsonElement>("friend_invite", new { name_tcvn3_hex = nameHex }, cancellationToken);
    }

    /// <summary>
    /// Dispatches <c>GOI_FRIEND_INVITE_RESPONSE</c> (<c>0x2A</c>, C2S <c>0xFC</c> <c>pf_friend=7, id=1</c> 39B) via <c>CoreShell::OperationRequest</c>.
    /// </summary>
    public Task<JsonElement> ReplyFriendInviteAsync(
        bool accept,
        string? inviterRoleNameUnicode = null,
        CancellationToken cancellationToken = default)
    {
        string nameHex = string.IsNullOrWhiteSpace(inviterRoleNameUnicode)
            ? string.Empty
            : Convert.ToHexString(Tcvn3Encoding.EncodeBytes(inviterRoleNameUnicode));
        return CallRpcAsync<JsonElement>(
            "friend_reply",
            new { name_tcvn3_hex = nameHex, accept = accept ? 1 : 0 },
            cancellationToken);
    }

    /// <summary>
    /// Calls <c>game.exe</c>'s native <c>KUiChatCentre::DeleteFriend</c> (<c>0x004797b0</c> / <c>0x004756c0</c>, C2S <c>0xFC</c> <c>pf_friend=7, id=3</c> 37B).
    /// </summary>
    public Task<JsonElement> DeleteFriendAsync(string friendRoleNameUnicode, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(friendRoleNameUnicode);
        string nameHex = Convert.ToHexString(Tcvn3Encoding.EncodeBytes(friendRoleNameUnicode));
        return CallRpcAsync<JsonElement>("friend_delete", new { name_tcvn3_hex = nameHex }, cancellationToken);
    }

    /// <summary>
    /// Calls <c>game.exe</c>'s native <c>KUiPlayerBar::SendSomeoneMessage</c> (<c>0x0044d640</c>, C2S <c>0xFB</c> <c>0x20</c> <c>chat_someonechat</c>).
    /// </summary>
    public Task<JsonElement> SendWhisperAsync(
        string targetRoleNameUnicode,
        string messageUnicode,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(targetRoleNameUnicode);
        ArgumentException.ThrowIfNullOrWhiteSpace(messageUnicode);
        string targetHex = Convert.ToHexString(Tcvn3Encoding.EncodeBytes(targetRoleNameUnicode));
        string msgHex = Convert.ToHexString(Tcvn3Encoding.EncodeBytes(messageUnicode));
        return CallRpcAsync<JsonElement>(
            "whisper_send",
            new { target_name_tcvn3_hex = targetHex, message_tcvn3_hex = msgHex },
            cancellationToken);
    }

    /// <summary>
    /// Calls <c>game.exe</c>'s native <c>AddBlackList</c> (<c>0x00405660</c>) and verifies membership via <c>IsInBlackName</c> (<c>0x004058d0</c>).
    /// </summary>
    public Task<JsonElement> AddToBlacklistAsync(string roleNameUnicode, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(roleNameUnicode);
        string nameHex = Convert.ToHexString(Tcvn3Encoding.EncodeBytes(roleNameUnicode));
        return CallRpcAsync<JsonElement>("blacklist_add", new { name_tcvn3_hex = nameHex }, cancellationToken);
    }

    /// <summary>
    /// Calls <c>game.exe</c>'s native <c>RemoveBlackList</c> (<c>0x004057b0</c>) and verifies removal via <c>IsInBlackName</c> (<c>0x004058d0</c>).
    /// </summary>
    public Task<JsonElement> RemoveFromBlacklistAsync(string roleNameUnicode, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(roleNameUnicode);
        string nameHex = Convert.ToHexString(Tcvn3Encoding.EncodeBytes(roleNameUnicode));
        return CallRpcAsync<JsonElement>("blacklist_remove", new { name_tcvn3_hex = nameHex }, cancellationToken);
    }

    /// <summary>
    /// Calls <c>game.exe</c>'s native <c>KPlayerTrade::ApplySetState</c> (<c>0x005697e0</c> C2S <c>0x69</c> or <c>0x00566de0</c> C2S <c>0x6A</c>).
    /// </summary>
    public Task<JsonElement> SetTradeOpenAsync(bool open = true, string messageUnicode = "", CancellationToken cancellationToken = default)
    {
        string msgHex = string.IsNullOrEmpty(messageUnicode)
            ? string.Empty
            : Convert.ToHexString(Tcvn3Encoding.EncodeBytes(messageUnicode));
        return CallRpcAsync<JsonElement>("trade_set_open", new { open = open ? 1 : 0, message_tcvn3_hex = msgHex }, cancellationToken);
    }

    /// <summary>
    /// Calls <c>game.exe</c>'s native <c>SendClientCmdTradeInvite</c> (<c>0x00566e00</c>, C2S <c>0x6B</c> 9B with <c>0x566480</c> hash).
    /// </summary>
    public Task<JsonElement> InviteTradeAsync(uint targetNpcDwId, int arrayIndex = -1, CancellationToken cancellationToken = default)
        => CallRpcAsync<JsonElement>("trade_invite", new { target_npc_dw_id = targetNpcDwId, array_index = arrayIndex }, cancellationToken);

    /// <summary>
    /// Calls <c>game.exe</c>'s native <c>SendClientCmdTradeReply</c> (<c>0x005b60e0</c>, C2S <c>0x75</c> 10B with <c>0x566480</c> hash).
    /// </summary>
    public Task<JsonElement> ReplyTradeInviteAsync(bool accept = true, int inviterPlayerIdx = -1, CancellationToken cancellationToken = default)
        => CallRpcAsync<JsonElement>("trade_reply", new { inviter_player_idx = inviterPlayerIdx, accept = accept ? 1 : 0 }, cancellationToken);

    /// <summary>
    /// Calls <c>game.exe</c>'s native <c>KPlayerTrade::MoveMoney</c> (<c>0x005698c0</c>, C2S <c>0x6C</c> 5B).
    /// </summary>
    public Task<JsonElement> SetTradeMoneyAsync(int money, CancellationToken cancellationToken = default)
        => CallRpcAsync<JsonElement>("trade_set_money", new { money }, cancellationToken);

    /// <summary>
    /// Calls <c>game.exe</c>'s native <c>KPlayerTrade::Lock</c> (<c>0x00569990</c>, C2S <c>0x6D 0x02</c> 2B).
    /// </summary>
    public Task<JsonElement> LockTradeAsync(bool isLocked = true, CancellationToken cancellationToken = default)
        => CallRpcAsync<JsonElement>("trade_lock", new { @lock = isLocked ? 1 : 0 }, cancellationToken);

    /// <summary>
    /// Calls <c>game.exe</c>'s native <c>KPlayerTrade::Decision</c> (<c>0x00569940</c>, C2S <c>0x6D 0x01</c> confirm or <c>0x00</c> cancel, 2B).
    /// </summary>
    public Task<JsonElement> ConfirmTradeAsync(bool accept = true, CancellationToken cancellationToken = default)
        => CallRpcAsync<JsonElement>("trade_confirm", new { accept = accept ? 1 : 0 }, cancellationToken);

    /// <summary>
    /// Dispatches <c>GOI_STALL_SET_ADV</c> (<c>0x41</c>, C2S <c>0x7C sub=1</c>) via <c>CoreShell::OperationRequest</c>.
    /// </summary>
    public Task<JsonElement> SetStallBannerAsync(string advertisementUnicode, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(advertisementUnicode);
        string advHex = Convert.ToHexString(Tcvn3Encoding.EncodeBytes(advertisementUnicode));
        return CallRpcAsync<JsonElement>("stall_set_banner", new { adv_tcvn3_hex = advHex }, cancellationToken);
    }

    /// <summary>
    /// Dispatches <c>GOI_STALL_MARK_PRICE</c> (<c>0x45</c>) via <c>CoreShell::OperationRequest</c>.
    /// </summary>
    public Task<JsonElement> MarkStallItemPriceAsync(uint serverItemId, int price, int arrayIndex = -1, CancellationToken cancellationToken = default)
        => CallRpcAsync<JsonElement>("stall_mark_price", new { server_item_id = serverItemId, array_index = arrayIndex, price }, cancellationToken);

    /// <summary>
    /// Dispatches <c>GOI_STALL_TOGGLE_OPEN</c> (<c>0x47</c>, C2S <c>0x7C sub=1</c> + <c>sub=0</c>) via <c>CoreShell::OperationRequest</c>.
    /// </summary>
    public Task<JsonElement> OpenStallAsync(CancellationToken cancellationToken = default)
        => CallRpcAsync<JsonElement>("stall_open", parameters: null, cancellationToken);

    /// <summary>
    /// Dispatches <c>GOI_STALL_CLOSE</c> (<c>0x46</c>, C2S <c>0x7C sub=3</c> 4B) via <c>CoreShell::OperationRequest</c>.
    /// </summary>
    public Task<JsonElement> CloseStallAsync(CancellationToken cancellationToken = default)
        => CallRpcAsync<JsonElement>("stall_close", parameters: null, cancellationToken);

    /// <summary>
    /// Dispatches <c>GOI_STALL_VIEW_ITEMS</c> (<c>0x42</c>, C2S <c>0x7C sub=4</c> 8B -> <c>0x9D sub=1</c> -> <c>0x7C sub=5</c> -> <c>0x9D sub=2</c>).
    /// </summary>
    public Task<JsonElement> ViewStallAsync(uint sellerNpcDwId, bool alsoRequestItems = false, CancellationToken cancellationToken = default)
        => CallRpcAsync<JsonElement>("stall_view", new { seller_npc_dw_id = sellerNpcDwId, also_request_items = alsoRequestItems ? 1 : 0 }, cancellationToken);

    /// <summary>
    /// Dispatches <c>GOI_STALL_BUY</c> (<c>0x48</c>, C2S <c>0x7C sub=2</c> 28B) via <c>CoreShell::OperationRequest</c>.
    /// </summary>
    public Task<JsonElement> BuyStallItemAsync(uint serverItemId, int arrayIndex = -1, CancellationToken cancellationToken = default)
        => CallRpcAsync<JsonElement>("stall_buy", new { server_item_id = serverItemId, array_index = arrayIndex }, cancellationToken);

    /// <summary>
    /// Dispatches <c>GOI_ENCHASER_COMPOUND</c> (<c>0x3D</c>, C2S <c>0x7F</c> 50B) via <c>CoreShell::OperationRequest</c>.
    /// </summary>
    public Task<JsonElement> EnchaserOperateAsync(
        int equipIdx,
        int mineralIdx,
        int slotOneBased,
        int huyenTinhIdx = 0,
        int catalyst2Idx = 0,
        CancellationToken cancellationToken = default)
        => CallRpcAsync<JsonElement>(
            "enchaser_operate",
            new { equip_idx = equipIdx, mineral_idx = mineralIdx, slot_one_based = slotOneBased, huyen_tinh_idx = huyenTinhIdx, catalyst2_idx = catalyst2Idx },
            cancellationToken);

    /// <summary>
    /// Dispatches <c>GOI_FOUNDRY_REQUEST</c> (<c>0x65</c>, C2S <c>0x92</c> 204B) via <c>CoreShell::OperationRequest</c>.
    /// </summary>
    public Task<JsonElement> FoundryOperateAsync(
        byte action,
        IReadOnlyList<int> mainItemIndices,
        IReadOnlyList<int>? catalystItemIndices = null,
        int option = 0,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(mainItemIndices);
        IReadOnlyList<int> cats = catalystItemIndices ?? [];
        int At(IReadOnlyList<int> src, int idx) => idx < src.Count ? src[idx] : 0;
        return CallRpcAsync<JsonElement>(
            "foundry_operate",
            new
            {
                action = (int)action,
                main_count = mainItemIndices.Count,
                main_0 = At(mainItemIndices, 0),
                main_1 = At(mainItemIndices, 1),
                main_2 = At(mainItemIndices, 2),
                main_3 = At(mainItemIndices, 3),
                main_4 = At(mainItemIndices, 4),
                main_5 = At(mainItemIndices, 5),
                main_6 = At(mainItemIndices, 6),
                main_7 = At(mainItemIndices, 7),
                catalyst_count = cats.Count,
                cat_0 = At(cats, 0),
                cat_1 = At(cats, 1),
                cat_2 = At(cats, 2),
                cat_3 = At(cats, 3),
                cat_4 = At(cats, 4),
                cat_5 = At(cats, 5),
                cat_6 = At(cats, 6),
                cat_7 = At(cats, 7),
                option,
            },
            cancellationToken);
    }

    /// <summary>
    /// Invokes <c>KPlayerTong::ApplyCreateTong</c> (<c>0x005b8250</c>, C2S <c>0xFD sub=1</c> 14B) on <c>Player[1].m_cTong</c>.
    /// </summary>
    public Task<JsonElement> CreateTongAsync(string tongNameUnicode, int camp = 1, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tongNameUnicode);
        string nameHex = Convert.ToHexString(Tcvn3Encoding.EncodeBytes(tongNameUnicode));
        return CallRpcAsync<JsonElement>("tong_create", new { tong_name_tcvn3_hex = nameHex, camp }, cancellationToken);
    }

    /// <summary>
    /// Invokes <c>KPlayerTong</c> C++ methods (<c>ApplyAddTong</c> <c>0x005b8450</c>, <c>AcceptMember</c> <c>0x005b88c0</c>, <c>ApplyLeave</c> <c>0x005b8e40</c>, C2S <c>0xFD</c>).
    /// </summary>
    public Task<JsonElement> TongActionAsync(
        string kind,
        string? targetRoleNameUnicode = null,
        uint targetNpcDwId = 0,
        int applicantPlayerIdx = -1,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(kind);
        string nameHex = string.IsNullOrWhiteSpace(targetRoleNameUnicode)
            ? string.Empty
            : Convert.ToHexString(Tcvn3Encoding.EncodeBytes(targetRoleNameUnicode));
        return CallRpcAsync<JsonElement>(
            "tong_action",
            new { kind, target_name_tcvn3_hex = nameHex, target_npc_dw_id = targetNpcDwId, applicant_player_idx = applicantPlayerIdx },
            cancellationToken);
    }

    /// <summary>
    /// Dispatches a raw <c>CoreShell::OperationRequest</c> call (<c>raw_op</c>).
    /// </summary>
    public Task<RawOpResult> RawOperationAsync(
        uint uOper,
        uint uParam = 0,
        int nParam = 0,
        CancellationToken cancellationToken = default)
        => CallRpcAsync<RawOpResult>("raw_op", new { u_oper = uOper, u_param = uParam, n_param = nParam }, cancellationToken);

    /// <summary>
    /// Sends the cooperative <c>quit</c> RPC command to ask <c>game.exe</c> to exit cleanly.
    /// </summary>
    public Task<JsonElement> QuitAsync(CancellationToken cancellationToken = default)
        => CallRpcAsync<JsonElement>("quit", parameters: null, cancellationToken);

    /// <summary>
    /// Polls <see cref="GetStateAsync"/> until <paramref name="predicate"/> returns <see langword="true"/> or <paramref name="timeout"/> elapses.
    /// </summary>
    public async Task<WorldDomSnapshot> WaitUntilAsync(
        Func<WorldDomSnapshot, bool> predicate,
        TimeSpan timeout,
        TimeSpan? pollInterval = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(predicate);

        TimeSpan interval = pollInterval ?? TimeSpan.FromMilliseconds(50);
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(timeout);
        var sw = Stopwatch.StartNew();

        WorldDomSnapshot? lastSnapshot = null;
        while (sw.Elapsed < timeout && !cancellationToken.IsCancellationRequested)
        {
            // Never pass timeoutCts.Token into GetStateAsync: cancelling ReadLineAsync mid-RPC
            // leaves an unread JSON response line buffered in the Named Pipe and shifts future RPCs.
            lastSnapshot = await GetStateAsync(cancellationToken);

            if (predicate(lastSnapshot))
            {
                return lastSnapshot;
            }

            try
            {
                await Task.Delay(interval, timeoutCts.Token);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                break;
            }
        }

        string diagnostics = lastSnapshot is null
            ? "no state captured"
            : $"loginStatus={lastSnapshot.Login.Status}, loginResult={lastSnapshot.Login.Result}, inWorld={lastSnapshot.Player.InWorld}, syncReady={lastSnapshot.Player.SyncReady}, regionLoaded={lastSnapshot.Player.RegionLoaded}, mapId={lastSnapshot.Player.MapId}, pos=({lastSnapshot.Player.MpsX},{lastSnapshot.Player.MpsY}), des=({lastSnapshot.Player.DesX},{lastSnapshot.Player.DesY}), doing={lastSnapshot.Player.Doing}, procAi={lastSnapshot.Player.ProcessAi}, lock={lastSnapshot.Player.LockMove}, pause={lastSnapshot.Player.PauseSend}, nodeRef={lastSnapshot.Player.NpcNodeRef}, noMove={lastSnapshot.Player.NoMoveByte}, freeze={lastSnapshot.Player.FreezeTime}, regIdx={lastSnapshot.Player.RegionIndex}(id=0x{lastSnapshot.Player.RegionDwId:X8},conL={lastSnapshot.Player.RegionConnectLeft},swCenter={lastSnapshot.Player.SubWorldCenterSlot}), fight={lastSnapshot.Player.FightMode}, pk={lastSnapshot.Player.PkFlag}, tick={lastSnapshot.TickCount}, dialog=(open={lastSnapshot.Dialog.IsOpen},seq={lastSnapshot.Dialog.Sequence},opts={lastSnapshot.Dialog.Options.Count},prompt='{lastSnapshot.Dialog.Prompt}'), msgBoxes={lastSnapshot.MessageBoxes.Count}, s2c=[{string.Join(",", lastSnapshot.RecentS2c.Select(p => $"0x{p.Opcode:X2}"))}], sysMsgs=[{string.Join(" | ", lastSnapshot.SystemMessages.Select(m => m.Text))}]";
        throw new TimeoutException($"Condition not satisfied within {timeout.TotalSeconds:F1}s ({diagnostics}).");
    }

    /// <summary>
    /// Executes the full Gateway connect -> account login -> select/create character -> enter world flow
    /// and returns once <c>Player.InWorld</c> is <see langword="true"/> with a bound player <c>DwId</c>.
    /// </summary>
    public async Task<WorldDomSnapshot> LoginAndEnterWorldAsync(
        string ip,
        int port,
        string account,
        string password,
        string? roleName = null,
        int gender = 0,
        int series = 0,
        int nativePlaceId = 53,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default)
    {
        TimeSpan overallTimeout = timeout ?? TimeSpan.FromSeconds(25);
        _ = await ConnectGatewayAsync(ip, port, cancellationToken);
        _ = await AccountLoginAsync(account, password, cancellationToken);

        WorldDomSnapshot afterAuth = await WaitUntilAsync(
            s => s.Player.InWorld || s.Login.Status == 6 || (s.Login.Status == 0 && s.Login.Result > 1) || s.MessageBoxes.Count > 0,
            overallTimeout,
            cancellationToken: cancellationToken);

        if (!afterAuth.Player.InWorld && afterAuth.Login.Status == 6)
        {
            int targetIdx = -1;
            if (!string.IsNullOrWhiteSpace(roleName))
            {
                RoleSlotSnapshot? match = afterAuth.Login.Roles.FirstOrDefault(
                    r => string.Equals(r.Name, roleName, StringComparison.Ordinal));
                if (match is not null)
                {
                    targetIdx = match.Index;
                }
            }
            else if (afterAuth.Login.Roles.Count > 0)
            {
                targetIdx = afterAuth.Login.Roles[0].Index;
            }

            if (targetIdx >= 0)
            {
                _ = await SelectRoleAsync(targetIdx, cancellationToken);
            }
            else
            {
                if (afterAuth.Login.NumRoles >= 3)
                {
                    throw new InvalidOperationException(
                        $"Cannot create role '{roleName}': account '{account}' already has {afterAuth.Login.NumRoles} characters.");
                }

                string desiredName = string.IsNullOrWhiteSpace(roleName) ? $"Bot{Environment.TickCount & 0xFFFF}" : roleName;
                _ = await CreateRoleAsync(desiredName, gender, series, nativePlaceId, cancellationToken);

                WorldDomSnapshot afterCreate = await WaitUntilAsync(
                    s => s.Player.InWorld || (s.Login.Status == 6 && s.Login.NumRoles > afterAuth.Login.NumRoles) || s.MessageBoxes.Count > 0,
                    overallTimeout,
                    cancellationToken: cancellationToken);

                if (!afterCreate.Player.InWorld && afterCreate.Login.Status == 6 && afterCreate.Login.Roles.Count > 0)
                {
                    RoleSlotSnapshot? created = afterCreate.Login.Roles.FirstOrDefault(
                        r => string.Equals(r.Name, desiredName, StringComparison.Ordinal))
                        ?? afterCreate.Login.Roles[^1];
                    _ = await SelectRoleAsync(created.Index, cancellationToken);
                }
            }
        }
        else if (!afterAuth.Player.InWorld && afterAuth.Login.Status != 6)
        {
            string boxes = afterAuth.MessageBoxes.Count > 0 ? string.Join(" | ", afterAuth.MessageBoxes) : "none";
            throw new InvalidOperationException(
                $"Account login failed before role selection (status={afterAuth.Login.Status}, result={afterAuth.Login.Result}, messageBoxes={boxes}).");
        }

        return await WaitUntilAsync(
            s => s.Player.InWorld
                && s.Player.DwId != 0
                && s.Player.MapId > 0
                && s.Player.SyncReady
                && s.Player.RegionLoaded,
            overallTimeout,
            cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Starts full-payload C2S/S2C wire frame recording inside <c>JxHeadlessAgent</c> (untruncated up to 64 KB per packet).
    /// </summary>
    public Task<BeginWireCaptureResult> BeginWireCaptureAsync(CancellationToken cancellationToken = default)
        => CallRpcAsync<BeginWireCaptureResult>("begin_wire_capture", parameters: null, cancellationToken);

    /// <summary>
    /// Stops full-payload C2S/S2C wire frame recording inside <c>JxHeadlessAgent</c> and returns all captured frames.
    /// </summary>
    public Task<EndWireCaptureResult> EndWireCaptureAsync(CancellationToken cancellationToken = default)
        => CallRpcAsync<EndWireCaptureResult>("end_wire_capture", parameters: null, cancellationToken);

    /// <summary>
    /// Waits briefly after world entry for any scheduled <c>login_hint.lua</c> activity reminder dialog
    /// (<c>EVENT_HINT_LINK</c>, e.g. 19:30 <c>Đêm huy hoàng</c>) and dismisses it via standard UI option selection (<c>0x5F</c>).
    /// </summary>
    public async Task<WorldDomSnapshot> DismissSystemLoginHintIfOpenAsync(CancellationToken cancellationToken = default)
    {
        WorldDomSnapshot start = await GetStateAsync(cancellationToken);
        uint settleTick = start.TickCount + 8u;
        WorldDomSnapshot snap = await WaitUntilAsync(
            s => (s.Dialog.IsOpen && IsSystemLoginHintPrompt(s.Dialog.Prompt)) || s.TickCount >= settleTick,
            TimeSpan.FromSeconds(5),
            cancellationToken: cancellationToken);

        for (int attempt = 0; attempt < 2 && snap.Dialog.IsOpen && IsSystemLoginHintPrompt(snap.Dialog.Prompt); attempt++)
        {
            int dismissIdx = 0;
            for (int i = 0; i < snap.Dialog.Options.Count; i++)
            {
                if (snap.Dialog.Options[i].Contains("không cần nhắc nhở", StringComparison.OrdinalIgnoreCase) ||
                    snap.Dialog.Options[i].Contains("Kết thúc đối thoại", StringComparison.OrdinalIgnoreCase))
                {
                    dismissIdx = i;
                    break;
                }
            }

            _ = await SelectDialogAsync(dismissIdx, cancellationToken);
            uint afterClickTick = snap.TickCount + 6u;
            snap = await WaitUntilAsync(
                s => !s.Dialog.IsOpen || s.TickCount >= afterClickTick,
                TimeSpan.FromSeconds(5),
                cancellationToken: cancellationToken);
        }

        return snap;
    }

    private static bool IsSystemLoginHintPrompt(string? prompt) =>
        !string.IsNullOrEmpty(prompt) &&
        prompt.Contains("Hệ Thống thông báo hoạt động", StringComparison.Ordinal);

    /// <summary>
    /// Terminates or cleanly stops an owned headless <c>game.exe</c> process and deletes its sandbox unless configured otherwise.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        if (_ownsProcess && _process is not null)
        {
            try
            {
                if (!_process.HasExited)
                {
                    try
                    {
                        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
                        await EnsurePipeConnectedAsync(TimeSpan.FromMilliseconds(500), cts.Token);
                        _ = await SendRawRpcAsync("quit", parameters: null, cts.Token);
                        _ = _process.WaitForExit(1500);
                    }
                    catch
                    {
                    }
                }
            }
            finally
            {
                ResetPipeConnection();
                if (_sandbox is not null &&
                    _preserveSandboxOnCrash &&
                    !string.IsNullOrWhiteSpace(CrashDumpPath) &&
                    File.Exists(CrashDumpPath))
                {
                    _sandbox.PreserveOnDispose = true;
                }

                _jobObject?.Dispose();

                try
                {
                    if (!_process.HasExited)
                    {
                        _process.Kill(entireProcessTree: true);
                        _process.WaitForExit(2000);
                    }
                }
                catch
                {
                }

                _process.Dispose();
                _sandbox?.Dispose();
            }
        }
        else
        {
            ResetPipeConnection();
        }

        _rpcLock.Dispose();
        GC.SuppressFinalize(this);
    }

    private async Task<TResult> CallRpcAsync<TResult>(string method, object? parameters, CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        await _rpcLock.WaitAsync(cancellationToken);
        try
        {
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(_rpcTimeout);

            try
            {
                await EnsurePipeConnectedAsync(_rpcTimeout, timeoutCts.Token);
                RpcEnvelope envelope = await SendRawRpcAsync(method, parameters, timeoutCts.Token);
                if (!envelope.Ok)
                {
                    throw new InvalidOperationException($"RPC '{method}' failed: {envelope.Error ?? "unknown native error"}");
                }

                TResult? typed = envelope.Result.Deserialize<TResult>(JsonOptions);
                return typed is null
                    ? throw new InvalidDataException($"RPC '{method}' returned null/unparseable result payload.")
                    : typed;
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                if (_process is not null && _process.HasExited)
                {
                    throw CreateCrashOrExitException($"Headless game.exe exited while calling '{method}'.");
                }

                throw new TimeoutException($"RPC '{method}' timed out after {_rpcTimeout.TotalSeconds:F1}s on pipe '{PipeName}'.");
            }
            catch (Exception ex) when (ex is IOException or EndOfStreamException)
            {
                ResetPipeConnection();
                if (_process is not null && _process.HasExited)
                {
                    throw CreateCrashOrExitException($"Headless game.exe exited during RPC '{method}'.", ex);
                }

                throw;
            }
        }
        finally
        {
            _rpcLock.Release();
        }
    }

    private async Task EnsurePipeConnectedAsync(TimeSpan connectTimeout, CancellationToken cancellationToken)
    {
        if (_pipe is { IsConnected: true } && _reader is not null && _writer is not null)
        {
            return;
        }

        ResetPipeConnection();

        var pipe = new NamedPipeClientStream(
            ".",
            PipeName,
            PipeDirection.InOut,
            PipeOptions.Asynchronous);

        int timeoutMs = Math.Clamp((int)connectTimeout.TotalMilliseconds, 100, 60_000);
        await pipe.ConnectAsync(timeoutMs, cancellationToken);

        _pipe = pipe;
        _reader = new StreamReader(pipe, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false), detectEncodingFromByteOrderMarks: false, leaveOpen: true);
        _writer = new StreamWriter(pipe, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false), leaveOpen: true)
        {
            AutoFlush = true,
            NewLine = "\n"
        };
    }

    private async Task<RpcEnvelope> SendRawRpcAsync(string method, object? parameters, CancellationToken cancellationToken)
    {
        if (_writer is null || _reader is null)
        {
            throw new IOException("Pipe stream is not connected.");
        }

        string id = Interlocked.Increment(ref _nextRequestId).ToString(System.Globalization.CultureInfo.InvariantCulture);
        string requestJson = JsonSerializer.Serialize(new
        {
            id,
            method,
            @params = parameters ?? new { }
        });

        await _writer.WriteLineAsync(requestJson.AsMemory(), cancellationToken);
        await _writer.FlushAsync(cancellationToken);

        string? responseLine = await _reader.ReadLineAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(responseLine))
        {
            throw new EndOfStreamException($"Pipe closed before receiving response for RPC '{method}'.");
        }

        return JsonSerializer.Deserialize<RpcEnvelope>(responseLine, JsonOptions)
            ?? throw new InvalidDataException($"Unable to deserialize JSON-RPC response line for '{method}'.");
    }

    private void ResetPipeConnection()
    {
        _writer?.Dispose();
        _writer = null;
        _reader?.Dispose();
        _reader = null;
        _pipe?.Dispose();
        _pipe = null;
    }

    private GameExeCrashException CreateCrashOrExitException(string summary, Exception? inner = null)
    {
        int? exitCode = null;
        if (_process is not null)
        {
            try
            {
                if (_process.HasExited)
                {
                    exitCode = _process.ExitCode;
                }
            }
            catch
            {
            }
        }

        string? dumpJson = null;
        if (!string.IsNullOrWhiteSpace(CrashDumpPath) && File.Exists(CrashDumpPath))
        {
            try
            {
                dumpJson = File.ReadAllText(CrashDumpPath, Encoding.UTF8);
            }
            catch
            {
            }
        }

        var msg = new StringBuilder(summary);
        if (exitCode.HasValue)
        {
            msg.Append($" ExitCode=0x{exitCode.Value:X8} ({exitCode.Value}).");
        }

        if (!string.IsNullOrWhiteSpace(dumpJson))
        {
            msg.Append($" CrashDump='{CrashDumpPath}': {dumpJson}");
        }

        return new GameExeCrashException(msg.ToString(), exitCode, CrashDumpPath, dumpJson, inner);
    }
}
