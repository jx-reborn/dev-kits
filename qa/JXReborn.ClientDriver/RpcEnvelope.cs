using System.Text.Json;
using System.Text.Json.Serialization;

namespace JXReborn.ClientDriver;

internal sealed record RpcEnvelope(
    [property: JsonPropertyName("id")] string? Id,
    [property: JsonPropertyName("ok")] bool Ok,
    [property: JsonPropertyName("result")] JsonElement Result,
    [property: JsonPropertyName("error")] string? Error);
