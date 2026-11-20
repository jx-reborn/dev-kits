using System.Buffers.Binary;
using System.Globalization;
using System.Text;
using JXReborn.GameAssets.WorldMaps;
using JXReborn.Primitives.WorldZones;
using JXReborn.ClientDriver.Tests.Configuration;

namespace JXReborn.ClientDriver.Tests.WorldJourneys;

/// <summary>
/// Reads <c>Region_C.dat</c> Section 0 (<c>16 * 32</c> obstacle cells) directly from the provisioned headless client's
/// <c>maps.pak</c> (Legacy XPack + NRV2B_8) so journey pathfinding and client-vs-server map audits use exact client terrain.
/// </summary>
public sealed class ClientPakObstacleReader
{
    private readonly IReadOnlyList<(string PakPath, Dictionary<uint, (uint Offset, uint UncompressedLength, uint CompressedLength, byte Method)> Index)> _mountedPaks;
    private readonly Dictionary<ushort, byte[]> _mapSubPathBytesById;
    private readonly Dictionary<(ushort MapId, int RegionX, int RegionY), int[]> _regionCache = [];

    private ClientPakObstacleReader(
        IReadOnlyList<(string PakPath, Dictionary<uint, (uint Offset, uint UncompressedLength, uint CompressedLength, byte Method)> Index)> mountedPaks,
        Dictionary<ushort, byte[]> mapSubPathBytesById)
    {
        _mountedPaks = mountedPaks;
        _mapSubPathBytesById = mapSubPathBytesById;
    }

    public static ClientPakObstacleReader? TryOpenFromTemplate(string? templateDirectory = null)
    {
        string resolvedTemplate = string.IsNullOrWhiteSpace(templateDirectory)
            ? QaWorkspacePaths.HeadlessClientTemplateDirectory
            : Path.GetFullPath(templateDirectory);

        string packageIniPath = Path.Combine(resolvedTemplate, "package.ini");
        List<(string PakPath, Dictionary<uint, (uint Offset, uint UncompressedLength, uint CompressedLength, byte Method)> Index)> mountedPaks =
            LoadMountedPaks(resolvedTemplate, packageIniPath);
        if (mountedPaks.Count == 0)
        {
            return null;
        }

        byte[]? mapListRaw = null;
        string looseMapListPath = Path.Combine(resolvedTemplate, "settings", "MapList.ini");
        if (File.Exists(looseMapListPath))
        {
            mapListRaw = File.ReadAllBytes(looseMapListPath);
        }

        mapListRaw ??= TryReadPakEntryBytes(mountedPaks, "\\settings\\maplist.ini"u8);
        if (mapListRaw is null)
        {
            return null;
        }

        Dictionary<ushort, byte[]> subPaths = ParseMapListSubPaths(mapListRaw);
        return new ClientPakObstacleReader(mountedPaks, subPaths);
    }

    public int GetCellObstacle(ushort mapId, int mpsX, int mpsY)
    {
        if (mpsX < 0 || mpsY < 0)
        {
            return 1;
        }

        int cellX = mpsX / WorldCoordinateConsts.CellWidthMps;
        int cellY = mpsY / WorldCoordinateConsts.CellHeightMps;
        int rx = cellX / WorldCoordinateConsts.RegionWidthCells;
        int lx = cellX % WorldCoordinateConsts.RegionWidthCells;
        int ry = cellY / WorldCoordinateConsts.RegionHeightCells;
        int ly = cellY % WorldCoordinateConsts.RegionHeightCells;

        int[] cells = GetOrLoadRegionCells(mapId, rx, ry);
        return cells[(lx * WorldCoordinateConsts.RegionHeightCells) + ly] & 0x0F;
    }

