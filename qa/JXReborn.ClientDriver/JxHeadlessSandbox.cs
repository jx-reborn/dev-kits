using System.Diagnostics;
using System.Globalization;

namespace JXReborn.ClientDriver;

/// <summary>
/// Manages an isolated per-instance working directory for a headless <c>game.exe</c> process.
/// Read-only asset directories are NTFS directory junctions while writable state folders and
/// <c>config.ini</c> are materialized per sandbox, and all junctions are safely unlinked before cleanup.
/// </summary>
public sealed class JxHeadlessSandbox : IDisposable
{
    private static readonly string[] RequiredBinaries =
    [
        "game.exe",
        "winmm.dll",
        "represent2.dll",
        "engine.dll",
        "rainbow.dll",
        "lualibdll.dll",
        "zlib.dll",
        "filtertext.dll"
    ];

    private static readonly string[] AssetJunctionNames =
    [
        "data",
        "data1",
        "music",
        "spr",
        "lang",
        "script",
        "ui",
        "pic",
        "game",
        "assets",
        "Login"
    ];

    private static readonly string[] WritableSubdirectoryNames =
    [
        "settings",
        "UserData"
    ];

    private bool _disposed;

    private JxHeadlessSandbox(string sandboxDirectory, string executablePath, string crashDumpPath, bool preserveOnDispose)
    {
        SandboxDirectory = sandboxDirectory;
        ExecutablePath = executablePath;
        CrashDumpPath = crashDumpPath;
        PreserveOnDispose = preserveOnDispose;
    }

    /// <summary>
    /// Full path to the isolated sandbox working directory.
    /// </summary>
    public string SandboxDirectory { get; }

    /// <summary>
    /// Full path to the sandboxed <c>game.exe</c>.
    /// </summary>
    public string ExecutablePath { get; }

    /// <summary>
    /// Full path where native crash dumps will be written for this instance.
    /// </summary>
    public string CrashDumpPath { get; }

    /// <summary>
    /// Controls whether <see cref="Dispose"/> keeps the sandbox directory on disk.
    /// </summary>
    public bool PreserveOnDispose { get; set; }

