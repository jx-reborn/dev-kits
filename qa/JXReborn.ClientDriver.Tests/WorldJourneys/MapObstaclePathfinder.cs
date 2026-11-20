using JXReborn.GameAssets.WorldMaps;
using JXReborn.Primitives.WorldZones;

namespace JXReborn.ClientDriver.Tests.WorldJourneys;

/// <summary>
/// Clearance-weighted cell-grid pathfinder and line-of-sight waypoint simplifier over <see cref="IMapCatalog"/>
/// and optional client <c>maps.pak</c> <see cref="ClientPakObstacleReader"/> terrain.
/// </summary>
public static class MapObstaclePathfinder
{
    private static readonly (int Dx, int Dy, int BaseCost)[] Neighbors =
    [
        (-1, 0, 10),
        (1, 0, 10),
        (0, -1, 10),
        (0, 1, 10),
        (-1, -1, 14),
        (-1, 1, 14),
        (1, -1, 14),
        (1, 1, 14)
    ];

    public static IReadOnlyList<(int MpsX, int MpsY)> FindWaypointsToTrap(
        IMapCatalog mapCatalog,
        ushort mapId,
        int startMpsX,
        int startMpsY,
        uint targetTrapId,
        ClientPakObstacleReader? clientPakObstacles = null,
        uint allowedPassThroughTrapId = 0,
        int maxSegmentLengthMps = 112)
    {
        ArgumentNullException.ThrowIfNull(mapCatalog);

        int startCellX = startMpsX / WorldCoordinateConsts.CellWidthMps;
        int startCellY = startMpsY / WorldCoordinateConsts.CellHeightMps;
        uint startTrapId = mapCatalog.GetCellTrapId(mapId, startMpsX, startMpsY);

        var occupiedSpawnCells = new HashSet<(int X, int Y)>();
        foreach (var spawn in mapCatalog.GetStaticNpcSpawnsForMap(mapId))
        {
            int scx = spawn.SpawnMpsX / WorldCoordinateConsts.CellWidthMps;
            int scy = spawn.SpawnMpsY / WorldCoordinateConsts.CellHeightMps;
            occupiedSpawnCells.Add((scx, scy));
        }

        List<(int CellX, int CellY)> cellPath = RunDijkstraToTrap(
            mapCatalog,
            clientPakObstacles,
            mapId,
            startCellX,
            startCellY,
            startTrapId,
            targetTrapId,
            allowedPassThroughTrapId,
            occupiedSpawnCells);

        if (cellPath.Count < 2)
        {
            throw new InvalidOperationException(
                $"No walkable path found on map {mapId} from ({startMpsX},{startMpsY}) to trap {targetTrapId}.");
        }

        // Keep the penultimate non-trap cell right before the trap cell so the caller approaches cleanly before stepping on the trap.
        (int trapCellX, int trapCellY) = cellPath[^1];
        List<(int CellX, int CellY)> approachCells = cellPath[..^1];

        List<(int MpsX, int MpsY)> waypoints = SimplifyApproachCells(
            mapCatalog,
            clientPakObstacles,
            mapId,
            startTrapId,
            allowedPassThroughTrapId,
            approachCells,
            occupiedSpawnCells,
            maxSegmentLengthMps);

        waypoints.Add(CellCenterToMps(trapCellX, trapCellY));
        return waypoints;
    }

