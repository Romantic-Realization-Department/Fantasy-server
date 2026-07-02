using Fantasy.Server.Domain.GameData.Enum;
using Fantasy.Server.Domain.Player.Enum;

namespace Fantasy.Server.Domain.GameData.Entity;

public class WeaponData
{
    public int WeaponId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public WeaponGrade Grade { get; private set; }
    public JobType JobType { get; private set; }
    public long BaseAtk { get; private set; }
    public long AtkPerEnhancement { get; private set; }
    public long MaxEnhancementLevel { get; private set; }
    public long MaxAwakeningLevel { get; private set; }
    public int? SynthesizeRequiredCount { get; private set; }
    public int? SynthesizeResultWeaponId { get; private set; }

    public static WeaponData Create(
        int weaponId,
        string name,
        WeaponGrade grade,
        JobType jobType,
        long baseAtk,
        long atkPerEnhancement,
        long maxEnhancementLevel = 0,
        long maxAwakeningLevel = 0,
        int? synthesizeRequiredCount = null,
        int? synthesizeResultWeaponId = null) => new()
    {
        WeaponId = weaponId,
        Name = name,
        Grade = grade,
        JobType = jobType,
        BaseAtk = baseAtk,
        AtkPerEnhancement = atkPerEnhancement,
        MaxEnhancementLevel = maxEnhancementLevel,
        MaxAwakeningLevel = maxAwakeningLevel,
        SynthesizeRequiredCount = synthesizeRequiredCount,
        SynthesizeResultWeaponId = synthesizeResultWeaponId
    };
}
