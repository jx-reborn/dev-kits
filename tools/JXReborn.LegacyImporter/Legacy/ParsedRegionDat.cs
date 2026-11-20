using System.Collections.Generic;

namespace JXReborn.LegacyImporter.Legacy;

/// <summary>
/// Full parsed contents of a single <c>XXX_Region_S.dat</c> file across all 4 server sections.
/// </summary>
public sealed record ParsedRegionDat(
    int RegionX,
    int RegionY,
    int[] ObstacleCells,
    IReadOnlyList<ParsedTrapCellSpan> Traps,
    IReadOnlyList<ParsedRegionNpc> Npcs,
    IReadOnlyList<ParsedRegionObj> Objects);
