using System.Text.Json.Serialization;

namespace JXReborn.ClientDriver;

/// <summary>
/// Full structured World DOM snapshot returned by the native <c>get_state</c> RPC.
/// </summary>
public sealed record WorldDomSnapshot(
    [property: JsonPropertyName("pid")] uint Pid,
    [property: JsonPropertyName("tick_count")] uint TickCount,
    [property: JsonPropertyName("hwnd")] ulong Hwnd,
    [property: JsonPropertyName("window_visible")] bool WindowVisible,
    [property: JsonPropertyName("net")] NetworkStateSnapshot Net,
    [property: JsonPropertyName("login")] LoginStateSnapshot Login,
    [property: JsonPropertyName("player")] PlayerSnapshot Player,
    [property: JsonPropertyName("npcs")] IReadOnlyList<NpcSnapshot> Npcs,
    [property: JsonPropertyName("dialog")] DialogSnapshot Dialog,
    [property: JsonPropertyName("system_messages")] IReadOnlyList<SystemMessageSnapshot> SystemMessages,
    [property: JsonPropertyName("message_boxes")] IReadOnlyList<string> MessageBoxes,
    [property: JsonPropertyName("recent_s2c")] IReadOnlyList<S2cPacketSnapshot> RecentS2c,
    IReadOnlyList<GroundObjectSnapshot>? GroundObjects = null,
    IReadOnlyList<InventoryItemSnapshot>? Items = null,
    IReadOnlyList<SkillSlotSnapshot>? Skills = null,
    IReadOnlyList<ChatMessageSnapshot>? ChatMessages = null,
    IReadOnlyList<C2sPacketSnapshot>? RecentC2s = null,
    TeamSnapshot? Team = null,
    SocialStateSnapshot? Social = null,
    TradeStateSnapshot? Trade = null,
    StallStateSnapshot? Stall = null,
    CraftingResultSnapshot? Crafting = null)
{
    [JsonPropertyName("ground_objects")]
    public IReadOnlyList<GroundObjectSnapshot> GroundObjects { get; init; } = GroundObjects ?? [];

    [JsonPropertyName("items")]
    public IReadOnlyList<InventoryItemSnapshot> Items { get; init; } = Items ?? [];

    [JsonPropertyName("skills")]
    public IReadOnlyList<SkillSlotSnapshot> Skills { get; init; } = Skills ?? [];

    [JsonPropertyName("chat_messages")]
    public IReadOnlyList<ChatMessageSnapshot> ChatMessages { get; init; } = ChatMessages ?? [];

    [JsonPropertyName("recent_c2s")]
    public IReadOnlyList<C2sPacketSnapshot> RecentC2s { get; init; } = RecentC2s ?? [];

    [JsonPropertyName("team")]
    public TeamSnapshot Team { get; init; } = Team ?? TeamSnapshot.Empty;

    [JsonPropertyName("social")]
    public SocialStateSnapshot Social { get; init; } = Social ?? SocialStateSnapshot.Empty;

    [JsonPropertyName("trade")]
    public TradeStateSnapshot Trade { get; init; } = Trade ?? TradeStateSnapshot.Empty;

    [JsonPropertyName("stall")]
    public StallStateSnapshot Stall { get; init; } = Stall ?? StallStateSnapshot.Empty;

    [JsonPropertyName("crafting")]
    public CraftingResultSnapshot Crafting { get; init; } = Crafting ?? CraftingResultSnapshot.Empty;
}
