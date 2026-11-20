using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using JXReborn.LegacyImporter.Legacy;
using JXReborn.LegacyImporter.Sources;
using JXReborn.Primitives.Quests;

namespace JXReborn.LegacyImporter.Exporters;

/// <summary>
/// Exports the 7 Dã Tẩu task settings tables (<c>settings/task/*.txt</c>) into
/// <c>data/quests/da-tau.quests.json</c> per <c>JX Reborn Data Standard v1</c>.
/// </summary>
public sealed class QuestDataExporter
{
    public (int TreasurePositions, int MilestoneAwards, int BasicAwards, int BuyGoods, int FindGoods, int UpGround, int KillMonsters) ExportQuests(
        ILegacyServerSource source,
        string outputDataDirectory,
        ExportCatalogRegistry? registry = null)
    {
        ArgumentNullException.ThrowIfNull(source);
        registry ??= new ExportCatalogRegistry();

        string questsOutDir = Path.Combine(outputDataDirectory, "quests");
        Directory.CreateDirectory(questsOutDir);

        List<Dictionary<string, object?>> treasurePositions = ExportTreasurePositions(source);
        List<Dictionary<string, object?>> milestoneAwards = ExportMilestoneAwards(source);
        List<Dictionary<string, object?>> basicAwards = ExportBasicAwards(source);
        List<Dictionary<string, object?>> buyGoodsTasks = ExportBuyGoods(source);
        List<Dictionary<string, object?>> findGoodsTasks = ExportFindGoods(source);
        List<Dictionary<string, object?>> upGroundTasks = ExportUpGround(source);
        List<Dictionary<string, object?>> killMonsterTasks = ExportKillMonsters(source);

        registry.RecordCount("quests.treasurePositions", treasurePositions.Count, treasurePositions.Count);
        registry.RecordCount("quests.milestoneAwards", milestoneAwards.Count, milestoneAwards.Count);
        registry.RecordCount("quests.basicAwards", basicAwards.Count, basicAwards.Count);
        registry.RecordCount("quests.buyGoodsTasks", buyGoodsTasks.Count, buyGoodsTasks.Count);
        registry.RecordCount("quests.findGoodsTasks", findGoodsTasks.Count, findGoodsTasks.Count);
        registry.RecordCount("quests.upGroundTasks", upGroundTasks.Count, upGroundTasks.Count);
        registry.RecordCount("quests.killMonsterTasks", killMonsterTasks.Count, killMonsterTasks.Count);

        var rootDoc = new Dictionary<string, object?>
        {
            ["$schema"] = "../../schemas/quest-catalog.schema.json",
            ["schemaVersion"] = "1.0.0",
            ["treasurePositions"] = treasurePositions,
            ["milestoneAwards"] = milestoneAwards,
            ["basicAwards"] = basicAwards,
            ["buyGoodsTasks"] = buyGoodsTasks,
            ["findGoodsTasks"] = findGoodsTasks,
            ["upGroundTasks"] = upGroundTasks,
            ["killMonsterTasks"] = killMonsterTasks,
        };

        LegacyPathResolver.WriteCanonicalJson(Path.Combine(questsOutDir, "da-tau.quests.json"), rootDoc);

        return (
            treasurePositions.Count,
            milestoneAwards.Count,
            basicAwards.Count,
            buyGoodsTasks.Count,
            findGoodsTasks.Count,
            upGroundTasks.Count,
            killMonsterTasks.Count);
    }

    private static List<Dictionary<string, object?>> ExportTreasurePositions(ILegacyServerSource source)
    {
        var list = new List<Dictionary<string, object?>>();
        KTabFileTable? table = KTabFileTable.TryLoad(source, @"settings\task\random\trea_pos.txt");
        if (table is null)
        {
            return list;
        }

        int rowId = 1;
        for (int r = 2; r < table.LineCount; r++)
        {
            if (table.IsRowCompletelyBlankOrTabsOnly(r) || table.GetColumnCount(r) < 6)
            {
                continue;
            }

            if (TryReadInt(table, r, 1, out int mapId) &&
                TryReadInt(table, r, 2, out int worldX) &&
                TryReadInt(table, r, 3, out int worldY) &&
                TryReadInt(table, r, 4, out int mapX) &&
                TryReadInt(table, r, 5, out int mapY))
            {
                list.Add(new Dictionary<string, object?>
                {
                    ["rowId"] = rowId++,
                    ["mapName"] = table.GetString(r, 0),
                    ["mapId"] = mapId,
                    ["worldX"] = worldX,
                    ["worldY"] = worldY,
                    ["mapX"] = mapX,
                    ["mapY"] = mapY,
                    ["description"] = table.GetColumnCount(r) > 6 ? table.GetString(r, 6) : string.Empty,
                    ["sprPath"] = table.GetColumnCount(r) > 7 ? ReadRawAsciiOrText(table, r, 7) : string.Empty,
                });
            }
        }

        return list;
    }

