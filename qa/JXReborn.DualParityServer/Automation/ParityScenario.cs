using System.Text.Json.Serialization;

namespace JXReborn.DualParityServer.Automation;

/// <summary>
/// Declarative JSON scenario executed in lockstep on both <c>legacy-linux</c> and <c>reborn-server</c>.
/// </summary>
public sealed record ParityScenario(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("description")] string? Description,
    [property: JsonPropertyName("steps")] IReadOnlyList<ScenarioStep> Steps);
