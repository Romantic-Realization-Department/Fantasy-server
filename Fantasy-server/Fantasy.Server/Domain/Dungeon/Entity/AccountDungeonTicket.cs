namespace Fantasy.Server.Domain.Dungeon.Entity;

public class AccountDungeonTicket
{
    public long Id { get; private set; }
    public long AccountId { get; private set; }
    public int TicketCount { get; private set; }
    public DateOnly LastDailyGrantDate { get; private set; }
    public DateOnly? DailyAdRewardClaimedDate { get; private set; }

    public static AccountDungeonTicket Create(long accountId, DateOnly todayKst) => new()
    {
        AccountId = accountId,
        TicketCount = 3,
        LastDailyGrantDate = todayKst,
        DailyAdRewardClaimedDate = null
    };

    public void GrantDaily(DateOnly todayKst)
    {
        TicketCount = 3;
        LastDailyGrantDate = todayKst;
        DailyAdRewardClaimedDate = null;
    }

    public void Consume()
    {
        if (TicketCount <= 0)
            throw new InvalidOperationException("티켓이 부족합니다.");
        TicketCount--;
    }

    public void GrantAdReward(DateOnly todayKst)
    {
        TicketCount++;
        DailyAdRewardClaimedDate = todayKst;
    }
}
