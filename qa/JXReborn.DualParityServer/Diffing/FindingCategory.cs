using System.Text.Json.Serialization;

namespace JXReborn.DualParityServer.Diffing;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum FindingCategory
{
    PlayerState = 0,
    NpcDialog = 1,
    Inventory = 2,
    SystemMessage = 3,
    Position = 4,
    NpcVisibility = 5,
    WireProtocol = 6,
    RuntimeError = 7
}
