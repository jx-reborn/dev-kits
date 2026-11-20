using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using JXReborn.LegacyImporter.Sources;
using Xunit;

namespace JXReborn.GameAssets.Tests.Sources;

public sealed class LegacyServerSourceTests
{
    [Fact]
    public void TarGzInMemoryServerSource_WhenLoadedFromArchive_ShouldResolveGbkAndGatewayFilesInMemoryWithoutDiskExtraction()
    {
        string tempDir = Path.Combine(Path.GetTempPath(), $"jx-src-tgz-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        string archivePath = Path.Combine(tempDir, "test-source.tar.gz");

        byte[] factionContent = Encoding.Latin1.GetBytes("[0]\nName=shaolin\n");
        byte[] npcsContent = Encoding.Latin1.GetBytes("Name\tKind\nTestNpc\t3\n");
        byte[] rolenameContent = Encoding.Latin1.GetBytes("@+GM\n");
        byte[] ignoredBinary = [0x7F, (byte)'E', (byte)'L', (byte)'F'];

        // Raw GBK bytes for "门派设定" encoded losslessly in TAR header as Latin-1
        byte[] gbkFactionBytes = [0xC3, 0xC5, 0xC5, 0xC9, 0xC9, 0xE8, 0xB6, 0xA8];
        string latin1FactionFile = Encoding.Latin1.GetString(gbkFactionBytes) + ".ini";

        using (FileStream fs = File.Create(archivePath))
        using (var gz = new GZipStream(fs, CompressionLevel.Optimal))
        {
            WriteUstarEntry(gz, $"legacy/server1/settings/faction/{latin1FactionFile}", factionContent);
            WriteUstarEntry(gz, "legacy/server1/settings/npcs.txt", npcsContent);
            WriteUstarEntry(gz, "legacy/gateway/settings/goddess_rolename.flt", rolenameContent);
            WriteUstarEntry(gz, "legacy/server1/jx_linux_y", ignoredBinary);
            gz.Write(new byte[1024]);
        }

        try
        {
            ILegacyServerSource source = LegacyServerSourceFactory.Create(archivePath);
            Assert.IsType<TarGzInMemoryServerSource>(source);

            Assert.True(source.TryReadServerFile(@"settings\faction\门派设定.ini", out byte[]? actualFaction));
            Assert.Equal(factionContent, actualFaction);

            Assert.True(source.TryReadServerFile(@"settings\npcs.txt", out byte[]? actualNpcs));
            Assert.Equal(npcsContent, actualNpcs);

            Assert.True(source.TryReadGatewayFile(@"settings\goddess_rolename.flt", out byte[]? actualFilter));
            Assert.Equal(rolenameContent, actualFilter);

            Assert.False(source.TryReadServerFile("jx_linux_y", out _));
            Assert.Empty(Directory.GetDirectories(tempDir));
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public void FolderLegacyServerSource_WhenGivenParentOrServer1Directory_ShouldResolveServerAndGatewayFiles()
    {
        string tempRoot = Path.Combine(Path.GetTempPath(), $"jx-src-dir-{Guid.NewGuid():N}");
        string server1Dir = Path.Combine(tempRoot, "server1");
        string gatewaySettingsDir = Path.Combine(tempRoot, "gateway", "settings");
        Directory.CreateDirectory(Path.Combine(server1Dir, "settings", "item"));
        Directory.CreateDirectory(gatewaySettingsDir);

        byte[] testBytes = Encoding.UTF8.GetBytes("col0\nval0\n");
        byte[] filterBytes = Encoding.UTF8.GetBytes("@+GameMaster\n");
        File.WriteAllBytes(Path.Combine(server1Dir, "settings", "item", "test.txt"), testBytes);
        File.WriteAllBytes(Path.Combine(gatewaySettingsDir, "goddess_rolename.flt"), filterBytes);

        try
        {
            var fromParent = new FolderLegacyServerSource(tempRoot);
            var fromServer1 = new FolderLegacyServerSource(server1Dir);

            Assert.True(fromParent.TryReadServerFile(@"settings\item\test.txt", out byte[]? parentServerBytes));
            Assert.Equal(testBytes, parentServerBytes);
            Assert.True(fromParent.TryReadGatewayFile(@"settings\goddess_rolename.flt", out byte[]? parentGwBytes));
            Assert.Equal(filterBytes, parentGwBytes);

            Assert.True(fromServer1.TryReadServerFile(@"settings\item\test.txt", out byte[]? subServerBytes));
            Assert.Equal(testBytes, subServerBytes);
            Assert.True(fromServer1.TryReadGatewayFile(@"settings\goddess_rolename.flt", out byte[]? subGwBytes));
            Assert.Equal(filterBytes, subGwBytes);

            string[] parentFiles = fromParent.EnumerateServerFiles("settings").ToArray();
            string[] subFiles = fromServer1.EnumerateServerFiles("settings").ToArray();
            Assert.Equal(parentFiles, subFiles);
            Assert.Equal([@"settings\item\test.txt"], parentFiles);
        }
        finally
        {
            Directory.Delete(tempRoot, recursive: true);
        }
    }

    [Fact]
    public void LegacyServerSourceFactory_WhenPathDoesNotExist_ShouldThrowNotFoundException()
    {
        string nonExistentBase = Path.Combine(Path.GetTempPath(), $"jx-src-missing-{Guid.NewGuid():N}");

        Assert.Throws<FileNotFoundException>(() => LegacyServerSourceFactory.Create(nonExistentBase + ".tar.gz"));
        Assert.Throws<DirectoryNotFoundException>(() => LegacyServerSourceFactory.Create(nonExistentBase));
    }

    private static void WriteUstarEntry(Stream stream, string latin1Path, byte[] data)
    {
        byte[] header = new byte[512];
        byte[] pathBytes = Encoding.Latin1.GetBytes(latin1Path);
        Array.Copy(pathBytes, 0, header, 0, Math.Min(100, pathBytes.Length));
        Encoding.ASCII.GetBytes("0000644\0").CopyTo(header, 100);
        Encoding.ASCII.GetBytes("0000000\0").CopyTo(header, 108);
        Encoding.ASCII.GetBytes("0000000\0").CopyTo(header, 116);
        Encoding.ASCII.GetBytes(Convert.ToString(data.Length, 8).PadLeft(11, '0') + "\0").CopyTo(header, 124);
        Encoding.ASCII.GetBytes("00000000000\0").CopyTo(header, 136);
        header.AsSpan(148, 8).Fill((byte)' ');
        header[156] = (byte)'0';
        Encoding.ASCII.GetBytes("ustar\0").CopyTo(header, 257);
        Encoding.ASCII.GetBytes("00").CopyTo(header, 263);

        int checksum = 0;
        for (int i = 0; i < 512; i++)
        {
            checksum += header[i];
        }

        Encoding.ASCII.GetBytes(Convert.ToString(checksum, 8).PadLeft(6, '0') + "\0 ").CopyTo(header, 148);
        stream.Write(header, 0, 512);

        if (data.Length > 0)
        {
            stream.Write(data, 0, data.Length);
            int pad = (512 - (data.Length % 512)) % 512;
            if (pad > 0)
            {
                stream.Write(new byte[pad], 0, pad);
            }
        }
    }
}
