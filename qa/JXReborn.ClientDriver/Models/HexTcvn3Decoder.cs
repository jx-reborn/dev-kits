using JXReborn.Primitives.Encodings;

namespace JXReborn.ClientDriver;

/// <summary>
/// Decodes an uppercase or lowercase hex-encoded null-terminated TCVN3 buffer into Unicode.
/// </summary>
internal static class HexTcvn3Decoder
{
    public static string Decode(string? hex)
    {
        if (string.IsNullOrWhiteSpace(hex))
        {
            return string.Empty;
        }

        try
        {
            byte[] bytes = Convert.FromHexString(hex);
            return Tcvn3Encoding.DecodeNullTerminated(bytes);
        }
        catch (FormatException)
        {
            return string.Empty;
        }
    }
}
