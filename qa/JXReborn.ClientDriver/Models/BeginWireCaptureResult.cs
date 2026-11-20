using System.Text.Json.Serialization;

namespace JXReborn.ClientDriver;

/// <summary>
/// Response returned by <c>begin_wire_capture</c> in <c>JxHeadlessAgent</c>.
/// </summary>
public sealed record BeginWireCaptureResult(
    [property: JsonPropertyName("capturing")] bool Capturing,
    [property: JsonPropertyName("base_tick")] uint BaseTickFrames);
