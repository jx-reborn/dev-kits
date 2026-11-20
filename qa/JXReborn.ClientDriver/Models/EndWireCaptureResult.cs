using System.Text.Json.Serialization;

namespace JXReborn.ClientDriver;

/// <summary>
/// Response returned by <c>end_wire_capture</c> in <c>JxHeadlessAgent</c> containing full untruncated C2S/S2C frames.
/// </summary>
public sealed record EndWireCaptureResult(
    [property: JsonPropertyName("capturing")] bool Capturing,
    [property: JsonPropertyName("base_tick")] uint BaseTickFrames,
    [property: JsonPropertyName("end_tick")] uint EndTickFrames,
    [property: JsonPropertyName("frames")] IReadOnlyList<CapturedWireFrameSnapshot> Frames);
