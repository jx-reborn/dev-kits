using System.Text.Json.Serialization;

namespace JXReborn.ClientDriver;

/// <summary>
/// Result returned by <c>connect_gateway</c>.
/// </summary>
public sealed record ConnectGatewayResult(
    [property: JsonPropertyName("connected")] bool Connected,
    [property: JsonPropertyName("ip")] string Ip,
    [property: JsonPropertyName("port")] int Port,
    [property: JsonPropertyName("login_status")] int LoginStatus);
