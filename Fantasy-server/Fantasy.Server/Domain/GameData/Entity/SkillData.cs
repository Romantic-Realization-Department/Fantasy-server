using Fantasy.Server.Domain.GameData.Enum;
using Fantasy.Server.Domain.Player.Enum;

namespace Fantasy.Server.Domain.GameData.Entity;

public class SkillData
{
    public int SkillId { get; private set; }
    public JobType JobType { get; private set; }
    public bool IsActive { get; private set; }
    public long SpCost { get; private set; }
    public int? PrereqSkillId { get; private set; }
    public SkillEffectType EffectType { get; private set; }
    public double EffectValue { get; private set; }

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
