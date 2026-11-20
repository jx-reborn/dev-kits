using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace JXReborn.LegacyImporter.Sources;

/// <summary>
/// Unified read-only abstraction over legacy server data,
/// supporting direct folder access and zero-extraction in-memory <c>.tar.gz</c> archives.
/// </summary>
public interface ILegacyServerSource
{
    /// <summary>
    /// Gets a human-readable description of the resolved source (archive summary or folder path).
    /// </summary>
    string DisplayName { get; }

    /// <summary>
    /// Resolves a string path relative to <c>server1</c> into a canonical <c>\</c>-separated virtual key.
    /// </summary>
    bool TryResolveServerFile(string relativeOrEnginePath, [NotNullWhen(true)] out string? virtualKey);

    /// <summary>
    /// Resolves a raw legacy byte path (GBK or Latin-1) relative to <c>server1</c> into a canonical <c>\</c>-separated virtual key.
    /// </summary>
    bool TryResolveServerFile(ReadOnlySpan<byte> rawPathBytes, [NotNullWhen(true)] out string? virtualKey);

    /// <summary>
    /// Reads the raw bytes of a file inside <c>server1</c> using either a relative path or an existing virtual key.
    /// </summary>
    bool TryReadServerFile(string relativeOrVirtualKey, [NotNullWhen(true)] out byte[]? bytes);

    /// <summary>
    /// Reads the raw bytes of a file inside <c>server1</c> using raw legacy GBK/Latin-1 path bytes.
    /// </summary>
    bool TryReadServerFile(ReadOnlySpan<byte> rawPathBytes, [NotNullWhen(true)] out byte[]? bytes);

    /// <summary>
    /// Resolves a raw byte sub-directory path under <paramref name="parentSubDir"/> (e.g. <c>maps</c>)
    /// into a canonical <c>\</c>-separated virtual directory key relative to <c>server1</c>.
    /// </summary>
    string? TryResolveServerDirectory(ReadOnlySpan<byte> rawSubPathBytes, string parentSubDir = "maps");

    /// <summary>
    /// Enumerates <c>\</c>-separated virtual file keys under a <c>server1</c> sub-directory.
    /// </summary>
    IEnumerable<string> EnumerateServerFiles(string relativeSubDir, bool recursive = true);

    /// <summary>
    /// Reads a file from the companion <c>gateway</c> root directory (when available).
    /// </summary>
    bool TryReadGatewayFile(string relativePath, [NotNullWhen(true)] out byte[]? bytes);
}