    public static IReadOnlyList<(int MpsX, int MpsY)> FindWaypointsNearPosition(
        IMapCatalog mapCatalog,
        ushort mapId,
        int startMpsX,
        int startMpsY,
        int targetMpsX,
        int targetMpsY,
        int stopRadiusCells = 2,
        int minDistanceCells = 0,
        ClientPakObstacleReader? clientPakObstacles = null,
        int maxSegmentLengthMps = 192)
    {
        ArgumentNullException.ThrowIfNull(mapCatalog);

        int startCellX = startMpsX / WorldCoordinateConsts.CellWidthMps;
        int startCellY = startMpsY / WorldCoordinateConsts.CellHeightMps;
        int targetCellX = targetMpsX / WorldCoordinateConsts.CellWidthMps;
        int targetCellY = targetMpsY / WorldCoordinateConsts.CellHeightMps;
        uint startTrapId = mapCatalog.GetCellTrapId(mapId, startMpsX, startMpsY);

        var occupiedSpawnCells = new HashSet<(int X, int Y)>();
        foreach (var spawn in mapCatalog.GetStaticNpcSpawnsForMap(mapId))
        {
            int scx = spawn.SpawnMpsX / WorldCoordinateConsts.CellWidthMps;
            int scy = spawn.SpawnMpsY / WorldCoordinateConsts.CellHeightMps;
            occupiedSpawnCells.Add((scx, scy));
        }

        var dist = new Dictionary<(int X, int Y), int>
        {
            [(startCellX, startCellY)] = 0
        };
        var prev = new Dictionary<(int X, int Y), (int X, int Y)>();
        var pq = new PriorityQueue<(int X, int Y), int>();
        pq.Enqueue((startCellX, startCellY), 0);

        (int X, int Y)? reachedCell = null;
        while (pq.TryDequeue(out (int X, int Y) cur, out int curCost))
        {
            if (curCost > dist.GetValueOrDefault(cur, int.MaxValue))
            {
                continue;
            }

            int chebDist = Math.Max(Math.Abs(cur.X - targetCellX), Math.Abs(cur.Y - targetCellY));
            if (chebDist <= stopRadiusCells &&
                chebDist >= minDistanceCells &&
                !occupiedSpawnCells.Contains(cur))
            {
                reachedCell = cur;
                break;
            }

            foreach ((int dx, int dy, int moveCost) in Neighbors)
            {
                int nx = cur.X + dx;
                int ny = cur.Y + dy;
                (int nMpsX, int nMpsY) = CellCenterToMps(nx, ny);

                uint trapId = mapCatalog.GetCellTrapId(mapId, nMpsX, nMpsY);
                bool isStartGateExit = startTrapId != 0
                    && trapId == startTrapId
                    && Math.Abs(nx - startCellX) <= 4
                    && Math.Abs(ny - startCellY) <= 4;
                if (trapId != 0 && !isStartGateExit)
                {
                    continue;
                }

                if (!IsJointlyPassable(mapCatalog, clientPakObstacles, mapId, nMpsX, nMpsY))
                {
                    continue;
                }

                if (dx != 0 && dy != 0)
                {
                    (int adj1X, int adj1Y) = CellCenterToMps(cur.X + dx, cur.Y);
                    (int adj2X, int adj2Y) = CellCenterToMps(cur.X, cur.Y + dy);
                    if (!IsJointlyPassable(mapCatalog, clientPakObstacles, mapId, adj1X, adj1Y) ||
                        !IsJointlyPassable(mapCatalog, clientPakObstacles, mapId, adj2X, adj2Y))
                    {
                        continue;
                    }
                }

                int clearancePenalty = ComputeClearancePenalty(mapCatalog, clientPakObstacles, mapId, nx, ny);
                int spawnPenalty = ComputeNpcSpawnPenalty(occupiedSpawnCells, nx, ny);
                int nextCost = curCost + moveCost + clearancePenalty + spawnPenalty;
                if (nextCost < dist.GetValueOrDefault((nx, ny), int.MaxValue))
                {
                    dist[(nx, ny)] = nextCost;
                    prev[(nx, ny)] = cur;
                    pq.Enqueue((nx, ny), nextCost);
                }
            }
        }

        if (reachedCell is null)
        {
            throw new InvalidOperationException(
                $"No walkable path found on map {mapId} from ({startMpsX},{startMpsY}) near ({targetMpsX},{targetMpsY}).");
        }

        var cellPath = new List<(int CellX, int CellY)>();
        (int X, int Y) step = reachedCell.Value;
        cellPath.Add(step);
        while (prev.TryGetValue(step, out (int X, int Y) parent))
        {
            step = parent;
            cellPath.Add(step);
        }

        cellPath.Reverse();
        return SimplifyApproachCells(
            mapCatalog,
            clientPakObstacles,
            mapId,
            startTrapId,
            allowedPassThroughTrapId: 0,
            cellPath,
            occupiedSpawnCells,
            maxSegmentLengthMps);
    }

