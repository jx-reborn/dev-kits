using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using JXReborn.LegacyImporter.Legacy;

namespace JXReborn.LegacyImporter.Sources;

/// <summary>
/// Direct filesystem <see cref="ILegacyServerSource"/> reading legacy <c>server1</c> and <c>gateway</c>
/// directories directly without temporary extraction.
/// </summary>
public sealed class FolderLegacyServerSource : ILegacyServerSource
{
    public string Server1RootPath { get; }

    public string GatewayRootPath { get; }

    public string DisplayName { get; }

    public FolderLegacyServerSource(string folderPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(folderPath);
        string full = Path.GetFullPath(folderPath);
        if (!Directory.Exists(full))
        {
            throw new DirectoryNotFoundException($"Source folder not found: '{full}'.");
        }

        if (LegacyPathResolver.TryResolveDirectoryStringPath(full, "server1") is { } subServer1 &&
            LegacyPathResolver.TryResolveDirectoryStringPath(full, "settings") is null)
        {
            Server1RootPath = subServer1;
            GatewayRootPath = LegacyPathResolver.TryResolveDirectoryStringPath(full, "gateway")
                              ?? Path.Combine(full, "gateway");
        }
        else
        {
            Server1RootPath = full;
            string? parent = Path.GetDirectoryName(
                full.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
            GatewayRootPath = parent is not null
                ? (LegacyPathResolver.TryResolveDirectoryStringPath(parent, "gateway") ?? Path.Combine(parent, "gateway"))
                : string.Empty;
        }

        DisplayName = $"folder:{Server1RootPath}";
    }

    public bool TryResolveServerFile(string relativeOrEnginePath, [NotNullWhen(true)] out string? virtualKey)
    {
        string? fullPath = LegacyPathResolver.TryResolveStringPath(Server1RootPath, relativeOrEnginePath);
        if (fullPath is null || !File.Exists(fullPath))
        {
            virtualKey = null;
            return false;
        }

        virtualKey = ToVirtualKey(fullPath);
        return true;
    }

    public bool TryResolveServerFile(ReadOnlySpan<byte> rawPathBytes, [NotNullWhen(true)] out string? virtualKey)
    {
        string? fullPath = LegacyPathResolver.TryResolveFile(Server1RootPath, rawPathBytes);
        if (fullPath is null || !File.Exists(fullPath))
        {
            virtualKey = null;
            return false;
        }

        virtualKey = ToVirtualKey(fullPath);
        return true;
    }

    public bool TryReadServerFile(string relativeOrVirtualKey, [NotNullWhen(true)] out byte[]? bytes)
    {
        string? fullPath = LegacyPathResolver.TryResolveStringPath(Server1RootPath, relativeOrVirtualKey);
        if (fullPath is null || !File.Exists(fullPath))
        {
            bytes = null;
            return false;
        }

        bytes = File.ReadAllBytes(fullPath);
        return true;
    }

    public bool TryReadServerFile(ReadOnlySpan<byte> rawPathBytes, [NotNullWhen(true)] out byte[]? bytes)
    {
        string? fullPath = LegacyPathResolver.TryResolveFile(Server1RootPath, rawPathBytes);
        if (fullPath is null || !File.Exists(fullPath))
        {
            bytes = null;
            return false;
        }

        bytes = File.ReadAllBytes(fullPath);
        return true;
    }

    public string? TryResolveServerDirectory(ReadOnlySpan<byte> rawSubPathBytes, string parentSubDir = "maps")
    {
        string? fullDir = LegacyPathResolver.TryResolveDirectory(Server1RootPath, rawSubPathBytes, parentSubDir);
        return fullDir is null ? null : ToVirtualKey(fullDir);
    }

    public IEnumerable<string> EnumerateServerFiles(string relativeSubDir, bool recursive = true)
    {
        string? targetDir = LegacyPathResolver.TryResolveDirectoryStringPath(Server1RootPath, relativeSubDir);
        if (targetDir is null || !Directory.Exists(targetDir))
        {
            return [];
        }

        SearchOption option = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
        return Directory.EnumerateFiles(targetDir, "*", option)
            .Select(ToVirtualKey)
            .OrderBy(static k => k, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public bool TryReadGatewayFile(string relativePath, [NotNullWhen(true)] out byte[]? bytes)
    {
        if (string.IsNullOrWhiteSpace(GatewayRootPath) || !Directory.Exists(GatewayRootPath))
        {
            bytes = null;
            return false;
        }

        string? fullPath = LegacyPathResolver.TryResolveStringPath(GatewayRootPath, relativePath);
        if (fullPath is null || !File.Exists(fullPath))
        {
            bytes = null;
            return false;
        }

        bytes = File.ReadAllBytes(fullPath);
        return true;
    }

    private string ToVirtualKey(string fullPath) =>
        Path.GetRelativePath(Server1RootPath, fullPath)
            .Replace('/', '\\')
            .Trim('\\');
}
