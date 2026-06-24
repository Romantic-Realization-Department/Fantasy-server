using Fantasy.Server.Domain.Player.Enum;

namespace Fantasy.Server.Domain.GameData.Entity;

public class JobBaseStat
{
    public JobType JobType { get; private set; }
    public long BaseHp { get; private set; }
    public long BaseAtk { get; private set; }
    public double CritRate { get; private set; }
    public double CritDmgMultiplier { get; private set; }
    public double HpPerLevel { get; private set; }
    public double AtkPerLevel { get; private set; }

    public static JobBaseStat Create(
        JobType jobType,
        long baseHp,
        long baseAtk,
        double critRate,
        double critDmgMultiplier,
        double hpPerLevel,
        double atkPerLevel) => new()
    {
        JobType = jobType,
        BaseHp = baseHp,
        BaseAtk = baseAtk,
        CritRate = critRate,
        CritDmgMultiplier = critDmgMultiplier,
        HpPerLevel = hpPerLevel,
        AtkPerLevel = atkPerLevel
    };
}
