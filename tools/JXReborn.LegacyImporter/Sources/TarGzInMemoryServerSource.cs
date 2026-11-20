using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using JXReborn.Primitives.Encodings;

namespace JXReborn.LegacyImporter.Sources;

/// <summary>
/// Single-pass in-memory <see cref="ILegacyServerSource"/> reading exporter-required entries directly
/// from a <c>.tar.gz</c>/<c>.tgz</c> archive without writing temporary extracted files to disk.
/// </summary>
public sealed class TarGzInMemoryServerSource : ILegacyServerSource
{
    private const int TarBlockSize = 512;

    private readonly Dictionary<string, (string CanonicalKey, byte[] Content)> _serverFiles;
    private readonly Dictionary<string, string> _serverDirs;
    private readonly Dictionary<string, byte[]> _gatewayFiles;

    public string DisplayName { get; }

    private TarGzInMemoryServerSource(
        string archivePath,
        string sha256Hex,
        Dictionary<string, (string CanonicalKey, byte[] Content)> serverFiles,
        Dictionary<string, string> serverDirs,
        Dictionary<string, byte[]> gatewayFiles)
    {
        _serverFiles = serverFiles;
        _serverDirs = serverDirs;
        _gatewayFiles = gatewayFiles;
        DisplayName = $"archive:{archivePath} (sha256={sha256Hex[..12]}..., files={_serverFiles.Count})";
    }

    public static bool IsTarGzArchivePath(string path) =>
        !string.IsNullOrWhiteSpace(path) &&
        (path.EndsWith(".tar.gz", StringComparison.OrdinalIgnoreCase) ||
         path.EndsWith(".tgz", StringComparison.OrdinalIgnoreCase));

    public static TarGzInMemoryServerSource Load(string archivePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(archivePath);
        string fullArchivePath = Path.GetFullPath(archivePath);
        if (!File.Exists(fullArchivePath))
        {
            throw new FileNotFoundException($"Source archive not found: '{fullArchivePath}'.", fullArchivePath);
        }

        var serverFiles = new Dictionary<string, (string CanonicalKey, byte[] Content)>(StringComparer.OrdinalIgnoreCase);
        var serverDirs = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var gatewayFiles = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);

        // Preserve raw 1-byte GBK TAR header names losslessly as Latin-1 chars so keys match
        // MapList.ini and Win-1252 legacy engine paths identically across Windows and Linux.
        Encoding headerEncoding = Encoding.Latin1;
        byte[] header = new byte[TarBlockSize];
        byte[] skipBuffer = ArrayPool<byte>.Shared.Rent(65536);

        string sha256Hex;
        try
        {
            using FileStream fs = File.OpenRead(fullArchivePath);
            sha256Hex = Convert.ToHexStringLower(SHA256.HashData(fs));
            fs.Position = 0;

            using var gz = new GZipStream(fs, CompressionMode.Decompress);
            string? pendingLongName = null;

            while (ReadExactOrEof(gz, header, TarBlockSize))
            {
                if (IsAllZero(header))
                {
                    break;
                }

                byte typeFlag = header[156];
                long size = ParseOctalInt64(header.AsSpan(124, 12));

                if (typeFlag == (byte)'L')
                {
                    byte[] nameBytes = new byte[size];
                    ReadExact(gz, nameBytes, (int)size);
                    SkipPadding(gz, size);
                    pendingLongName = DecodeEntryName(nameBytes, headerEncoding);
                    continue;
                }

                string rawName = pendingLongName ?? ReadHeaderName(header, headerEncoding);
                pendingLongName = null;

                if (typeFlag == 0 || typeFlag == (byte)'0')
                {
                    if (TryClassifyRetainedEntry(rawName, out bool isGateway, out string? relBackslashKey))
                    {
                        byte[] content = ReadExactNewArray(gz, size);
                        SkipPadding(gz, size);

                        if (isGateway)
                        {
                            gatewayFiles[relBackslashKey] = content;
                        }
                        else
                        {
                            serverFiles[relBackslashKey] = (relBackslashKey, content);
                            RegisterParentDirs(serverDirs, relBackslashKey);
                        }

                        continue;
                    }
                }
                else if (typeFlag == (byte)'5')
                {
                    if (TryClassifyRetainedDirectory(rawName, out string? relDirKey))
                    {
                        RegisterSelfAndParentDirs(serverDirs, relDirKey);
                    }
                }

                SkipBytes(gz, size, skipBuffer);
                SkipPadding(gz, size);
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(skipBuffer);
        }

        return new TarGzInMemoryServerSource(fullArchivePath, sha256Hex, serverFiles, serverDirs, gatewayFiles);
    }

