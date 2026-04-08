namespace Fantasy.Server.Domain.GameData.Entity;

public class LevelTable
{
    public long Level { get; private set; }
    public long RequiredExp { get; private set; }
    public long RewardSp { get; private set; }

    public static LevelTable Create(long level, long requiredExp, long rewardSp) => new()
    {
        Level = level,
        RequiredExp = requiredExp,
        RewardSp = rewardSp
    };
}
