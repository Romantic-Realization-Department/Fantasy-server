using Fantasy.Server.Domain.GameData.Enum;
using Fantasy.Server.Domain.Player.Enum;

namespace Fantasy.Server.Domain.GameData.Entity;

public class SkillData
{
    public int SkillId { get; init; }
    public JobType JobType { get; init; }
    public bool IsActive { get; init; }
    public long SpCost { get; init; }
    public int? PrereqSkillId { get; init; }
    public SkillEffectType EffectType { get; init; }
    public double EffectValue { get; init; }

    public static SkillData Create(
        int skillId,
        JobType jobType,
        bool isActive,
        long spCost,
        int? prereqSkillId,
        SkillEffectType effectType,
        double effectValue) => new()
    {
        SkillId = skillId,
        JobType = jobType,
        IsActive = isActive,
        SpCost = spCost,
        PrereqSkillId = prereqSkillId,
        EffectType = effectType,
        EffectValue = effectValue
    };
}
