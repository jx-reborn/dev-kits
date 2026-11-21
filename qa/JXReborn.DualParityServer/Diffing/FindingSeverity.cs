using System.Text.Json.Serialization;

namespace JXReborn.DualParityServer.Diffing;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum FindingSeverity
{
    Info = 0,
    Warning = 1,
    Critical = 2
}
