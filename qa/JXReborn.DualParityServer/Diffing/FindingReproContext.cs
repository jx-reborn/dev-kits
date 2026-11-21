using System.Text.Json.Serialization;

namespace JXReborn.DualParityServer.Diffing;

/// <summary>
/// Reproduction context attached to every finding so both humans and future AI Agents can locate the exact NPC/map/action.
/// </summary>
public sealed record FindingReproContext(
    [property: JsonPropertyName("mapId")] int MapId,
    [property: JsonPropertyName("playerPos")] int[] PlayerPos,
    [property: JsonPropertyName("npcName")] string? NpcName,
    [property: JsonPropertyName("actions")] IReadOnlyList<string> Actions);
