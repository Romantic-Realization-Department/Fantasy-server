namespace Fantasy.Server.Domain.Player.Entity;

public class PlayerStage
{
    public long Id { get; private set; }
    public long PlayerId { get; private set; }
    public long MaxStage { get; private set; }
    public DateTime LastCalculatedAt { get; private set; }

    public static PlayerStage Create(long playerId) => new()
    {
        PlayerId = playerId,
        MaxStage = 1,
        LastCalculatedAt = DateTime.UtcNow
    };

    public static PlayerStage Create(long playerId, long maxStage, DateTime lastCalculatedAt) => new()
    {
        PlayerId = playerId,
        MaxStage = maxStage,
        LastCalculatedAt = lastCalculatedAt
    };

    public void Update(long maxStage)
    {
        MaxStage = maxStage;
    }

    public void UpdateLastCalculatedAt()
    {
        LastCalculatedAt = DateTime.UtcNow;
    }
}
