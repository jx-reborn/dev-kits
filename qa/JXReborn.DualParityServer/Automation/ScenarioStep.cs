using System.Text.Json.Serialization;

namespace JXReborn.DualParityServer.Automation;

/// <summary>
/// Single atomic action step in a scripted <see cref="ParityScenario"/>.
/// </summary>
public sealed record ScenarioStep(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("x")] int? X = null,
    [property: JsonPropertyName("y")] int? Y = null,
    [property: JsonPropertyName("npcName")] string? NpcName = null,
    [property: JsonPropertyName("optionIndex")] int? OptionIndex = null,
    [property: JsonPropertyName("genre")] int? Genre = null,
    [property: JsonPropertyName("detailType")] int? DetailType = null,
    [property: JsonPropertyName("particularType")] int? ParticularType = null,
    [property: JsonPropertyName("pkMode")] int? PkMode = null,
    [property: JsonPropertyName("channel")] int? Channel = null,
    [property: JsonPropertyName("message")] string? Message = null,
    [property: JsonPropertyName("waitMs")] int? WaitMs = null,
    [property: JsonPropertyName("maxBranches")] int? MaxBranches = null);
