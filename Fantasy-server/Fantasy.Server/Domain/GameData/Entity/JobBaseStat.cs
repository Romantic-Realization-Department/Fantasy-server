using Fantasy.Server.Domain.Player.Enum;

namespace Fantasy.Server.Domain.GameData.Entity;

public class JobBaseStat
{
    public JobType JobType { get; init; }
    public long BaseHp { get; init; }
    public long BaseAtk { get; init; }
    public double CritRate { get; init; }
    public double CritDmgMultiplier { get; init; }
    public double HpPerLevel { get; init; }
    public double AtkPerLevel { get; init; }

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
