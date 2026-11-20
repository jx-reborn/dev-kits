using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.IO;
using System.Text;
using JXReborn.LegacyImporter.Sources;

namespace JXReborn.LegacyImporter.Legacy;

/// <summary>
/// Authoritative <c>MAGIC_ATTRIB</c> registry seeded from <c>jx_linux_y</c> <c>KMagicDesc::KMagicDesc</c> (<c>0x08088900</c>)
/// <c>MAGIC_ATTRIB_STRING[0..0x128]</c> for exact C++/wire ID fidelity, supplemented by <c>settings/MagicDesc.Ini</c>
/// for Vietnamese descriptions and post-<c>0x128</c> attribute extensions (`JX Reborn Data Standard v1`).
/// </summary>
public sealed class MagicAttributeRegistry
{
    private static readonly (int Id, string Code)[] CanonicalMagicAttribStrings =
    [
        (0, "skill_begin"),
        (1, "skill_cost_v"),
        (2, "skill_costtype_v"),
        (3, "skill_mintimepercast_v"),
        (4, "skill_misslenum_v"),
        (5, "skill_misslesform_v"),
        (6, "skill_param1_v"),
        (7, "skill_param2_v"),
        (8, "skill_skillexp_v"),
        (9, "skill_waittime"),
        (10, "skill_mintimepercastonhorse_v"),
        (11, "skill_appendskill"),
        (12, "skill_eventskilllevel"),
        (13, "skill_end"),
        (14, "missle_begin"),
        (15, "missle_movekind_v"),
        (16, "missle_speed_v"),
        (17, "missle_lifetime_v"),
        (18, "missle_height_v"),
        (19, "missle_damagerange_v"),
        (20, "missle_radius_v"),
        (21, "missle_missrate"),
        (22, "missle_hitcount"),
        (23, "missle_reserve3"),
        (24, "missle_reserve4"),
        (25, "missle_reserve5"),
        (26, "missle_end"),
        (27, "item_begin"),
        (28, "weapondamagemin_v"),
        (29, "weapondamagemax_v"),
        (30, "armordefense_v"),
        (31, "durability_v"),
        (32, "requirestr"),
        (33, "requiredex"),
        (34, "requirevit"),
        (35, "requireeng"),
        (36, "requirelevel"),
        (37, "requireseries"),
        (38, "requiresex"),
        (39, "requiremenpai"),
        (40, "weapondamageenhance_p"),
        (41, "armordefenseenhance_p"),
        (42, "requirementreduce_p"),
        (43, "indestructible_b"),
        (44, "item_usagetime_v"),
        (45, "req_levelortranslife"),
        (46, "require_translife"),
        (47, "require_fortune_value"),
        (48, "item_reserve5"),
        (49, "item_reserve6"),
        (50, "item_reserve7"),
        (51, "item_reserve8"),
        (52, "item_reserve9"),
        (53, "item_reserve10"),
        (54, "item_end"),
        (55, "damage_begin"),
        (56, "attackrating_v"),
        (57, "attackrating_p"),
        (58, "ignoredefense_p"),
        (59, "physicsdamage_v"),
        (60, "colddamage_v"),
        (61, "firedamage_v"),
        (62, "lightingdamage_v"),
        (63, "poisondamage_v"),
        (64, "magicdamage_v"),
        (65, "physicsenhance_p"),
        (66, "steallife_p"),
        (67, "stealmana_p"),
        (68, "stealstamina_p"),
        (69, "knockback_p"),
        (70, "deadlystrike_p"),
        (71, "fatallystrike_p"),
        (72, "stun_p"),
        (73, "addskillexp1"),
        (74, "addskillexp2"),
        (75, "seriesdamage_p"),
        (76, "damage_reserve4"),
        (77, "damage_reserve5"),
        (78, "damage_reserve6"),
        (79, "damage_reserve7"),
        (80, "damage_reserve8"),
        (81, "damage_reserve9"),
        (82, "damage_reserve10"),
        (83, "damage_end"),
        (84, "normal_begin"),
        (85, "lifemax_v"),
        (86, "lifemax_p"),
        (87, "life_v"),
        (88, "lifereplenish_v"),
        (89, "manamax_v"),
        (90, "manamax_p"),
        (91, "mana_v"),
        (92, "manareplenish_v"),
        (93, "staminamax_v"),
        (94, "staminamax_p"),
        (95, "stamina_v"),
        (96, "staminareplenish_v"),
        (97, "strength_v"),
        (98, "dexterity_v"),
        (99, "vitality_v"),
        (100, "energy_v"),
        (101, "poisonres_p"),
        (102, "fireres_p"),
        (103, "lightingres_p"),
        (104, "physicsres_p"),
        (105, "coldres_p"),
        (106, "freezetimereduce_p"),
        (107, "burntimereduce_p"),
        (108, "poisontimereduce_p"),
        (109, "poisondamagereduce_v"),
        (110, "stuntimereduce_p"),
        (111, "fastwalkrun_p"),
        (112, "visionradius_p"),
        (113, "fasthitrecover_v"),
        (114, "allres_p"),
        (115, "attackspeed_v"),
        (116, "castspeed_v"),
        (117, "meleedamagereturn_v"),
        (118, "meleedamagereturn_p"),
        (119, "rangedamagereturn_v"),
        (120, "rangedamagereturn_p"),
        (121, "addphysicsdamage_v"),
        (122, "addfiredamage_v"),
        (123, "addcolddamage_v"),
        (124, "addlightingdamage_v"),
        (125, "addpoisondamage_v"),
        (126, "addphysicsdamage_p"),
        (127, "slowmissle_b"),
        (128, "changecamp_b"),
        (129, "physicsarmor_v"),
        (130, "coldarmor_v"),
        (131, "firearmor_v"),
        (132, "poisonarmor_v"),
        (133, "lightingarmor_v"),
        (134, "damage2addmana_p"),
        (135, "lucky_v"),
        (136, "steallifeenhance_p"),
        (137, "stealmanaenhance_p"),
        (138, "stealstaminaenhance_p"),
        (139, "allskill_v"),
        (140, "metalskill_v"),
        (141, "woodskill_v"),
        (142, "waterskill_v"),
        (143, "fireskill_v"),
        (144, "earthskill_v"),
        (145, "knockbackenhance_p"),
        (146, "deadlystrikeenhance_p"),
        (147, "stunenhance_p"),
        (148, "badstatustimereduce_v"),
        (149, "manashield_p"),
        (150, "adddefense_v"),
        (151, "adddefense_p"),
        (152, "fatallystrikeenhance_p"),
        (153, "lifepotion_v"),
        (154, "manapotion_v"),
        (155, "physicsresmax_p"),
        (156, "coldresmax_p"),
        (157, "fireresmax_p"),
        (158, "lightingresmax_p"),
        (159, "poisonresmax_p"),
        (160, "allresmax_p"),
        (161, "coldenhance_p"),
        (162, "fireenhance_p"),
        (163, "lightingenhance_p"),
        (164, "poisonenhance_p"),
        (165, "magicenhance_p"),
        (166, "attackratingenhance_v"),
        (167, "attackratingenhance_p"),
        (168, "addphysicsmagic_v"),
        (169, "addcoldmagic_v"),
        (170, "addfiremagic_v"),
        (171, "addlightingmagic_v"),
        (172, "addpoisonmagic_v"),
        (173, "fatallystrikeres_p"),
        (174, "statusimmunity_b"),
        (175, "expenhance_v"),
        (176, "expenhance_p"),
        (177, "seriesres_p"),
        (178, "seriesenhance_p"),
        (179, "createnpc"),
        (180, "autoattacknpc"),
        (181, "dynamicmagicshield_v"),
        (182, "nomovespeed"),
        (183, "execscript"),
        (184, "changefeature1"),
        (185, "changefeature2"),
        (186, "stealfeature"),
        (187, "addstealfeatureskill"),
        (188, "lucky_v_partner"),
        (189, "listen_msg"),
        (190, "lifereplenish_p"),
        (191, "ignoreskill_p"),
        (192, "returnskill_p"),
        (193, "poisondamagereturn_v"),
        (194, "poisondamagereturn_p"),
        (195, "autoreplyskill"),
        (196, "autoattackskill"),
        (197, "autorescueskill"),
        (198, "autodeathskill"),
        (199, "randmove"),
        (200, "hide"),
        (201, "ignorenegativestate_p"),
        (202, "poison2decmana_p"),
        (203, "staticmagicshield_v"),
        (204, "staticmagicshield_p"),
        (205, "returnres_p"),
        (206, "add120skillexpenhance_p"),
        (207, "dec_percasttimehorse"),
        (208, "dec_percasttime"),
        (209, "enhance_709_auto"),
        (210, "enhance_708_life_p"),
        (211, "enhance_93_life_v"),
        (212, "enhance_711_auto"),
        (213, "enhance_714_auto"),
        (214, "enhance_717_auto"),
        (215, "enhance_723_miss_p"),
        (216, "showpersoninfo"),
        (217, "ignoredamage"),
        (218, "sorbdamage_p"),
        (219, "anti_hitrecover"),
        (220, "anti_stuntimereduce_p"),
        (221, "anti_poisonres_p"),
        (222, "anti_fireres_p"),
        (223, "anti_lightingres_p"),
        (224, "anti_physicsres_p"),
        (225, "anti_coldres_p"),
        (226, "block_rate"),
        (227, "enhancehit_rate"),
        (228, "poisonres_yan_p"),
        (229, "lightingres_yan_p"),
        (230, "fireres_yan_p"),
        (231, "physicsres_yan_p"),
        (232, "coldres_yan_p"),
        (233, "lifemax_yan_v"),
        (234, "lifemax_yan_p"),
        (235, "manamax_yan_v"),
        (236, "manamax_yan_p"),
        (237, "sorbdamage_yan_p"),
        (238, "fastwalkrun_yan_p"),
        (239, "attackspeed_yan_v"),
        (240, "castspeed_yan_v"),
        (241, "allres_yan_p"),
        (242, "anti_maxres_p"),
        (243, "skill_enhance"),
        (244, "magicdamage_p"),
        (245, "fasthitrecover_yan_v"),
        (246, "five_elements_enhance_v"),
        (247, "five_elements_resist_v"),
        (249, "add_damage_p"),
        (250, "forbit_attack"),
        (251, "frozen_action"),
        (252, "forbit_takemedicine"),
        (253, "invincibility"),
        (254, "not_add_pkvalue_p"),
        (255, "add_boss_damage"),
        (256, "pk_punish_weaken"),
        (257, "pk_punish_enhance"),
        (258, "normal_end"),
        (259, "skillexpbegin"),
        (260, "addskilldamage1"),
        (261, "addskilldamage2"),
        (262, "addskilldamage3"),
        (263, "addskilldamage4"),
        (264, "addskilldamage5"),
        (265, "addskilldamage6"),
        (266, "skill_attackradius"),
        (267, "skill_startevent"),
        (268, "skill_flyevent"),
        (269, "skill_collideevent"),
        (270, "skill_vanishedevent"),
        (271, "skill_dohurt"),
        (272, "skill_bymissle"),
        (273, "skill_showevent"),
        (274, "skill_desc"),
        (279, "skillexpend"),
        (280, "missle_exp_begin"),
        (281, "missle_range"),
        (282, "missle_dmginterval"),
        (283, "missle_zspeed"),
        (284, "missle_ablility"),
        (285, "missle_param"),
        (286, "missle_wait"),
        (287, "missle_fly"),
        (288, "missle_collide"),
        (289, "missle_vanish"),
        (290, "missle_exp_rev1"),
        (291, "missle_exp_rev2"),
        (292, "missle_exp_rev3"),
        (293, "missle_exp_rev4"),
        (294, "missle_exp_rev5"),
        (295, "missle_exp_end"),
        (296, "magic_end"),
    ];

