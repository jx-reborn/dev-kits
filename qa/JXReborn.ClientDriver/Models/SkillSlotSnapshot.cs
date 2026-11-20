using System.Text.Json.Serialization;

namespace JXReborn.ClientDriver;

/// <summary>
/// Learned skill entry from local <c>KNpc::m_SkillList</c>.
/// </summary>
public sealed record SkillSlotSnapshot(
    [property: JsonPropertyName("slot_index")] int SlotIndex,
    [property: JsonPropertyName("skill_id")] int SkillId,
    [property: JsonPropertyName("base_level")] int BaseLevel,
    [property: JsonPropertyName("cur_level")] int CurLevel);
