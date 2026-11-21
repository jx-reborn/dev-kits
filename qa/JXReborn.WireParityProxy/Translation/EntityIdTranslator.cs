using JXReborn.Primitives.Items;
using JXReborn.TestBase.WireCaptures;
using JXReborn.WireParityProxy.ShadowStates;

namespace JXReborn.WireParityProxy.Translation;

/// <summary>
/// Resolves dynamic entity IDs (<c>SelfNpcId</c>, <c>NpcId</c>, <c>ItemId</c>, and bag grid coordinates)
/// from Primary (<c>legacy-linux</c>) runtime space into Shadow (<c>reborn-server</c>) runtime space.
/// </summary>
public sealed class EntityIdTranslator
{
    private const double MaxNpcMatchDistanceMps = 256.0;

    private readonly ProxyShadowClientState _primary;
    private readonly ProxyShadowClientState _shadow;

    public EntityIdTranslator(ProxyShadowClientState primary, ProxyShadowClientState shadow)
    {
        ArgumentNullException.ThrowIfNull(primary);
        ArgumentNullException.ThrowIfNull(shadow);
        _primary = primary;
        _shadow = shadow;
    }

    public bool TryTranslateNpcId(uint primaryNpcId, out uint shadowNpcId)
    {
        if (primaryNpcId == 0)
        {
            shadowNpcId = 0;
            return false;
        }

        if (primaryNpcId == _primary.SelfNpcId && _shadow.SelfNpcId != 0)
        {
            shadowNpcId = _shadow.SelfNpcId;
            return true;
        }

        IReadOnlyDictionary<uint, VirtualJxNpcEntity> primaryNpcs = _primary.SnapshotNpcs();
        IReadOnlyDictionary<uint, VirtualJxNpcEntity> shadowNpcs = _shadow.SnapshotNpcs();

        if (!primaryNpcs.TryGetValue(primaryNpcId, out VirtualJxNpcEntity? primaryNpc))
        {
            shadowNpcId = 0;
            return false;
        }

        VirtualJxNpcEntity? best = null;
        double bestDist = double.MaxValue;

        foreach (VirtualJxNpcEntity candidate in shadowNpcs.Values)
        {
            bool templateMatch = primaryNpc.TemplateId > 0 && candidate.TemplateId == primaryNpc.TemplateId;
            bool nameMatch = !string.IsNullOrWhiteSpace(primaryNpc.NameUnicode) &&
                             string.Equals(candidate.NameUnicode, primaryNpc.NameUnicode, StringComparison.Ordinal);

            if (!templateMatch && !nameMatch)
            {
                continue;
            }

            long dx = (long)candidate.MpsX - primaryNpc.MpsX;
            long dy = (long)candidate.MpsY - primaryNpc.MpsY;
            double dist = Math.Sqrt((dx * dx) + (dy * dy));
            if (dist <= MaxNpcMatchDistanceMps && dist < bestDist)
            {
                bestDist = dist;
                best = candidate;
            }
        }

        if (best is not null)
        {
            shadowNpcId = best.NpcId;
            return true;
        }

        shadowNpcId = 0;
        return false;
    }

    public bool TryTranslateItemByIdOrCell(
        uint primaryItemId,
        ItemContainerPlace primaryPlace,
        byte primaryGridX,
        byte primaryGridY,
        out VirtualJxItemSlot? shadowItem)
    {
        IReadOnlyDictionary<uint, VirtualJxItemSlot> primaryItems = _primary.SnapshotItems();
        IReadOnlyDictionary<uint, VirtualJxItemSlot> shadowItems = _shadow.SnapshotItems();

        VirtualJxItemSlot? primarySlot = null;
        if (primaryItemId != 0 && primaryItems.TryGetValue(primaryItemId, out VirtualJxItemSlot? byId))
        {
            primarySlot = byId;
        }
        else
        {
            primarySlot = FindCoveringItem(primaryItems.Values, primaryPlace, primaryGridX, primaryGridY);
        }

        if (primarySlot is null)
        {
            shadowItem = null;
            return false;
        }

        // 1. Prefer exact (Genre, Detail, Particular, Level, Place, GridX, GridY)
        foreach (VirtualJxItemSlot candidate in shadowItems.Values)
        {
            if (SameTemplate(primarySlot, candidate) &&
                candidate.ContainerPlace == primarySlot.ContainerPlace &&
                candidate.GridX == primarySlot.GridX &&
                candidate.GridY == primarySlot.GridY)
            {
                shadowItem = candidate;
                return true;
            }
        }

        // 2. Fallback: same template in the same container place
        foreach (VirtualJxItemSlot candidate in shadowItems.Values)
        {
            if (SameTemplate(primarySlot, candidate) &&
                candidate.ContainerPlace == primarySlot.ContainerPlace)
            {
                shadowItem = candidate;
                return true;
            }
        }

        // 3. Fallback: same template anywhere
        foreach (VirtualJxItemSlot candidate in shadowItems.Values)
        {
            if (SameTemplate(primarySlot, candidate))
            {
                shadowItem = candidate;
                return true;
            }
        }

        shadowItem = null;
        return false;
    }

    private static bool SameTemplate(VirtualJxItemSlot a, VirtualJxItemSlot b) =>
        a.Genre == b.Genre &&
        a.DetailType == b.DetailType &&
        a.ParticularType == b.ParticularType &&
        a.Level == b.Level;

    private static VirtualJxItemSlot? FindCoveringItem(
        IEnumerable<VirtualJxItemSlot> items,
        ItemContainerPlace place,
        byte x,
        byte y)
    {
        foreach (VirtualJxItemSlot slot in items)
        {
            if (slot.ContainerPlace != place)
            {
                continue;
            }

            if (place == ItemContainerPlace.Equip)
            {
                if (slot.GridX == x)
                {
                    return slot;
                }

                continue;
            }

            if (x >= slot.GridX && x < slot.GridX + Math.Max(1, slot.WidthCells) &&
                y >= slot.GridY && y < slot.GridY + Math.Max(1, slot.HeightCells))
            {
                return slot;
            }
        }

        return null;
    }
}
