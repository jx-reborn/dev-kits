using System.Collections.Generic;
using JXReborn.GameAssets.NpcTemplates;
using JXReborn.GameAssets.WorldMaps;
using JXReborn.Primitives.WorldZones;

namespace JXReborn.ZoneEngine.Tests.Combat;

internal sealed class OverlayMapObstacleCatalog : IMapCatalog
{
    private readonly IMapCatalog _inner;
    private readonly HashSet<(int CellX, int CellY)> _blockedCells;

    public OverlayMapObstacleCatalog(IMapCatalog inner, params (int CellX, int CellY)[] blockedCells)
    {
        _inner = inner;
        _blockedCells = new HashSet<(int CellX, int CellY)>(blockedCells);
    }

    public IReadOnlyCollection<MapDescriptor> ActiveMaps => _inner.ActiveMaps;

    public bool IsMapLoaded(ushort mapId) => _inner.IsMapLoaded(mapId);

    public bool TryGetMapDescriptor(ushort mapId, out MapDescriptor? descriptor) =>
        _inner.TryGetMapDescriptor(mapId, out descriptor);

    public bool TryGetRegionObstacleGrid(ushort mapId, int regionX, int regionY, out RegionObstacleGrid? grid) =>
        _inner.TryGetRegionObstacleGrid(mapId, regionX, regionY, out grid);

    public bool TryGetRegionTrapGrid(ushort mapId, int regionX, int regionY, out RegionTrapGrid? trapGrid) =>
        _inner.TryGetRegionTrapGrid(mapId, regionX, regionY, out trapGrid);

    public int GetCellObstacle(ushort mapId, int mpsX, int mpsY)
    {
        int cellX = mpsX / WorldCoordinateConsts.CellWidthMps;
        int cellY = mpsY / WorldCoordinateConsts.CellHeightMps;
        if (_blockedCells.Contains((cellX, cellY)))
        {
            return 1;
        }

        return _inner.GetCellObstacle(mapId, mpsX, mpsY);
    }

    public uint GetCellTrapId(ushort mapId, int mpsX, int mpsY)
    {
        int cellX = mpsX / WorldCoordinateConsts.CellWidthMps;
        int cellY = mpsY / WorldCoordinateConsts.CellHeightMps;
        if (_blockedCells.Contains((cellX, cellY)))
        {
            return 0u;
        }

        return _inner.GetCellTrapId(mapId, mpsX, mpsY);
    }

    public IReadOnlyList<StaticNpcSpawnTemplate> GetStaticNpcSpawnsForMap(ushort mapId) =>
        _inner.GetStaticNpcSpawnsForMap(mapId);

    public bool TryResolveActiveOrFallbackMapId(ushort requestedMapId, out ushort resolvedMapId, out bool wasRedirected) =>
        _inner.TryResolveActiveOrFallbackMapId(requestedMapId, out resolvedMapId, out wasRedirected);
}
