namespace Fantasy.Server.Domain.GameData.Entity;

public class LevelTable
{
    public long Level { get; init; }
    public long RequiredExp { get; init; }
    public long RewardSp { get; init; }

    public static LevelTable Create(long level, long requiredExp, long rewardSp) => new()
    {
        Level = level,
        RequiredExp = requiredExp,
        RewardSp = rewardSp
    };
}
