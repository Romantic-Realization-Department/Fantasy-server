using Fantasy.Server.Domain.Player.Enum;

namespace Fantasy.Server.Domain.Player.Entity;

public class Player
{
    public long Id { get; private set; }
    public long AccountId { get; private set; }
    public JobType JobType { get; private set; }
    public long Level { get; private set; }
    public long Exp { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    public static Player Create(long accountId, JobType jobType) => new()
    {
        AccountId = accountId,
        JobType = jobType,
        Level = 1,
        Exp = 0,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };

    public void UpdateLevel(long level)
    {
        Level = level;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateExp(long exp)
    {
        Exp = exp;
        UpdatedAt = DateTime.UtcNow;
    }
}