    private static List<Dictionary<string, object?>> ExportMilestoneAwards(ILegacyServerSource source)
    {
        var list = new List<Dictionary<string, object?>>();
        KTabFileTable? table = KTabFileTable.TryLoad(source, @"settings\task\award_link.txt");
        if (table is null)
        {
            return list;
        }

        for (int r = 1; r < table.LineCount; r++)
        {
            if (table.IsRowCompletelyBlankOrTabsOnly(r) || table.GetColumnCount(r) < 9)
            {
                continue;
            }

            if (TryReadInt(table, r, 0, out int step) &&
                TryReadInt(table, r, 2, out int quality) &&
                TryReadInt(table, r, 3, out int genre) &&
                TryReadInt(table, r, 4, out int detail) &&
                TryReadInt(table, r, 5, out int particular) &&
                TryReadInt(table, r, 6, out int level) &&
                TryReadInt(table, r, 7, out int goodsFive) &&
                TryReadInt(table, r, 8, out int magicLevel))
            {
                list.Add(new Dictionary<string, object?>
                {
                    ["milestoneStep"] = step,
                    ["name"] = table.GetString(r, 1),
                    ["quality"] = quality,
                    ["genre"] = genre,
                    ["detail"] = detail,
                    ["particular"] = particular,
                    ["level"] = level,
                    ["goodsFive"] = goodsFive,
                    ["magicLevel"] = magicLevel,
                });
            }
        }

        return list;
    }

    private static List<Dictionary<string, object?>> ExportBasicAwards(ILegacyServerSource source)
    {
        var list = new List<Dictionary<string, object?>>();
        KTabFileTable? table = KTabFileTable.TryLoad(source, @"settings\task\award_basic.txt");
        if (table is null)
        {
            return list;
        }

        int id = 1;
        for (int r = 1; r < table.LineCount; r++)
        {
            if (table.IsRowCompletelyBlankOrTabsOnly(r) || table.GetColumnCount(r) < 8)
            {
                continue;
            }

            if (TryReadInt(table, r, 1, out int quality) &&
                TryReadInt(table, r, 2, out int genre) &&
                TryReadInt(table, r, 3, out int detail) &&
                TryReadInt(table, r, 4, out int particular) &&
                TryReadInt(table, r, 5, out int goodsFive) &&
                TryReadInt(table, r, 6, out int level))
            {
                long taskValue = table.GetColumnCount(r) > 8 && TryReadLong(table, r, 8, out long tv) ? tv : 0L;
                list.Add(new Dictionary<string, object?>
                {
                    ["awardId"] = id++,
                    ["name"] = table.GetString(r, 0),
                    ["awardKind"] = (int)DaTauAwardKind.Item,
                    ["quality"] = quality,
                    ["genre"] = genre,
                    ["detail"] = detail,
                    ["particular"] = particular,
                    ["goodsFive"] = goodsFive,
                    ["level"] = level,
                    ["taskValue"] = taskValue,
                });
            }
        }

        return list;
    }

    private static List<Dictionary<string, object?>> ExportBuyGoods(ILegacyServerSource source)
    {
        var list = new List<Dictionary<string, object?>>();
        KTabFileTable? table = KTabFileTable.TryLoad(source, @"settings\task\tasklink_buygoods.txt");
        if (table is null)
        {
            return list;
        }

        for (int r = 1; r < table.LineCount; r++)
        {
            if (table.IsRowCompletelyBlankOrTabsOnly(r) || table.GetColumnCount(r) < 6)
            {
                continue;
            }

            if (TryReadInt(table, r, 0, out int taskId) &&
                TryReadInt(table, r, 1, out int genre) &&
                TryReadInt(table, r, 2, out int detail) &&
                TryReadInt(table, r, 3, out int particular) &&
                TryReadByte(table, r, 4, out byte level) &&
                TryReadByte(table, r, 5, out byte goodsFive))
            {
                list.Add(new Dictionary<string, object?>
                {
                    ["taskId"] = taskId,
                    ["genre"] = genre,
                    ["detail"] = detail,
                    ["particular"] = particular,
                    ["level"] = (int)level,
                    ["goodsFive"] = (int)goodsFive,
                    ["shopName"] = table.GetColumnCount(r) > 28 ? table.GetString(r, 28) : string.Empty,
                    ["itemName"] = table.GetColumnCount(r) > 29 ? table.GetString(r, 29) : string.Empty,
                });
            }
        }

        return list;
    }