    public bool TryResolveServerFile(string relativeOrEnginePath, [NotNullWhen(true)] out string? virtualKey)
    {
        if (TryFindServerEntry(relativeOrEnginePath, out var entry))
        {
            virtualKey = entry.CanonicalKey;
            return true;
        }

        virtualKey = null;
        return false;
    }

    public bool TryResolveServerFile(ReadOnlySpan<byte> rawPathBytes, [NotNullWhen(true)] out string? virtualKey)
    {
        int nullIdx = rawPathBytes.IndexOf((byte)0);
        if (nullIdx >= 0)
        {
            rawPathBytes = rawPathBytes[..nullIdx];
        }

        if (!rawPathBytes.IsEmpty)
        {
            string latin1Path = Encoding.Latin1.GetString(rawPathBytes).Trim();
            if (TryResolveServerFile(latin1Path, out virtualKey))
            {
                return true;
            }

            string gbkPath = GbkEncoding.Instance.GetString(rawPathBytes).Trim();
            if (!string.Equals(gbkPath, latin1Path, StringComparison.Ordinal) &&
                TryResolveServerFile(gbkPath, out virtualKey))
            {
                return true;
            }
        }

        virtualKey = null;
        return false;
    }

    public bool TryReadServerFile(string relativeOrVirtualKey, [NotNullWhen(true)] out byte[]? bytes)
    {
        if (TryFindServerEntry(relativeOrVirtualKey, out var entry))
        {
            bytes = entry.Content;
            return true;
        }

        bytes = null;
        return false;
    }

    public bool TryReadServerFile(ReadOnlySpan<byte> rawPathBytes, [NotNullWhen(true)] out byte[]? bytes)
    {
        if (TryResolveServerFile(rawPathBytes, out string? virtualKey) &&
            _serverFiles.TryGetValue(virtualKey, out var entry))
        {
            bytes = entry.Content;
            return true;
        }

        bytes = null;
        return false;
    }

    public string? TryResolveServerDirectory(ReadOnlySpan<byte> rawSubPathBytes, string parentSubDir = "maps")
    {
        int nullIdx = rawSubPathBytes.IndexOf((byte)0);
        if (nullIdx >= 0)
        {
            rawSubPathBytes = rawSubPathBytes[..nullIdx];
        }

        if (rawSubPathBytes.IsEmpty)
        {
            return null;
        }

        string basePrefix = NormalizeBackslash(parentSubDir);

        string latin1Sub = NormalizeBackslash(Encoding.Latin1.GetString(rawSubPathBytes));
        string cand1 = CombineVirtual(basePrefix, latin1Sub);
        if (TryFindServerDirKey(cand1, out string? canonicalDir))
        {
            return canonicalDir;
        }

        string gbkSub = NormalizeBackslash(GbkEncoding.Instance.GetString(rawSubPathBytes));
        if (!string.Equals(gbkSub, latin1Sub, StringComparison.Ordinal))
        {
            string cand2 = CombineVirtual(basePrefix, gbkSub);
            if (TryFindServerDirKey(cand2, out canonicalDir))
            {
                return canonicalDir;
            }
        }

        return null;
    }