    public (int CheckedRegions, int MismatchedRegions, int MismatchedCells) AuditPassabilityAgainstServer(
        IMapCatalog mapCatalog,
        ushort mapId,
        int leftRegionX,
        int topRegionY,
        int rightRegionX,
        int bottomRegionY)
    {
        ArgumentNullException.ThrowIfNull(mapCatalog);

        int checkedRegions = 0;
        int mismatchedRegions = 0;
        int mismatchedCells = 0;

        for (int ry = topRegionY; ry <= bottomRegionY; ry++)
        {
            for (int rx = leftRegionX; rx <= rightRegionX; rx++)
            {
                if (!TryLoadRegionCells(mapId, rx, ry, out int[]? clientCells) || clientCells is null)
                {
                    continue;
                }

                checkedRegions++;
                int regionDiffs = 0;
                for (int lx = 0; lx < WorldCoordinateConsts.RegionWidthCells; lx++)
                {
                    int mpsX = (((rx * WorldCoordinateConsts.RegionWidthCells) + lx) * WorldCoordinateConsts.CellWidthMps) + 16;
                    for (int ly = 0; ly < WorldCoordinateConsts.RegionHeightCells; ly++)
                    {
                        int mpsY = (((ry * WorldCoordinateConsts.RegionHeightCells) + ly) * WorldCoordinateConsts.CellHeightMps) + 16;
                        bool serverPassable = (mapCatalog.GetCellObstacle(mapId, mpsX, mpsY) & 0x0F) == 0;
                        bool clientPassable = (clientCells[(lx * WorldCoordinateConsts.RegionHeightCells) + ly] & 0x0F) == 0;
                        if (serverPassable != clientPassable)
                        {
                            regionDiffs++;
                        }
                    }
                }

                if (regionDiffs > 0)
                {
                    mismatchedRegions++;
                    mismatchedCells += regionDiffs;
                }
            }
        }

        return (checkedRegions, mismatchedRegions, mismatchedCells);
    }

    private int[] GetOrLoadRegionCells(ushort mapId, int rx, int ry)
    {
        if (TryLoadRegionCells(mapId, rx, ry, out int[]? cells) && cells is not null)
        {
            return cells;
        }

        return [];
    }

    private bool TryLoadRegionCells(ushort mapId, int rx, int ry, out int[]? cells)
    {
        if (_regionCache.TryGetValue((mapId, rx, ry), out int[]? cached))
        {
            cells = cached.Length == 512 ? cached : null;
            return cells is not null;
        }

        if (!_mapSubPathBytesById.TryGetValue(mapId, out byte[]? mapSubPath))
        {
            _regionCache[(mapId, rx, ry)] = [];
            cells = null;
            return false;
        }

        byte[] suffixAscii = Encoding.ASCII.GetBytes(
            string.Create(CultureInfo.InvariantCulture, $"\\v_{ry:D3}\\{rx:D3}_region_c.dat"));
        byte[] prefixAscii = "\\maps\\"u8.ToArray();
        byte[] fullPathBytes = new byte[prefixAscii.Length + mapSubPath.Length + suffixAscii.Length];
        Buffer.BlockCopy(prefixAscii, 0, fullPathBytes, 0, prefixAscii.Length);
        Buffer.BlockCopy(mapSubPath, 0, fullPathBytes, prefixAscii.Length, mapSubPath.Length);
        Buffer.BlockCopy(suffixAscii, 0, fullPathBytes, prefixAscii.Length + mapSubPath.Length, suffixAscii.Length);

        byte[]? regionBytes = TryReadPakEntryBytes(_mountedPaks, fullPathBytes);
        if (regionBytes is null)
        {
            _regionCache[(mapId, rx, ry)] = [];
            cells = null;
            return false;
        }

        int[] parsed = new int[512];
        if (regionBytes.Length >= 12)
        {
            uint numSections = BinaryPrimitives.ReadUInt32LittleEndian(regionBytes.AsSpan(0, 4));
            int headerBytes = 4 + ((int)numSections * 8);
            if (numSections >= 1 && regionBytes.Length >= headerBytes)
            {
                // In Region_C.dat (KScenePlaceRegionC::Load @ 0x005F916F -> LoadObstacle @ 0x005F88E0),
                // Section 0 (byte offset 4) holds the 2048-byte (16x32 int32) m_ObstacleInfo matrix.
                uint obsRelOffset = BinaryPrimitives.ReadUInt32LittleEndian(regionBytes.AsSpan(4, 4));
                uint obsLength = BinaryPrimitives.ReadUInt32LittleEndian(regionBytes.AsSpan(8, 4));
                int obsAbsOffset = headerBytes + (int)obsRelOffset;
                if (obsLength >= 2048u && obsAbsOffset + 2048 <= regionBytes.Length)
                {
                    for (int i = 0; i < 512; i++)
                    {
                        parsed[i] = BinaryPrimitives.ReadInt32LittleEndian(regionBytes.AsSpan(obsAbsOffset + (i * 4), 4));
                    }
                }
            }
        }

        _regionCache[(mapId, rx, ry)] = parsed;
        cells = parsed;
        return true;
    }

