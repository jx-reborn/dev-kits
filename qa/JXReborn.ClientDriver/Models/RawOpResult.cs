using System.Text.Json.Serialization;

namespace JXReborn.ClientDriver;

/// <summary>
/// Result returned by <c>raw_op</c>.
/// </summary>
public sealed record RawOpResult(
    [property: JsonPropertyName("return_value")] int ReturnValue,
    [property: JsonPropertyName("u_oper")] uint UOper);
