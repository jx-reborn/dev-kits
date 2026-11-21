using System.Text.Json.Serialization;

namespace JXReborn.DualParityServer.Diffing;

/// <summary>
/// Optional file artifacts (screenshots, wire captures) linked from a finding.
/// </summary>
public sealed record FindingArtifacts(
    [property: JsonPropertyName("legacyScreenshot")] string? LegacyScreenshot = null,
    [property: JsonPropertyName("rebornScreenshot")] string? RebornScreenshot = null,
    [property: JsonPropertyName("capturePath")] string? CapturePath = null);
