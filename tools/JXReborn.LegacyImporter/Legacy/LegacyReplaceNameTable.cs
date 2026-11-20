using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.IO;
using JXReborn.LegacyImporter.Sources;

namespace JXReborn.LegacyImporter.Legacy;

/// <summary>
/// Loads legacy <c>lang/vn/replacename_npc.txt</c> (<c>KSubWorldSet::SubExecute_LoadReplaceName</c> in <c>jx_linux_y</c>)
/// to translate raw GBK <c>KSPNpc.szName</c> bytes from <c>Region_S.dat</c> (<c>maps.pak</c>) and Lua <c>AddNpcEx</c> spawns
/// into canonical Vietnamese display names (e.g. <c>\xD2\xB0\xDB\xC5</c> -&gt; <c>Dã Tẩu</c>).
/// </summary>
public sealed class LegacyReplaceNameTable
{
    public static readonly LegacyReplaceNameTable Empty = new(
        FrozenDictionary<string, string>.Empty,
        FrozenDictionary<string, string>.Empty);

    private readonly FrozenDictionary<string, string> _byRawHex;
    private readonly FrozenDictionary<string, string> _byDecodedSource;

    private LegacyReplaceNameTable(
        FrozenDictionary<string, string> byRawHex,
        FrozenDictionary<string, string> byDecodedSource)
    {
        _byRawHex = byRawHex;
        _byDecodedSource = byDecodedSource;
    }

    public int Count => _byRawHex.Count;

    public static LegacyReplaceNameTable LoadNpcReplaceNames(ILegacyServerSource source)
    {
        ArgumentNullException.ThrowIfNull(source);
        return source.TryReadServerFile(@"lang\vn\replacename_npc.txt", out byte[]? fileBytes)
            ? ParseFromBytes(fileBytes)
            : Empty;
    }

    public static LegacyReplaceNameTable LoadNpcReplaceNames(string serverRootPath)
    {
        string? filePath = LegacyPathResolver.TryResolveStringPath(serverRootPath, @"lang\vn\replacename_npc.txt");
        if (filePath is null || !File.Exists(filePath))
        {
            return Empty;
        }

        byte[] fileBytes = File.ReadAllBytes(filePath);
        return ParseFromBytes(fileBytes);
    }

    public static LegacyReplaceNameTable ParseFromBytes(ReadOnlySpan<byte> fileBytes)
    {
        var byHex = new Dictionary<string, string>(StringComparer.Ordinal);
        var byDecoded = new Dictionary<string, string>(StringComparer.Ordinal);

        int lineIndex = 0;
        int pos = 0;
        while (pos < fileBytes.Length)
        {
            int lineEnd = fileBytes[pos..].IndexOf((byte)'\n');
            ReadOnlySpan<byte> line = lineEnd >= 0
                ? fileBytes.Slice(pos, lineEnd)
                : fileBytes[pos..];
            pos += lineEnd >= 0 ? lineEnd + 1 : line.Length;

            if (!line.IsEmpty && line[^1] == (byte)'\r')
            {
                line = line[..^1];
            }

            int currentRow = lineIndex++;
            if (currentRow == 0 || line.IsEmpty)
            {
                continue;
            }

            int tabIdx = line.IndexOf((byte)'\t');
            if (tabIdx <= 0 || tabIdx + 1 >= line.Length)
            {
                continue;
            }

            ReadOnlySpan<byte> rawSource = TrimNullAndSpaces(line[..tabIdx]);
            ReadOnlySpan<byte> rawTarget = line[(tabIdx + 1)..];
            int secondTab = rawTarget.IndexOf((byte)'\t');
            if (secondTab >= 0)
            {
                rawTarget = rawTarget[..secondTab];
            }

            if (rawSource.IsEmpty || rawTarget.IsEmpty)
            {
                continue;
            }

            string targetName = HybridLegacyTextDecoder.DecodeSmart(rawTarget, preferVietnamese: true).Trim();
            if (string.IsNullOrWhiteSpace(targetName))
            {
                continue;
            }

            string hexKey = Convert.ToHexString(rawSource);
            byHex.TryAdd(hexKey, targetName);

            string decodedSource = HybridLegacyTextDecoder.DecodeSmart(rawSource, preferVietnamese: false).Trim();
            if (!string.IsNullOrWhiteSpace(decodedSource))
            {
                byDecoded.TryAdd(decodedSource, targetName);
            }
        }

        return new LegacyReplaceNameTable(
            byHex.ToFrozenDictionary(StringComparer.Ordinal),
            byDecoded.ToFrozenDictionary(StringComparer.Ordinal));
    }

    public bool TryTranslate(ReadOnlySpan<byte> rawNullTerminatedBytes, out string translatedName)
    {
        translatedName = string.Empty;
        if (_byRawHex.Count == 0)
        {
            return false;
        }

        ReadOnlySpan<byte> slice = TrimNullAndSpaces(rawNullTerminatedBytes);
        if (slice.IsEmpty)
        {
            return false;
        }

        string hexKey = Convert.ToHexString(slice);
        if (_byRawHex.TryGetValue(hexKey, out string? found) && !string.IsNullOrWhiteSpace(found))
        {
            translatedName = found;
            return true;
        }

        string decodedGbk = HybridLegacyTextDecoder.DecodeSmart(slice, preferVietnamese: false).Trim();
        if (!string.IsNullOrWhiteSpace(decodedGbk) &&
            _byDecodedSource.TryGetValue(decodedGbk, out string? byText) &&
            !string.IsNullOrWhiteSpace(byText))
        {
            translatedName = byText;
            return true;
        }

        return false;
    }

    private static ReadOnlySpan<byte> TrimNullAndSpaces(ReadOnlySpan<byte> raw)
    {
        int nullIdx = raw.IndexOf((byte)0);
        if (nullIdx >= 0)
        {
            raw = raw[..nullIdx];
        }

        while (!raw.IsEmpty && (raw[0] == (byte)' ' || raw[0] == (byte)'\t'))
        {
            raw = raw[1..];
        }

        while (!raw.IsEmpty && (raw[^1] == (byte)' ' || raw[^1] == (byte)'\t'))
        {
            raw = raw[..^1];
        }

        return raw;
    }
}
