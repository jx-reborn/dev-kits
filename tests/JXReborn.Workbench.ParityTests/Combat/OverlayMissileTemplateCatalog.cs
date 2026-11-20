using System;
using System.Collections.Generic;
using JXReborn.GameAssets.Skills;

namespace JXReborn.ZoneEngine.Tests.Combat;

internal sealed class OverlayMissileTemplateCatalog : IMissileTemplateCatalog
{
    private readonly IMissileTemplateCatalog _inner;
    private readonly Dictionary<int, MissileTemplate> _overrides = [];

    public OverlayMissileTemplateCatalog(IMissileTemplateCatalog inner, params MissileTemplate[] overrides)
    {
        _inner = inner;
        foreach (MissileTemplate missile in overrides)
        {
            _overrides[missile.Id] = missile;
        }
    }

    public int Count => _inner.Count + _overrides.Count;

    public IReadOnlyCollection<MissileTemplate> AllMissiles => _inner.AllMissiles;

    public bool TryGetMissile(int missileId, out MissileTemplate? missile)
    {
        if (_overrides.TryGetValue(missileId, out missile))
        {
            return true;
        }

        return _inner.TryGetMissile(missileId, out missile);
    }

    public bool TryGetMissileByCode(string code, out MissileTemplate? missile)
    {
        foreach (MissileTemplate m in _overrides.Values)
        {
            if (string.Equals(m.Code, code, StringComparison.OrdinalIgnoreCase))
            {
                missile = m;
                return true;
            }
        }

        return _inner.TryGetMissileByCode(code, out missile);
    }
}
