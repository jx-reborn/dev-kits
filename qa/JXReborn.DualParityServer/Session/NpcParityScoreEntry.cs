namespace JXReborn.DualParityServer.Session;

/// <summary>
/// Per-NPC parity verification summary recorded during an NPC dialog crawl or scripted run.
/// </summary>
public sealed record NpcParityScoreEntry(
    int MapId,
    string NpcName,
    int MpsX,
    int MpsY,
    bool Matched,
    int BranchesTested,
    int CriticalFindings,
    int WarningFindings,
    string Summary);