    private readonly FrozenDictionary<int, string> _codeById;
    private readonly FrozenDictionary<string, int> _idByCode;
    private readonly FrozenDictionary<int, string> _descTemplateById;
    private readonly List<(int AttributeId, string AttributeCode, int ConstValue)> _constRules;

    public IReadOnlyList<(int AttributeId, string AttributeCode, int ConstValue)> ConstRules => _constRules;

    private MagicAttributeRegistry(
        FrozenDictionary<int, string> codeById,
        FrozenDictionary<string, int> idByCode,
        FrozenDictionary<int, string> descTemplateById,
        List<(int, string, int)> constRules)
    {
        _codeById = codeById;
        _idByCode = idByCode;
        _descTemplateById = descTemplateById;
        _constRules = constRules;
    }

    public static MagicAttributeRegistry Load(string serverRootPath) =>
        !string.IsNullOrWhiteSpace(serverRootPath) && Directory.Exists(serverRootPath)
            ? Load(new FolderLegacyServerSource(serverRootPath))
            : Load((ILegacyServerSource?)null);

    public static MagicAttributeRegistry Load(ILegacyServerSource? source)
    {
        var codeById = new Dictionary<int, string>();
        var idByCode = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var descById = new Dictionary<int, string>();

        foreach (var (id, code) in CanonicalMagicAttribStrings)
        {
            codeById[id] = code;
            idByCode.TryAdd(code, id);
        }

        int nextExtId = 297;
        if (source is not null &&
            source.TryReadServerFile(@"settings\MagicDesc.Ini", out byte[]? rawMagicDesc))
        {
            int start = 0;
            for (int i = 0; i <= rawMagicDesc.Length; i++)
            {
                if (i == rawMagicDesc.Length || rawMagicDesc[i] == (byte)'\n')
                {
                    int len = i - start;
                    if (len > 0 && rawMagicDesc[start + len - 1] == (byte)'\r')
                    {
                        len--;
                    }

                    ReadOnlySpan<byte> line = rawMagicDesc.AsSpan(start, len);
                    ParseMagicDescLine(line, ref nextExtId, codeById, idByCode, descById);
                    start = i + 1;
                }
            }
        }

        var constRules = new List<(int, string, int)>();
        if (source is not null &&
            source.TryReadServerFile(@"settings\attribconstdata.ini", out byte[]? rawConst))
        {
            string constText = Encoding.Latin1.GetString(rawConst);
            foreach (string rawLine in constText.Split(['\r', '\n'], StringSplitOptions.None))
            {
                string line = rawLine.Trim();
                if (line.Length == 0 || line.StartsWith(';') || line.StartsWith("//", StringComparison.Ordinal) || line.StartsWith('['))
                {
                    continue;
                }

                int eq = line.IndexOf('=');
                if (eq <= 0)
                {
                    continue;
                }

                string key = line[..eq].Trim();
                string valStr = line[(eq + 1)..].Trim();
                if (int.TryParse(valStr, out int constVal))
                {
                    int attrId = idByCode.GetValueOrDefault(key, 0);
                    constRules.Add((attrId, key.ToLowerInvariant(), constVal));
                }
            }
        }

        return new MagicAttributeRegistry(
            codeById.ToFrozenDictionary(),
            idByCode.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase),
            descById.ToFrozenDictionary(),
            constRules);
    }

    public string GetCode(int attributeId) =>
        _codeById.TryGetValue(attributeId, out string? code) ? code : $"attr_{attributeId}";

    public int GetId(string attributeCode)
    {
        if (string.IsNullOrWhiteSpace(attributeCode))
        {
            return 0;
        }

        string trimmed = attributeCode.Trim();
        if (_idByCode.TryGetValue(trimmed, out int id))
        {
            return id;
        }

        return int.TryParse(trimmed, out int numeric) ? numeric : 0;
    }

    public string GetNameTemplate(int attributeId, string fallback) =>
        _descTemplateById.TryGetValue(attributeId, out string? tpl) && !string.IsNullOrWhiteSpace(tpl)
            ? tpl
            : fallback;

    public static string GetSkillSlotCategory(int attributeId, string attributeCode)
    {
        if (attributeCode.StartsWith("skill_", StringComparison.OrdinalIgnoreCase))
        {
            return "skillMechanic";
        }

        if (attributeCode.StartsWith("missle_", StringComparison.OrdinalIgnoreCase))
        {
            return "missileMechanic";
        }

        if (attributeId is >= 56 and <= 82 ||
            attributeCode.Contains("damage", StringComparison.OrdinalIgnoreCase) ||
            attributeCode.StartsWith("attackrating", StringComparison.OrdinalIgnoreCase))
        {
            return "damage";
        }

        if (attributeCode.Contains("skill", StringComparison.OrdinalIgnoreCase) &&
            attributeCode.Contains("enhance", StringComparison.OrdinalIgnoreCase))
        {
            return "synergy";
        }

        if (attributeId is 87 or 91 or 95 or 153 or 154)
        {
            return "immediate";
        }

        return "state";
    }

    private static void ParseMagicDescLine(
        ReadOnlySpan<byte> line,
        ref int nextExtId,
        Dictionary<int, string> codeById,
        Dictionary<string, int> idByCode,
        Dictionary<int, string> descById)
    {
        int s = 0;
        while (s < line.Length && (line[s] == (byte)' ' || line[s] == (byte)'\t'))
        {
            s++;
        }

        if (s >= line.Length || line[s] == (byte)'[' || line[s] == (byte)';')
        {
            return;
        }

        if (s + 1 < line.Length && line[s] == (byte)'/' && line[s + 1] == (byte)'/')
        {
            return;
        }

        int eq = line[s..].IndexOf((byte)'=');
        if (eq <= 0)
        {
            return;
        }

        ReadOnlySpan<byte> keyBytes = line.Slice(s, eq);
        ReadOnlySpan<byte> valBytes = line[(s + eq + 1)..];
        string key = Encoding.ASCII.GetString(keyBytes).Trim().ToLowerInvariant();
        string desc = HybridLegacyTextDecoder.DecodeSmart(valBytes, preferVietnamese: true).Trim();

        if (idByCode.TryGetValue(key, out int canonicalId))
        {
            if (desc.Length > 0)
            {
                descById.TryAdd(canonicalId, desc);
            }

            return;
        }

        // Map legacy MagicDesc.Ini alias keys to canonical IDs for descriptions without allocating bogus IDs
        if (key.StartsWith("skill_addskilldamage", StringComparison.Ordinal) &&
            idByCode.TryGetValue(key["skill_".Length..], out int addDmgId))
        {
            if (desc.Length > 0)
            {
                descById.TryAdd(addDmgId, desc);
            }

            return;
        }

        if (key == "mintimepercastonhorse_v" &&
            idByCode.TryGetValue("skill_mintimepercastonhorse_v", out int horseCastId))
        {
            if (desc.Length > 0)
            {
                descById.TryAdd(horseCastId, desc);
            }

            return;
        }

        // Post-0x128 extension attribute
        int extId = nextExtId++;
        codeById[extId] = key;
        idByCode[key] = extId;
        if (desc.Length > 0)
        {
            descById[extId] = desc;
        }
    }
}
