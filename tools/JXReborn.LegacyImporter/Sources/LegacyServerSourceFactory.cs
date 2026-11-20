using System;
using System.IO;

namespace JXReborn.LegacyImporter.Sources;

/// <summary>
/// Resolves default legacy source locations and constructs the appropriate <see cref="ILegacyServerSource"/>
/// for either a directory or a <c>.tar.gz</c>/<c>.tgz</c> archive.
/// </summary>
public static class LegacyServerSourceFactory
{
    public static ILegacyServerSource Create(string sourcePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);

        return TarGzInMemoryServerSource.IsTarGzArchivePath(sourcePath)
            ? TarGzInMemoryServerSource.Load(sourcePath)
            : new FolderLegacyServerSource(sourcePath);
    }

    public static string ResolveDefaultSourcePath(string? explicitSource, string artifactsDir)
    {
        if (!string.IsNullOrWhiteSpace(explicitSource))
        {
            return explicitSource;
        }

        string? envArchive = Environment.GetEnvironmentVariable("LEGACY_SERVER_ARCHIVE");
        if (!string.IsNullOrWhiteSpace(envArchive) && File.Exists(envArchive))
        {
            return envArchive;
        }

        string? envDir = Environment.GetEnvironmentVariable("LEGACY_SERVER_DIR");
        if (!string.IsNullOrWhiteSpace(envDir) && Directory.Exists(envDir))
        {
            return envDir;
        }

        string fallbackDir = Path.GetFullPath(Path.Combine("..", "legacy-server"));
        if (Directory.Exists(fallbackDir))
        {
            string server1Subdir = Path.Combine(fallbackDir, "server1");
            return Directory.Exists(server1Subdir) ? server1Subdir : fallbackDir;
        }

        throw new FileNotFoundException(
            "Legacy server source path was not specified. Pass --source <path-or-archive> or set LEGACY_SERVER_DIR / LEGACY_SERVER_ARCHIVE.");
    }
}