    /// <summary>
    /// Provisions an isolated sandbox directory under <paramref name="sandboxRootDirectory"/> from <paramref name="templateDirectory"/>.
    /// </summary>
    public static JxHeadlessSandbox Create(
        string templateDirectory,
        string sandboxRootDirectory,
        string instanceId,
        int gatewayPort = 5622,
        string? crashDumpPath = null,
        bool preserveOnDispose = false,
        bool interactiveWindow = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(templateDirectory);
        ArgumentException.ThrowIfNullOrWhiteSpace(sandboxRootDirectory);
        ArgumentException.ThrowIfNullOrWhiteSpace(instanceId);

        string resolvedTemplate = Path.GetFullPath(templateDirectory);
        if (!Directory.Exists(resolvedTemplate))
        {
            throw new DirectoryNotFoundException(
                $"Headless client template directory does not exist: '{resolvedTemplate}'. Run qa/JXReborn.ClientDriver.Native/build-native.ps1 first.");
        }

        foreach (string required in RequiredBinaries)
        {
            string candidate = Path.Combine(resolvedTemplate, required);
            if (!File.Exists(candidate))
            {
                string extraHint = string.Equals(required, "game.exe", StringComparison.OrdinalIgnoreCase)
                    ? " If Windows Security quarantined artifacts/headless-client/game.exe, add the artifacts directory to Windows Security Exclusions and re-run build-native.ps1."
                    : " Run qa/JXReborn.ClientDriver.Native/build-native.ps1 to provision artifacts/headless-client.";
                throw new FileNotFoundException(
                    $"Required headless client file '{required}' is missing from '{resolvedTemplate}'.{extraHint}",
                    candidate);
            }
        }

        string resolvedSandboxRoot = Path.GetFullPath(sandboxRootDirectory);
        Directory.CreateDirectory(resolvedSandboxRoot);

        string sandboxDir = Path.Combine(resolvedSandboxRoot, instanceId);
        if (Directory.Exists(sandboxDir))
        {
            DeleteDirectoryWithJunctionSafety(sandboxDir);
        }

        Directory.CreateDirectory(sandboxDir);

        try
        {
            // Copy top-level files (DLLs, exe, ini, package.ini) except config.ini which is generated per port.
            foreach (string srcFile in Directory.EnumerateFiles(resolvedTemplate, "*", SearchOption.TopDirectoryOnly))
            {
                string fileName = Path.GetFileName(srcFile);
                if (string.Equals(fileName, "config.ini", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                File.Copy(srcFile, Path.Combine(sandboxDir, fileName), overwrite: true);
            }

            // Materialize NTFS directory junctions for read-only asset folders (resolving final target so we do not chain junctions).
            foreach (string dirName in AssetJunctionNames)
            {
                string templateSubDir = Path.Combine(resolvedTemplate, dirName);
                if (!Directory.Exists(templateSubDir))
                {
                    continue;
                }

                string targetPath = ResolveJunctionTarget(templateSubDir);
                string dstSubDir = Path.Combine(sandboxDir, dirName);
                CreateDirectoryJunction(dstSubDir, targetPath);

                if (interactiveWindow)
                {
                    string? sourceClientRoot = Directory.GetParent(targetPath)?.FullName;
                    if (!string.IsNullOrWhiteSpace(sourceClientRoot))
                    {
                        string realRepresent = Path.Combine(sourceClientRoot, "represent2.dll");
                        if (File.Exists(realRepresent))
                        {
                            File.Copy(realRepresent, Path.Combine(sandboxDir, "represent2.dll"), overwrite: true);
                        }
                    }
                }
            }

            string sandboxPackageIni = Path.Combine(sandboxDir, "package.ini");
            if (File.Exists(sandboxPackageIni))
            {
                bool hasDataMaps = File.Exists(Path.Combine(sandboxDir, "data", "maps.pak"));
                bool hasData1Maps = File.Exists(Path.Combine(sandboxDir, "data1", "maps.pak"));
                if (!hasDataMaps && hasData1Maps)
                {
                    var latin1 = System.Text.Encoding.GetEncoding(28591);
                    string pkgText = File.ReadAllText(sandboxPackageIni, latin1);
                    string updatedPkgText = System.Text.RegularExpressions.Regex.Replace(
                        pkgText,
                        @"(?m)^(Path\s*=\s*)\\\\?data(?=[ \t]*\r?$)",
                        @"${1}\\data1");
                    if (!string.Equals(pkgText, updatedPkgText, StringComparison.Ordinal))
                    {
                        File.WriteAllText(sandboxPackageIni, updatedPkgText, latin1);
                    }
                }
                else if (hasDataMaps && !hasData1Maps)
                {
                    var latin1 = System.Text.Encoding.GetEncoding(28591);
                    string pkgText = File.ReadAllText(sandboxPackageIni, latin1);
                    string updatedPkgText = System.Text.RegularExpressions.Regex.Replace(
                        pkgText,
                        @"(?m)^(Path\s*=\s*)\\\\?data1(?=[ \t]*\r?$)",
                        @"${1}\\data");
                    if (!string.Equals(pkgText, updatedPkgText, StringComparison.Ordinal))
                    {
                        File.WriteAllText(sandboxPackageIni, updatedPkgText, latin1);
                    }
                }
            }

            // Copy writable folders cleanly per client instance.
            foreach (string writableName in WritableSubdirectoryNames)
            {
                string srcWritable = Path.Combine(resolvedTemplate, writableName);
                if (Directory.Exists(srcWritable))
                {
                    CopyDirectoryRecursive(srcWritable, Path.Combine(sandboxDir, writableName));
                }
            }

            Directory.CreateDirectory(Path.Combine(sandboxDir, "Logs"));

            string configText = string.Create(
                CultureInfo.InvariantCulture,
                $"""
                [Server]
                ServerOn=0
                GameServPort={gatewayPort}
                DenialPort=5623

                [Client]
                CanOpenUrlWhenExit=0
                FullScreen=0
                Represent=2
                FPS=0
                DynamicLight=0
                CapPath=.\pic
                KeepOutSide=1
                AwardPosition=-1
                Bil_StartAuto=0
                ShowMiniskill=1
                Plugin=0
                Theme=CTC
                """);
            File.WriteAllText(Path.Combine(sandboxDir, "config.ini"), configText + Environment.NewLine, System.Text.Encoding.ASCII);

            string resolvedCrashDump = string.IsNullOrWhiteSpace(crashDumpPath)
                ? Path.Combine(sandboxDir, $"{instanceId}-crash.json")
                : Path.GetFullPath(crashDumpPath);

            return new JxHeadlessSandbox(
                sandboxDir,
                Path.Combine(sandboxDir, "game.exe"),
                resolvedCrashDump,
                preserveOnDispose);
        }
        catch
        {
            TryDeleteDirectoryWithJunctionSafety(sandboxDir);
            throw;
        }
    }

    /// <summary>
    /// Removes this sandbox directory after unlinking any NTFS junctions non-recursively.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        if (!PreserveOnDispose)
        {
            TryDeleteDirectoryWithJunctionSafety(SandboxDirectory);
        }

        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Safely deletes a directory tree by unlinking every reparse-point junction non-recursively first,
    /// guaranteeing external junction targets are never traversed or deleted.
    /// </summary>
    public static void DeleteDirectoryWithJunctionSafety(string rootDirectory)
    {
        if (!Directory.Exists(rootDirectory))
        {
            return;
        }

        UnlinkTopLevelReparsePoints(rootDirectory);

        // Guard against nested reparse points inside subdirectories without crossing reparse boundaries.
        foreach (string child in Directory.EnumerateDirectories(rootDirectory, "*", SearchOption.TopDirectoryOnly))
        {
            UnlinkNestedReparsePoints(child);
        }

        Directory.Delete(rootDirectory, recursive: true);
    }

    private static void UnlinkTopLevelReparsePoints(string directory)
    {
        foreach (string sub in Directory.EnumerateDirectories(directory, "*", SearchOption.TopDirectoryOnly))
        {
            if ((File.GetAttributes(sub) & FileAttributes.ReparsePoint) != 0)
            {
                Directory.Delete(sub, recursive: false);
            }
        }
    }

    private static void UnlinkNestedReparsePoints(string directory)
    {
        if ((File.GetAttributes(directory) & FileAttributes.ReparsePoint) != 0)
        {
            Directory.Delete(directory, recursive: false);
            return;
        }

        foreach (string sub in Directory.EnumerateDirectories(directory, "*", SearchOption.TopDirectoryOnly))
        {
            UnlinkNestedReparsePoints(sub);
        }
    }

    private static void TryDeleteDirectoryWithJunctionSafety(string rootDirectory)
    {
        for (int attempt = 0; attempt < 3; attempt++)
        {
            try
            {
                DeleteDirectoryWithJunctionSafety(rootDirectory);
                return;
            }
            catch (IOException) when (attempt < 2)
            {
                Thread.Sleep(50);
            }
            catch (UnauthorizedAccessException) when (attempt < 2)
            {
                Thread.Sleep(50);
            }
        }
    }

    private static string ResolveJunctionTarget(string directoryPath)
    {
        DirectoryInfo info = new(directoryPath);
        FileSystemInfo? resolved = info.ResolveLinkTarget(returnFinalTarget: true);
        return resolved is not null ? resolved.FullName : info.FullName;
    }

    private static void CreateDirectoryJunction(string linkPath, string targetPath)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "cmd.exe",
            Arguments = $"/c mklink /J \"{linkPath}\" \"{targetPath}\"",
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        using Process proc = Process.Start(psi)
            ?? throw new InvalidOperationException($"Unable to start cmd.exe to create junction '{linkPath}'.");
        string stderr = proc.StandardError.ReadToEnd();
        proc.WaitForExit();
        if (proc.ExitCode != 0)
        {
            throw new IOException($"Failed to create directory junction '{linkPath}' -> '{targetPath}': {stderr.Trim()}");
        }
    }

    private static void CopyDirectoryRecursive(string sourceDir, string destinationDir)
    {
        Directory.CreateDirectory(destinationDir);
        foreach (string file in Directory.EnumerateFiles(sourceDir, "*", SearchOption.TopDirectoryOnly))
        {
            File.Copy(file, Path.Combine(destinationDir, Path.GetFileName(file)), overwrite: true);
        }

        foreach (string subDir in Directory.EnumerateDirectories(sourceDir, "*", SearchOption.TopDirectoryOnly))
        {
            string targetSubDir = Path.Combine(destinationDir, Path.GetFileName(subDir));
            if ((File.GetAttributes(subDir) & FileAttributes.ReparsePoint) != 0)
            {
                CreateDirectoryJunction(targetSubDir, ResolveJunctionTarget(subDir));
            }
            else
            {
                CopyDirectoryRecursive(subDir, targetSubDir);
            }
        }
    }
}
