namespace Fantasy.Server.Domain.Player.Entity;

public class RewardTransaction
{
    public Guid Id { get; private set; }
    public long PlayerId { get; private set; }
    public string SourceType { get; private set; } = string.Empty;
    public string? SourceRefId { get; private set; }
    public string RewardType { get; private set; } = string.Empty;
    public string? RewardRefId { get; private set; }
    public long Amount { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public static RewardTransaction Create(
        long playerId,
        string sourceType,
        string? sourceRefId,
        string rewardType,
        string? rewardRefId,
        long amount) => new()
    {
        Id = Guid.CreateVersion7(),
        PlayerId = playerId,
        SourceType = sourceType,
        SourceRefId = sourceRefId,
        RewardType = rewardType,
        RewardRefId = rewardRefId,
        Amount = amount,
        CreatedAt = DateTime.UtcNow
    };
}