    private static List<Dictionary<string, object?>> ExportFindGoods(ILegacyServerSource source)
    {
        var list = new List<Dictionary<string, object?>>();
        KTabFileTable? table = KTabFileTable.TryLoad(source, @"settings\task\tasklink_findgoods.txt");
        if (table is null)
        {
            return list;
        }

        for (int r = 1; r < table.LineCount; r++)
        {
            if (table.IsRowCompletelyBlankOrTabsOnly(r) || table.GetColumnCount(r) < 10)
            {
                continue;
            }

            if (TryReadInt(table, r, 0, out int taskId) &&
                TryReadInt(table, r, 1, out int genre) &&
                TryReadInt(table, r, 2, out int detail) &&
                TryReadInt(table, r, 3, out int particular))
            {
                byte level = TryReadByte(table, r, 4, out byte lvl) ? lvl : (byte)1;
                byte goodsFive = TryReadByte(table, r, 5, out byte gf) ? gf : (byte)0;
                string magicEnName = ReadRawAsciiOrText(table, r, 6);
                int minVal = TryReadInt(table, r, 8, out int mn) ? mn : 0;
                int maxVal = TryReadInt(table, r, 9, out int mx) ? mx : 100;
                string itemName = table.GetColumnCount(r) > 32 ? table.GetString(r, 32) : string.Empty;

                list.Add(new Dictionary<string, object?>
                {
                    ["taskId"] = taskId,
                    ["genre"] = genre,
                    ["detail"] = detail,
                    ["particular"] = particular,
                    ["level"] = (int)level,
                    ["goodsFive"] = (int)goodsFive,
                    ["magicEnName"] = magicEnName,
                    ["minValue"] = minVal,
                    ["maxValue"] = maxVal,
                    ["itemName"] = itemName,
                });
            }
        }

        return list;
    }

    private static List<Dictionary<string, object?>> ExportUpGround(ILegacyServerSource source)
    {
        var list = new List<Dictionary<string, object?>>();
        KTabFileTable? table = KTabFileTable.TryLoad(source, @"settings\task\tasklink_upground.txt");
        if (table is null)
        {
            return list;
        }

        for (int r = 1; r < table.LineCount; r++)
        {
            if (table.IsRowCompletelyBlankOrTabsOnly(r) || table.GetColumnCount(r) < 3)
            {
                continue;
            }

            if (TryReadInt(table, r, 0, out int taskId) &&
                TryReadByte(table, r, 1, out byte numType) &&
                TryReadInt(table, r, 2, out int delta))
            {
                list.Add(new Dictionary<string, object?>
                {
                    ["taskId"] = taskId,
                    ["numericType"] = (int)numType,
                    ["targetDelta"] = delta,
                });
            }
        }

        return list;
    }

    private static List<Dictionary<string, object?>> ExportKillMonsters(ILegacyServerSource source)
    {
        var list = new List<Dictionary<string, object?>>();
        KTabFileTable? table = KTabFileTable.TryLoad(source, @"settings\task\killmostert.txt");
        if (table is null)
        {
            return list;
        }

        for (int r = 1; r < table.LineCount; r++)
        {
            if (table.IsRowCompletelyBlankOrTabsOnly(r) || table.GetColumnCount(r) < 6)
            {
                continue;
            }

            if (TryReadInt(table, r, 0, out int taskId) &&
                TryReadInt(table, r, 2, out int mapId) &&
                TryReadInt(table, r, 5, out int killCount))
            {
                list.Add(new Dictionary<string, object?>
                {
                    ["taskId"] = taskId,
                    ["mapId"] = mapId,
                    ["mapName"] = table.GetString(r, 3),
                    ["monsterName"] = table.GetString(r, 4),
                    ["killCount"] = killCount,
                });
            }
        }

        return list;
    }

    private static bool TryReadInt(KTabFileTable table, int row, int col, out int value)
    {
        string text = Encoding.Latin1.GetString(table.GetRawCell(row, col)).Trim();
        return int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
    }

    private static bool TryReadLong(KTabFileTable table, int row, int col, out long value)
    {
        string text = Encoding.Latin1.GetString(table.GetRawCell(row, col)).Trim();
        return long.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
    }

    private static bool TryReadByte(KTabFileTable table, int row, int col, out byte value)
    {
        string text = Encoding.Latin1.GetString(table.GetRawCell(row, col)).Trim();
        return byte.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
    }

    private static string ReadRawAsciiOrText(KTabFileTable table, int row, int col)
    {
        ReadOnlySpan<byte> raw = table.GetRawCell(row, col);
        return raw.IsEmpty ? string.Empty : HybridLegacyTextDecoder.DecodeSmart(raw, preferVietnamese: true).Trim();
    }
}
