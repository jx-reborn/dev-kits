using System.Text.Json.Serialization;

namespace JXReborn.ClientDriver;

/// <summary>
/// Network connection state exposed by <c>get_state</c>.
/// </summary>
public sealed record NetworkStateSnapshot(
    [property: JsonPropertyName("gw_connected")] bool GwConnected,
    [property: JsonPropertyName("gs_connected")] bool GsConnected,
    [property: JsonPropertyName("disconnecting")] bool Disconnecting);