    public IEnumerable<string> EnumerateServerFiles(string relativeSubDir, bool recursive = true)
    {
        string normalized = NormalizeBackslash(relativeSubDir);
        string prefix = string.Empty;
        if (normalized.Length > 0)
        {
            if (!TryFindServerDirKey(normalized, out string? resolvedDir))
            {
                return [];
            }

            prefix = resolvedDir + "\\";
        }

        var result = new List<string>();
        foreach (var entry in _serverFiles.Values)
        {
            string key = entry.CanonicalKey;
            if (prefix.Length > 0 && !key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (!recursive)
            {
                ReadOnlySpan<char> remainder = key.AsSpan(prefix.Length);
                if (remainder.IndexOf('\\') >= 0)
                {
                    continue;
                }
            }

            result.Add(key);
        }

        result.Sort(StringComparer.OrdinalIgnoreCase);
        return result;
    }

    public bool TryReadGatewayFile(string relativePath, [NotNullWhen(true)] out byte[]? bytes)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            bytes = null;
            return false;
        }

        string normalized = NormalizeBackslash(relativePath);
        if (_gatewayFiles.TryGetValue(normalized, out bytes))
        {
            return true;
        }

        byte[] gbkBytes = GbkEncoding.Instance.GetBytes(normalized);
        string mojibake = Encoding.Latin1.GetString(gbkBytes);
        if (!string.Equals(mojibake, normalized, StringComparison.Ordinal) &&
            _gatewayFiles.TryGetValue(mojibake, out bytes))
        {
            return true;
        }

