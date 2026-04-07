namespace Fantasy.Server.Domain.GameData.Entity;

public class StageData
{
    public long Stage { get; private set; }
    public long MonsterHp { get; private set; }
    public long MonsterAtk { get; private set; }
    public long XpPerSecond { get; private set; }
    public long GoldPerSecond { get; private set; }
    public bool IsBossStage { get; private set; }

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
