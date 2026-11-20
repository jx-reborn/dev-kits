using System.Text.Json.Serialization;

namespace JXReborn.ClientDriver;

/// <summary>
/// Result returned by <c>account_login</c>, <c>create_role</c>, or <c>select_role</c>.
/// </summary>
public sealed record LoginActionResult(
    [property: JsonPropertyName("sent")] bool Sent,
    [property: JsonPropertyName("login_status")] int LoginStatus,
    [property: JsonPropertyName("login_result")] int LoginResult);
