using System.Text.Json.Serialization;

namespace JXReborn.ClientDriver;

/// <summary>
/// Viewed player stall item entry from <c>game.exe</c> <c>KStallClient</c> vector (<c>0x00680774..0x00680778</c>, 92-byte stride).
/// </summary>
public sealed record ViewedStallItemSnapshot(
    [property: JsonPropertyName("server_item_id")] uint ServerItemId,
    [property: JsonPropertyName("array_index")] int ArrayIndex,
    [property: JsonPropertyName("price")] int Price,
    [property: JsonPropertyName("grid_x")] int GridX,
    [property: JsonPropertyName("grid_y")] int GridY);
