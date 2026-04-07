namespace Fantasy.Server.Domain.Player.Entity;

public class PlayerStage
{
    public long Id { get; private set; }
    public long PlayerId { get; private set; }
    public long MaxStage { get; private set; }

    public static PlayerStage Create(long playerId) => new()
    {
        PlayerId = playerId,
        MaxStage = 1
    };

    public void Update(long maxStage)
    {
        MaxStage = maxStage;
    }
}
