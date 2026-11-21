using System.Text.Json.Serialization;

namespace JXReborn.DualParityServer.Diffing;

/// <summary>
/// Compact state delta captured across a single step for Legacy vs Reborn.
/// </summary>
public sealed record FindingStateDelta(
    [property: JsonPropertyName("legacyMoneyDelta")] int LegacyMoneyDelta,
    [property: JsonPropertyName("rebornMoneyDelta")] int RebornMoneyDelta,
    [property: JsonPropertyName("legacyExpDelta")] long LegacyExpDelta,
    [property: JsonPropertyName("rebornExpDelta")] long RebornExpDelta,
    [property: JsonPropertyName("legacyLevel")] int LegacyLevel,
    [property: JsonPropertyName("rebornLevel")] int RebornLevel,
    [property: JsonPropertyName("legacyMapId")] int LegacyMapId,
    [property: JsonPropertyName("rebornMapId")] int RebornMapId);
