using System.Text.Json.Serialization;

namespace JXReborn.ClientDriver;

/// <summary>
/// Response returned by the native <c>ping</c> RPC method.
/// </summary>
public sealed record PingResult(
    [property: JsonPropertyName("pong")] bool Pong,
    [property: JsonPropertyName("pid")] uint Pid,
    [property: JsonPropertyName("tick_count")] uint TickCount);