    private static List<(int CellX, int CellY)> RunDijkstraToTrap(
        IMapCatalog mapCatalog,
        ClientPakObstacleReader? clientPakObstacles,
        ushort mapId,
        int startCellX,
        int startCellY,
        uint startTrapId,
        uint targetTrapId,
        uint allowedPassThroughTrapId,
        HashSet<(int X, int Y)> occupiedSpawnCells)
    {
        var dist = new Dictionary<(int X, int Y), int>
        {
            [(startCellX, startCellY)] = 0
        };
        var prev = new Dictionary<(int X, int Y), (int X, int Y)>();
        var pq = new PriorityQueue<(int X, int Y), int>();
        pq.Enqueue((startCellX, startCellY), 0);

        (int X, int Y)? reachedTrapCell = null;

        while (pq.TryDequeue(out (int X, int Y) cur, out int curCost))
        {
            if (curCost > dist.GetValueOrDefault(cur, int.MaxValue))
            {
                continue;
            }

            (int curMpsX, int curMpsY) = CellCenterToMps(cur.X, cur.Y);
            uint curTrap = mapCatalog.GetCellTrapId(mapId, curMpsX, curMpsY);
            if (cur != (startCellX, startCellY) && curTrap == targetTrapId)
            {
                reachedTrapCell = cur;
                break;
            }

            foreach ((int dx, int dy, int moveCost) in Neighbors)
            {
                int nx = cur.X + dx;
                int ny = cur.Y + dy;
                (int nMpsX, int nMpsY) = CellCenterToMps(nx, ny);

                uint trapId = mapCatalog.GetCellTrapId(mapId, nMpsX, nMpsY);
                bool isStartGateExit = startTrapId != 0
                    && trapId == startTrapId
                    && Math.Abs(nx - startCellX) <= 4
                    && Math.Abs(ny - startCellY) <= 4;
                bool isAllowedPassThrough = allowedPassThroughTrapId != 0 && trapId == allowedPassThroughTrapId;

                if (trapId != 0 && trapId != targetTrapId && !isStartGateExit && !isAllowedPassThrough)
                {
                    continue;
                }

                if (trapId != targetTrapId)
                {
                    if (!IsJointlyPassable(mapCatalog, clientPakObstacles, mapId, nMpsX, nMpsY))
                    {
                        continue;
                    }

                    if (dx != 0 && dy != 0)
                    {
                        (int adj1X, int adj1Y) = CellCenterToMps(cur.X + dx, cur.Y);
                        (int adj2X, int adj2Y) = CellCenterToMps(cur.X, cur.Y + dy);
                        if (!IsJointlyPassable(mapCatalog, clientPakObstacles, mapId, adj1X, adj1Y) ||
                            !IsJointlyPassable(mapCatalog, clientPakObstacles, mapId, adj2X, adj2Y))
                        {
                            continue;
                        }
                    }
                }
                else if (clientPakObstacles is not null && clientPakObstacles.GetCellObstacle(mapId, nMpsX, nMpsY) != 0)
                {
                    // Ensure chosen target trap cell is also reachable in client terrain.
                    continue;
                }

                int clearancePenalty = ComputeClearancePenalty(mapCatalog, clientPakObstacles, mapId, nx, ny);
                int spawnPenalty = ComputeNpcSpawnPenalty(occupiedSpawnCells, nx, ny);
                int nextCost = curCost + moveCost + clearancePenalty + spawnPenalty;
                if (nextCost < dist.GetValueOrDefault((nx, ny), int.MaxValue))
                {
                    dist[(nx, ny)] = nextCost;
                    prev[(nx, ny)] = cur;
                    pq.Enqueue((nx, ny), nextCost);
                }
            }
        }

        if (reachedTrapCell is null)
        {
            return [];
        }

        var path = new List<(int CellX, int CellY)>();
        (int X, int Y) step = reachedTrapCell.Value;
        path.Add(step);
        while (prev.TryGetValue(step, out (int X, int Y) parent))
        {
            step = parent;
            path.Add(step);
        }

        path.Reverse();
        return path;
    }

    private static List<(int MpsX, int MpsY)> SimplifyApproachCells(
        IMapCatalog mapCatalog,
        ClientPakObstacleReader? clientPakObstacles,
        ushort mapId,
        uint startTrapId,
        uint allowedPassThroughTrapId,
        List<(int CellX, int CellY)> cells,
        HashSet<(int X, int Y)> occupiedSpawnCells,
        int maxSegmentLengthMps)
    {
        var result = new List<(int MpsX, int MpsY)>();
        if (cells.Count <= 1)
        {
            return result;
        }

        int anchorIdx = 0;
        while (anchorIdx < cells.Count - 1)
        {
            int bestNext = anchorIdx + 1;
            (int ax, int ay) = CellCenterToMps(cells[anchorIdx].CellX, cells[anchorIdx].CellY);

            for (int candidate = anchorIdx + 2; candidate < cells.Count; candidate++)
            {
                if (occupiedSpawnCells.Contains(cells[candidate]))
                {
                    continue;
                }

                (int cx, int cy) = CellCenterToMps(cells[candidate].CellX, cells[candidate].CellY);
                double dist = Math.Sqrt(((double)(cx - ax) * (cx - ax)) + ((double)(cy - ay) * (cy - ay)));
                if (dist > maxSegmentLengthMps)
                {
                    break;
                }

                if (HasClearStraightLine(mapCatalog, clientPakObstacles, mapId, startTrapId, allowedPassThroughTrapId, occupiedSpawnCells, ax, ay, cx, cy))
                {
                    bestNext = candidate;
                }
            }

            result.Add(CellCenterToMps(cells[bestNext].CellX, cells[bestNext].CellY));
            anchorIdx = bestNext;
        }

        return result;
    }

