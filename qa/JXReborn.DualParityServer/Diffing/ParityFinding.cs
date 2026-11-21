using System.Text.Json.Serialization;

namespace JXReborn.DualParityServer.Diffing;

/// <summary>
/// AI-Native and human-readable parity divergence record emitted by <see cref="SemanticDiffEngine"/>.
/// </summary>
public sealed record ParityFinding(
    [property: JsonPropertyName("findingId")] string FindingId,
    [property: JsonPropertyName("clusterKey")] string ClusterKey,
    [property: JsonPropertyName("severity")] FindingSeverity Severity,
    [property: JsonPropertyName("category")] FindingCategory Category,
    [property: JsonPropertyName("stepIndex")] int StepIndex,
    [property: JsonPropertyName("stepName")] string StepName,
    [property: JsonPropertyName("fieldPath")] string FieldPath,
    [property: JsonPropertyName("legacyValue")] string LegacyValue,
    [property: JsonPropertyName("rebornValue")] string RebornValue,
    [property: JsonPropertyName("repro")] FindingReproContext Repro,
    [property: JsonPropertyName("stateDelta")] FindingStateDelta StateDelta,
    [property: JsonPropertyName("legacyRecentS2cOpcodes")] IReadOnlyList<string> LegacyRecentS2cOpcodes,
    [property: JsonPropertyName("rebornRecentS2cOpcodes")] IReadOnlyList<string> RebornRecentS2cOpcodes,
    [property: JsonPropertyName("artifacts")] FindingArtifacts Artifacts,
    [property: JsonPropertyName("timestampUtc")] DateTimeOffset TimestampUtc);
