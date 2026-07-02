using Fantasy.Server.Domain.Dungeon.Enum;

namespace Fantasy.Server.Domain.Dungeon.Entity;

public class PlayerDungeonProgress
{
    public long Id { get; private set; }
    public long PlayerId { get; private set; }
    public DungeonType DungeonType { get; private set; }
    public long HighestClearedStage { get; private set; }
    public long HighScore { get; private set; }
    public DateTime? LastClearedAt { get; private set; }

    public static PlayerDungeonProgress Create(long playerId, DungeonType dungeonType) => new()
    {
        PlayerId = playerId,
        DungeonType = dungeonType,
        HighestClearedStage = 1,
        HighScore = 0,
        LastClearedAt = null
    };

    public void ClearStage(long stage)
    {
        if (stage > HighestClearedStage) HighestClearedStage = stage;
        LastClearedAt = DateTime.UtcNow;
    }

    public void UpdateHighScore(long score)
    {
        if (score > HighScore) HighScore = score;
        LastClearedAt = DateTime.UtcNow;
    }
}
