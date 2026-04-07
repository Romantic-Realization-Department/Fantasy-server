namespace Fantasy.Server.Domain.GameData.Entity;

public class StageData
{
    public long Stage { get; init; }
    public long MonsterHp { get; init; }
    public long MonsterAtk { get; init; }
    public long XpPerSecond { get; init; }
    public long GoldPerSecond { get; init; }
    public bool IsBossStage { get; init; }

    public static StageData Create(
        long stage,
        long monsterHp,
        long monsterAtk,
        long xpPerSecond,
        long goldPerSecond,
        bool isBossStage = false) => new()
    {
        Stage = stage,
        MonsterHp = monsterHp,
        MonsterAtk = monsterAtk,
        XpPerSecond = xpPerSecond,
        GoldPerSecond = goldPerSecond,
        IsBossStage = isBossStage
    };
}
