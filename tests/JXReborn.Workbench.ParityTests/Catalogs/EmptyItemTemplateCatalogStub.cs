using System.Collections.Generic;
using JXReborn.GameAssets.Items;
using JXReborn.Primitives.Characters;
using JXReborn.Primitives.Items;

namespace JXReborn.GameAssets.Tests.Catalogs;

internal sealed class EmptyItemTemplateCatalogStub : IItemTemplateCatalog
{
    public int ItemCount => 0;

    public int MagicAffixCount => 0;

    public IReadOnlyCollection<ItemTemplate> AllItems => [];

    public IReadOnlyCollection<MagicAffixTemplate> AllMagicAffixes => [];

    public IReadOnlyCollection<GroundObjectTemplate> AllGroundObjects => [];

    public IReadOnlyCollection<EquipmentSetTemplate> AllEquipmentSets => [];

    public bool TryGetItemByCode(string code, out ItemTemplate? item)
    {
        item = null;
        return false;
    }

    public bool TryGetItemByRowId(ItemQualityKind quality, int rowId, out ItemTemplate? item)
    {
        item = null;
        return false;
    }

    public bool TryGetItem(
        ItemGenreKind genre,
        int detailType,
        int particularType,
        byte level,
        out ItemTemplate? item,
        ItemQualityKind quality = ItemQualityKind.Normal)
    {
        item = null;
        return false;
    }

    public IReadOnlyList<MagicAffixTemplate> GetCandidateMagicAffixes(
        bool isSuffix,
        int equipDetailType,
        int weaponParticularType,
        ElementSeries series,
        int maxLevel) => [];

    public bool TryGetMagicAffixById(int affixRowId, out MagicAffixTemplate? affix, string affixPool = "blueRandom")
    {
        affix = null;
        return false;
    }

    public bool TryGetGroundObject(int objDataId, out GroundObjectTemplate? objTemplate)
    {
        objTemplate = null;
        return false;
    }

    public bool TryGetEquipmentSet(bool isExtendedSuite, int setId, out EquipmentSetTemplate? equipmentSet)
    {
        equipmentSet = null;
        return false;
    }

    public int GetClassicSuiteRequiredPieces(int suiteId) => 0;
}
