using System.Diagnostics;
using JXReborn.Primitives.Encodings;
using Xunit;

namespace JXReborn.ClientDriver.Tests.Sandboxes;

public sealed class ClientDriverSandboxAndModelsTests
{
    [Fact]
    [Trait("Category", "Unit")]
    public void WorldDomModels_ShouldDecodeTcvn3HexProperties()
    {
        string expected = "KiếmHiệp";
        string hex = Convert.ToHexString(Tcvn3Encoding.EncodeBytes(expected)) + "0000";

        var role = new RoleSlotSnapshot(0, hex, 0, 0, 53);
        Assert.Equal(expected, role.Name);

        var dialog = new DialogSnapshot(
            IsOpen: true,
            Sequence: 1,
            OpenedTick: 42,
            PromptTcvn3Hex: hex,
            OptionsTcvn3Hex: [hex]);
        Assert.Equal(expected, dialog.Prompt);
        Assert.Single(dialog.Options);
        Assert.Equal(expected, dialog.Options[0]);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void SandboxDispose_ShouldUnlinkNtfsJunctionsWithoutDeletingTargetFiles()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }
        string tempRoot = Path.Combine(Path.GetTempPath(), $"jx-sandbox-unit-{Guid.NewGuid():N}");
        string targetDataDir = Path.Combine(tempRoot, "readonly-target", "data");
        string templateDir = Path.Combine(tempRoot, "template");
        string sandboxRoot = Path.Combine(tempRoot, "sandboxes");

        try
        {
            Directory.CreateDirectory(targetDataDir);
            string sentinelFile = Path.Combine(targetDataDir, "sentinel.txt");
            File.WriteAllText(sentinelFile, "DO_NOT_DELETE");

            Directory.CreateDirectory(templateDir);
            foreach (string dll in new[] { "game.exe", "winmm.dll", "represent2.dll", "engine.dll", "rainbow.dll", "lualibdll.dll", "zlib.dll", "filtertext.dll" })
            {
                File.WriteAllText(Path.Combine(templateDir, dll), "dummy");
            }

            Directory.CreateDirectory(Path.Combine(templateDir, "settings"));
            File.WriteAllText(Path.Combine(templateDir, "settings", "test.ini"), "ok");

            string templateDataJunction = Path.Combine(templateDir, "data");
            CreateJunction(templateDataJunction, targetDataDir);

            using (JxHeadlessSandbox sandbox = JxHeadlessSandbox.Create(
                templateDir,
                sandboxRoot,
                "unit-inst-1",
                gatewayPort: 5622))
            {
                Assert.True(File.Exists(Path.Combine(sandbox.SandboxDirectory, "data", "sentinel.txt")));
                Assert.Contains("GameServPort=5622", File.ReadAllText(Path.Combine(sandbox.SandboxDirectory, "config.ini")), StringComparison.Ordinal);
            }

            Assert.True(File.Exists(sentinelFile), "Sentinel file inside junction target must remain untouched after sandbox disposal.");
            Assert.False(Directory.Exists(Path.Combine(sandboxRoot, "unit-inst-1")));
        }
        finally
        {
            if (Directory.Exists(tempRoot))
            {
                JxHeadlessSandbox.DeleteDirectoryWithJunctionSafety(tempRoot);
            }
        }
    }

    private static void CreateJunction(string linkPath, string targetPath)
    {
        using Process proc = Process.Start(new ProcessStartInfo
        {
            FileName = "cmd.exe",
            Arguments = $"/c mklink /J \"{linkPath}\" \"{targetPath}\"",
            UseShellExecute = false,
            CreateNoWindow = true
        })!;
        proc.WaitForExit();
        Assert.Equal(0, proc.ExitCode);
    }
}