        bytes = null;
        return false;
    }

    private bool TryFindServerEntry(string relativeOrEnginePath, out (string CanonicalKey, byte[] Content) entry)
    {
        entry = default;
        if (string.IsNullOrWhiteSpace(relativeOrEnginePath))
        {
            return false;
        }

        string trimmed = NormalizeBackslash(relativeOrEnginePath);
        if (trimmed.Length == 0)
        {
            return false;
        }

        if (_serverFiles.TryGetValue(trimmed, out entry))
        {
            return true;
        }

        byte[] gbkBytes = GbkEncoding.Instance.GetBytes(trimmed);
        string mojibake = Encoding.Latin1.GetString(gbkBytes);
        if (!string.Equals(mojibake, trimmed, StringComparison.Ordinal) &&
            _serverFiles.TryGetValue(mojibake, out entry))
        {
            return true;
        }

        if (IsAllLatin1(trimmed))
        {
            string decodedGbk = GbkEncoding.Instance.GetString(Encoding.Latin1.GetBytes(trimmed));
            if (!string.Equals(decodedGbk, trimmed, StringComparison.Ordinal) &&
                _serverFiles.TryGetValue(decodedGbk, out entry))
            {
                return true;
            }
        }

        return false;
    }

    private bool TryFindServerDirKey(string candidate, [NotNullWhen(true)] out string? canonicalDir)
    {
        string trimmed = NormalizeBackslash(candidate);
        if (_serverDirs.TryGetValue(trimmed, out canonicalDir))
        {
            return true;
        }

        byte[] gbkBytes = GbkEncoding.Instance.GetBytes(trimmed);
        string mojibake = Encoding.Latin1.GetString(gbkBytes);
        if (!string.Equals(mojibake, trimmed, StringComparison.Ordinal) &&
            _serverDirs.TryGetValue(mojibake, out canonicalDir))
        {
            return true;
        }

        if (IsAllLatin1(trimmed))
        {
            string decodedGbk = GbkEncoding.Instance.GetString(Encoding.Latin1.GetBytes(trimmed));
            if (!string.Equals(decodedGbk, trimmed, StringComparison.Ordinal) &&
                _serverDirs.TryGetValue(decodedGbk, out canonicalDir))
            {
                return true;
            }
        }

        canonicalDir = null;
        return false;
    }

    private static bool IsAllLatin1(string text)
    {
        for (int i = 0; i < text.Length; i++)
        {
            if (text[i] > 0xFF)
            {
                return false;
            }
        }

        return true;
    }

    private static string NormalizeBackslash(string path) =>
        (path ?? string.Empty).Trim().Replace('/', '\\').Trim('\\');

    private static string CombineVirtual(string prefix, string suffix)
    {
        if (prefix.Length == 0)
        {
            return suffix;
        }

        return suffix.Length == 0 ? prefix : $"{prefix}\\{suffix}";
    }

    private static void RegisterParentDirs(Dictionary<string, string> dirs, string fileKey)
    {
        int idx = 0;
        while ((idx = fileKey.IndexOf('\\', idx)) >= 0)
        {
            string dir = fileKey[..idx];
            dirs.TryAdd(dir, dir);
            idx++;
        }
    }

    private static void RegisterSelfAndParentDirs(Dictionary<string, string> dirs, string dirKey)
    {
        RegisterParentDirs(dirs, dirKey);
        if (dirKey.Length > 0)
        {
            dirs.TryAdd(dirKey, dirKey);
        }
    }

    private static bool TryClassifyRetainedEntry(
        string rawName,
        out bool isGateway,
        [NotNullWhen(true)] out string? relBackslashKey)
    {
        isGateway = false;
        relBackslashKey = null;

        if (!TryNormalizeArchivePath(rawName, out string? normalized))
        {
            return false;
        }

        int serverIdx = normalized.IndexOf("server1/", StringComparison.OrdinalIgnoreCase);
        if (serverIdx >= 0 && (serverIdx == 0 || normalized[serverIdx - 1] == '/'))
        {
            string rel = normalized[(serverIdx + "server1/".Length)..];
            if (rel.Length == 0)
            {
                return false;
            }

            if (IsNeededServer1RelativeFile(rel))
            {
                relBackslashKey = rel.Replace('/', '\\');
                return true;
            }

            return false;
        }

        int gwIdx = normalized.IndexOf("gateway/", StringComparison.OrdinalIgnoreCase);
        if (gwIdx >= 0 && (gwIdx == 0 || normalized[gwIdx - 1] == '/'))
        {
            string rel = normalized[(gwIdx + "gateway/".Length)..];
            if (string.Equals(rel, "settings/goddess_rolename.flt", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(rel, "goddess_rolename.flt", StringComparison.OrdinalIgnoreCase))
            {
                isGateway = true;
                relBackslashKey = rel.Replace('/', '\\');
                return true;
            }
        }

        return false;
    }

    private static bool TryClassifyRetainedDirectory(string rawName, [NotNullWhen(true)] out string? relDirKey)
    {
        relDirKey = null;
        if (!TryNormalizeArchivePath(rawName, out string? normalized))
        {
            return false;
        }

        string trimmed = normalized.TrimEnd('/');
        int serverIdx = trimmed.IndexOf("server1/", StringComparison.OrdinalIgnoreCase);
        if (serverIdx < 0 || (serverIdx != 0 && trimmed[serverIdx - 1] != '/'))
        {
            return false;
        }

        string rel = trimmed[(serverIdx + "server1/".Length)..];
        if (rel.Length == 0)
        {
            return false;
        }

        string withSlash = rel + "/";
        if (withSlash.StartsWith("settings/", StringComparison.OrdinalIgnoreCase) ||
            withSlash.StartsWith("maps/", StringComparison.OrdinalIgnoreCase) ||
            withSlash.StartsWith("script/", StringComparison.OrdinalIgnoreCase) ||
            withSlash.StartsWith("lang/", StringComparison.OrdinalIgnoreCase))
        {
            relDirKey = rel.Replace('/', '\\');
            return true;
        }

        return false;
    }

    private static bool IsNeededServer1RelativeFile(string relUnix) =>
        relUnix.StartsWith("settings/", StringComparison.OrdinalIgnoreCase) ||
        relUnix.StartsWith("maps/", StringComparison.OrdinalIgnoreCase) ||
        relUnix.StartsWith("script/", StringComparison.OrdinalIgnoreCase) ||
        relUnix.StartsWith("lang/", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(relUnix, "pak/maps.pak", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(relUnix, "data/maps.pak", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(relUnix, "maps.pak", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(relUnix, "rolename.flt", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(relUnix, "goddess_rolename.flt", StringComparison.OrdinalIgnoreCase);

    private static bool TryNormalizeArchivePath(string rawName, [NotNullWhen(true)] out string? normalized)
    {
        string s = rawName.Replace('\\', '/').TrimStart('/');
        while (s.StartsWith("./", StringComparison.Ordinal))
        {
            s = s[2..];
        }

        if (s.Length == 0 || s.Contains("..", StringComparison.Ordinal))
        {
            normalized = null;
            return false;
        }

        normalized = s;
        return true;
    }

    private static string ReadHeaderName(byte[] header, Encoding encoding)
    {
        string name = DecodeEntryName(header.AsSpan(0, 100), encoding);
        string prefix = DecodeEntryName(header.AsSpan(345, 155), encoding);
        return string.IsNullOrEmpty(prefix) ? name : $"{prefix}/{name}";
    }

    private static string DecodeEntryName(ReadOnlySpan<byte> bytes, Encoding encoding)
    {
        int nulIdx = bytes.IndexOf((byte)0);
        ReadOnlySpan<byte> trimmed = nulIdx >= 0 ? bytes[..nulIdx] : bytes;
        return encoding.GetString(trimmed).TrimEnd('\0', '\r', '\n');
    }

    private static long ParseOctalInt64(ReadOnlySpan<byte> field)
    {
        if (field.Length > 0 && (field[0] & 0x80) != 0)
        {
            long val = 0;
            for (int i = 1; i < field.Length; i++)
            {
                val = (val << 8) | field[i];
            }

            return val;
        }

        long result = 0;
        foreach (byte b in field)
        {
            if (b is 0 or (byte)' ')
            {
                continue;
            }

            if (b is < (byte)'0' or > (byte)'7')
            {
                break;
            }

            result = (result << 3) + (b - (byte)'0');
        }

        return result;
    }

    private static bool IsAllZero(ReadOnlySpan<byte> block)
    {
        foreach (byte b in block)
        {
            if (b != 0)
            {
                return false;
            }
        }

        return true;
    }

    private static bool ReadExactOrEof(Stream stream, byte[] buffer, int count)
    {
        int offset = 0;
        while (offset < count)
        {
            int read = stream.Read(buffer, offset, count - offset);
            if (read == 0)
            {
                return offset == 0 ? false : throw new EndOfStreamException("Unexpected end of tar stream.");
            }

            offset += read;
        }

        return true;
    }

    private static void ReadExact(Stream stream, byte[] buffer, int count)
    {
        if (!ReadExactOrEof(stream, buffer, count))
        {
            throw new EndOfStreamException("Unexpected end of tar entry payload.");
        }
    }

    private static byte[] ReadExactNewArray(Stream stream, long size)
    {
        if (size < 0 || size > int.MaxValue)
        {
            throw new InvalidDataException($"Archive entry size out of range: {size}.");
        }

        if (size == 0)
        {
            return [];
        }

        byte[] buffer = new byte[(int)size];
        ReadExact(stream, buffer, buffer.Length);
        return buffer;
    }

    private static void SkipBytes(Stream input, long count, byte[] buffer)
    {
        long remaining = count;
        while (remaining > 0)
        {
            int toRead = (int)Math.Min(buffer.Length, remaining);
            int read = input.Read(buffer, 0, toRead);
            if (read == 0)
            {
                throw new EndOfStreamException("Unexpected end of tar stream while skipping entry.");
            }

            remaining -= read;
        }
    }

    private static void SkipPadding(Stream input, long size)
    {
        long remainder = size % TarBlockSize;
        if (remainder == 0)
        {
            return;
        }

        int pad = (int)(TarBlockSize - remainder);
        byte[] scratch = new byte[pad];
        ReadExact(input, scratch, pad);
    }
}