    private static List<(string PakPath, Dictionary<uint, (uint Offset, uint UncompressedLength, uint CompressedLength, byte Method)> Index)> LoadMountedPaks(
        string templateRoot,
        string packageIniPath)
    {
        var result = new List<(string, Dictionary<uint, (uint, uint, uint, byte)>)>();
        string pakSubDir = "data";
        var orderedPakNames = new List<(int Order, string FileName)>();

        if (File.Exists(packageIniPath))
        {
            foreach (string rawLine in File.ReadLines(packageIniPath))
            {
                string line = rawLine.Trim();
                if (line.Length == 0 || line.StartsWith(';') || line.StartsWith('['))
                {
                    continue;
                }

                int eq = line.IndexOf('=');
                if (eq <= 0)
                {
                    continue;
                }

                string key = line[..eq].Trim();
                string val = line[(eq + 1)..].Trim();
                if (key.Equals("Path", StringComparison.OrdinalIgnoreCase))
                {
                    pakSubDir = val.TrimStart('\\', '/');
                }
                else if (int.TryParse(key, NumberStyles.Integer, CultureInfo.InvariantCulture, out int order) && val.Length > 0)
                {
                    orderedPakNames.Add((order, val));
                }
            }
        }

        string pakFolder = Path.Combine(templateRoot, pakSubDir);
        if (!Directory.Exists(pakFolder))
        {
            string altFolder = Path.Combine(templateRoot, pakSubDir.Equals("data", StringComparison.OrdinalIgnoreCase) ? "data1" : "data");
            if (Directory.Exists(altFolder))
            {
                pakFolder = altFolder;
            }
        }

        foreach ((_, string pakName) in orderedPakNames.OrderBy(x => x.Order))
        {
            string fullPakPath = Path.Combine(pakFolder, pakName);
            if (File.Exists(fullPakPath))
            {
                result.Add((fullPakPath, ReadPakIndex(fullPakPath)));
            }
        }

        if (result.Count == 0)
        {
            string fallbackMapsPak = Path.Combine(pakFolder, "maps.pak");
            if (File.Exists(fallbackMapsPak))
            {
                result.Add((fallbackMapsPak, ReadPakIndex(fallbackMapsPak)));
            }
        }

        return result;
    }