    private static bool HasClearStraightLine(
        IMapCatalog mapCatalog,
        ClientPakObstacleReader? clientPakObstacles,
        ushort mapId,
        uint allowedStartTrapId,
        uint allowedPassThroughTrapId,
        HashSet<(int X, int Y)> occupiedSpawnCells,
        int x0,
        int y0,
        int x1,
        int y1)
    {
        double dx = x1 - x0;
        double dy = y1 - y0;
        double dist = Math.Sqrt((dx * dx) + (dy * dy));
        int samples = Math.Max(2, (int)Math.Ceiling(dist / 8.0));
        int startCellX = x0 / WorldCoordinateConsts.CellWidthMps;
        int startCellY = y0 / WorldCoordinateConsts.CellHeightMps;

        for (int s = 1; s <= samples; s++)
        {
            double t = (double)s / samples;
            int sx = (int)Math.Round(x0 + (dx * t));
            int sy = (int)Math.Round(y0 + (dy * t));

            int scx = sx / WorldCoordinateConsts.CellWidthMps;
            int scy = sy / WorldCoordinateConsts.CellHeightMps;
            if ((scx != startCellX || scy != startCellY) && occupiedSpawnCells.Contains((scx, scy)))
            {
                return false;
            }

            uint trapId = mapCatalog.GetCellTrapId(mapId, sx, sy);
            bool allowedStart = allowedStartTrapId != 0 && trapId == allowedStartTrapId && s <= 6;
            bool allowedPass = allowedPassThroughTrapId != 0 && trapId == allowedPassThroughTrapId;
            if (trapId != 0 && !allowedStart && !allowedPass)
            {
                return false;
            }

            if (!IsJointlyPassable(mapCatalog, clientPakObstacles, mapId, sx, sy) ||
                !IsJointlyPassable(mapCatalog, clientPakObstacles, mapId, sx - 20, sy) ||
                !IsJointlyPassable(mapCatalog, clientPakObstacles, mapId, sx + 20, sy) ||
                !IsJointlyPassable(mapCatalog, clientPakObstacles, mapId, sx, sy - 20) ||
                !IsJointlyPassable(mapCatalog, clientPakObstacles, mapId, sx, sy + 20))
            {
                return false;
            }
        }

        return true;
    }

    private static int ComputeClearancePenalty(
        IMapCatalog mapCatalog,
        ClientPakObstacleReader? clientPakObstacles,
        ushort mapId,
        int cx,
        int cy)
    {
        for (int radius = 1; radius <= 4; radius++)
        {
            for (int dx = -radius; dx <= radius; dx++)
            {
                for (int dy = -radius; dy <= radius; dy++)
                {
                    if (Math.Abs(dx) != radius && Math.Abs(dy) != radius)
                    {
                        continue;
                    }

                    (int mx, int my) = CellCenterToMps(cx + dx, cy + dy);
                    if (!IsJointlyPassable(mapCatalog, clientPakObstacles, mapId, mx, my))
                    {
                        return radius switch
                        {
                            1 => 180,
                            2 => 90,
                            3 => 40,
                            _ => 15
                        };
                    }
                }
            }
        }

        return 0;
    }

    private static int ComputeNpcSpawnPenalty(HashSet<(int X, int Y)> occupiedSpawnCells, int cx, int cy)
    {
        if (occupiedSpawnCells.Contains((cx, cy)))
        {
            return 300;
        }

        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                if (occupiedSpawnCells.Contains((cx + dx, cy + dy)))
                {
                    return 60;
                }
            }
        }

        return 0;
    }

    private static bool IsJointlyPassable(
        IMapCatalog mapCatalog,
        ClientPakObstacleReader? clientPakObstacles,
        ushort mapId,
        int mpsX,
        int mpsY)
    {
        if (mapCatalog.GetCellObstacle(mapId, mpsX, mpsY) != 0)
        {
            return false;
        }

        if (clientPakObstacles is not null && clientPakObstacles.GetCellObstacle(mapId, mpsX, mpsY) != 0)
        {
            return false;
        }

        return true;
    }

    private static (int MpsX, int MpsY) CellCenterToMps(int cellX, int cellY) =>
        ((cellX * WorldCoordinateConsts.CellWidthMps) + (WorldCoordinateConsts.CellWidthMps / 2),
         (cellY * WorldCoordinateConsts.CellHeightMps) + (WorldCoordinateConsts.CellHeightMps / 2));
}
