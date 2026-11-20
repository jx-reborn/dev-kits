using System;
using System.Collections.Generic;
using JXReborn.GameAssets.Skills;

namespace JXReborn.ZoneEngine.Tests.Combat;

internal sealed class OverlaySkillTemplateCatalog : ISkillTemplateCatalog
{
    private readonly ISkillTemplateCatalog _inner;
    private readonly Dictionary<int, SkillTemplate> _overrides = [];

    public OverlaySkillTemplateCatalog(ISkillTemplateCatalog inner, params SkillTemplate[] overrides)
    {
        _inner = inner;
        foreach (SkillTemplate skill in overrides)
        {
            _overrides[skill.Id] = skill;
        }
    }

    public int Count => _inner.Count + _overrides.Count;

    public IReadOnlyCollection<SkillTemplate> AllSkills => _inner.AllSkills;

    public IReadOnlyList<WeaponSkillMappingTemplate> WeaponSkillMappings => _inner.WeaponSkillMappings;

    public AttributeConstRulesTemplate? AttributeConstRules => _inner.AttributeConstRules;

    public bool TryGetSkill(int skillId, out SkillTemplate? skill)
    {
        if (_overrides.TryGetValue(skillId, out skill))
        {
            return true;
        }

        return _inner.TryGetSkill(skillId, out skill);
    }

    public bool TryGetSkillByCode(string code, out SkillTemplate? skill)
    {
        foreach (SkillTemplate s in _overrides.Values)
        {
            if (string.Equals(s.Code, code, StringComparison.OrdinalIgnoreCase))
            {
                skill = s;
                return true;
            }
        }

        return _inner.TryGetSkillByCode(code, out skill);
    }

    public int GetBasicWeaponSkillId(int weaponDetailOrParticular) =>
        _inner.GetBasicWeaponSkillId(weaponDetailOrParticular);
}
