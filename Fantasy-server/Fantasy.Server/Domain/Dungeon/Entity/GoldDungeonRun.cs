namespace Fantasy.Server.Domain.Dungeon.Entity;

public class GoldDungeonRun
{
    public Guid Id { get; private set; }
    public long AccountId { get; private set; }
    public DateTime StartedAt { get; private set; }
    public int DurationSeconds { get; private set; }
    public DateTime ExpiresAt { get; private set; }
    public int MaxClicks { get; private set; }
    public DateTime? ClaimedAt { get; private set; }
    public int? ClaimedClicks { get; private set; }
    public long? EarnedGold { get; private set; }
    public int? EarnedMithril { get; private set; }

    public bool IsClaimed => ClaimedAt.HasValue;

    public static GoldDungeonRun Create(long accountId, int durationSeconds, int maxClicksPerSecond)
    {
        var now = DateTime.UtcNow;
        return new GoldDungeonRun
        {
            Id = Guid.NewGuid(),
            AccountId = accountId,
            StartedAt = now,
            DurationSeconds = durationSeconds,
            ExpiresAt = now.AddSeconds(durationSeconds + 30),
            MaxClicks = maxClicksPerSecond * durationSeconds
        };
    }

    public void Claim(int clicks, long earnedGold, int earnedMithril)
    {
        ClaimedAt = DateTime.UtcNow;
        ClaimedClicks = clicks;
        EarnedGold = earnedGold;
        EarnedMithril = earnedMithril;
    }
}