    private static byte[]? TryReadPakEntryBytes(
        IReadOnlyList<(string PakPath, Dictionary<uint, (uint Offset, uint UncompressedLength, uint CompressedLength, byte Method)> Index)> mountedPaks,
        ReadOnlySpan<byte> pakInternalPath)
    {
        uint hash = HashPakPath(pakInternalPath);
        for (int i = 0; i < mountedPaks.Count; i++)
        {
            (string pakPath, var index) = mountedPaks[i];
            if (!index.TryGetValue(hash, out var entry))
            {
                continue;
            }

            byte[] rawPayload;
            using (var fs = new FileStream(pakPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                fs.Seek(entry.Offset, SeekOrigin.Begin);
                int readLen = entry.Method == 0 ? (int)entry.UncompressedLength : (int)entry.CompressedLength;
                rawPayload = new byte[readLen];
                fs.ReadExactly(rawPayload);
            }

            return entry.Method == 0
                ? rawPayload
                : DecompressUclNrv2b8(rawPayload, (int)entry.UncompressedLength);
        }

        return null;
    }

    private static Dictionary<ushort, byte[]> ParseMapListSubPaths(byte[] raw)
    {
        var result = new Dictionary<ushort, byte[]>();
        int lineStart = 0;
        for (int i = 0; i <= raw.Length; i++)
        {
            if (i < raw.Length && raw[i] != (byte)'\n' && raw[i] != (byte)'\r')
            {
                continue;
            }

            if (i > lineStart)
            {
                ReadOnlySpan<byte> line = raw.AsSpan(lineStart, i - lineStart);
                int eq = line.IndexOf((byte)'=');
                if (eq > 0)
                {
                    ReadOnlySpan<byte> keySpan = line[..eq];
                    bool allDigits = true;
                    for (int k = 0; k < keySpan.Length; k++)
                    {
                        if (keySpan[k] < (byte)'0' || keySpan[k] > (byte)'9')
                        {
                            allDigits = false;
                            break;
                        }
                    }

                    if (allDigits && ushort.TryParse(Encoding.ASCII.GetString(keySpan), NumberStyles.None, CultureInfo.InvariantCulture, out ushort mapId))
                    {
                        result[mapId] = line[(eq + 1)..].ToArray();
                    }
                }
            }

            lineStart = i + 1;
        }

        return result;
    }

    private static Dictionary<uint, (uint Offset, uint UncompressedLength, uint CompressedLength, byte Method)> ReadPakIndex(string pakPath)
    {
        using var fs = new FileStream(pakPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        Span<byte> header = stackalloc byte[20];
        fs.ReadExactly(header);

        uint count = BinaryPrimitives.ReadUInt32LittleEndian(header.Slice(4, 4));
        uint indexOffset = BinaryPrimitives.ReadUInt32LittleEndian(header.Slice(8, 4));

        byte[] indexBytes = new byte[count * 16];
        fs.Seek(indexOffset, SeekOrigin.Begin);
        fs.ReadExactly(indexBytes);

        var dict = new Dictionary<uint, (uint, uint, uint, byte)>((int)count);
        for (int i = 0; i < count; i++)
        {
            ReadOnlySpan<byte> item = indexBytes.AsSpan(i * 16, 16);
            uint id = BinaryPrimitives.ReadUInt32LittleEndian(item[..4]);
            uint offset = BinaryPrimitives.ReadUInt32LittleEndian(item.Slice(4, 4));
            uint ulen = BinaryPrimitives.ReadUInt32LittleEndian(item.Slice(8, 4));
            uint clenAndFlag = BinaryPrimitives.ReadUInt32LittleEndian(item.Slice(12, 4));
            dict[id] = (offset, ulen, clenAndFlag & 0x00FF_FFFFu, (byte)(clenAndFlag >> 24));
        }

        return dict;
    }

    private static uint HashPakPath(ReadOnlySpan<byte> rawBytes)
    {
        uint id = 0;
        for (int i = 0; i < rawBytes.Length; i++)
        {
            sbyte c = unchecked((sbyte)rawBytes[i]);
            if (rawBytes[i] >= (byte)'A' && rawBytes[i] <= (byte)'Z')
            {
                c = unchecked((sbyte)(c + 0x20));
            }

            uint term = unchecked(id + (uint)((i + 1) * c));
            id = unchecked((term % 0x8000000Bu) * 0xFFFFFFEFu);
        }

        return id ^ 0x12345678u;
    }

    private static byte[] DecompressUclNrv2b8(ReadOnlySpan<byte> src, int dstLen)
    {
        byte[] dst = new byte[dstLen];
        int si = 0;
        int di = 0;
        uint bb = 0;
        int bc = 0;
        uint lastMOff = 1;

        int GetBit(ReadOnlySpan<byte> input)
        {
            if (bc == 0)
            {
                bb = input[si++];
                bc = 8;
            }

            bc--;
            return (int)((bb >> bc) & 1u);
        }

        while (true)
        {
            while (GetBit(src) == 1)
            {
                dst[di++] = src[si++];
            }

            uint mOff = 1;
            do
            {
                mOff = (mOff << 1) + (uint)GetBit(src);
            }
            while (GetBit(src) == 0);

            if (mOff == 2u)
            {
                mOff = lastMOff;
            }
            else
            {
                mOff = ((mOff - 3u) << 8) + src[si++];
                if (mOff == 0xFFFF_FFFFu)
                {
                    break;
                }

                mOff++;
                lastMOff = mOff;
            }

            uint mLen = (uint)((GetBit(src) << 1) | GetBit(src));
            if (mLen == 0u)
            {
                mLen = 1u;
                do
                {
                    mLen = (mLen << 1) + (uint)GetBit(src);
                }
                while (GetBit(src) == 0);
                mLen += 2u;
            }

            if (mOff > 0xD00u)
            {
                mLen++;
            }

            mLen++;
            int copySrc = di - (int)mOff;
            for (uint k = 0; k < mLen; k++)
            {
                dst[di++] = dst[copySrc++];
            }
        }

        return dst;
    }
}
