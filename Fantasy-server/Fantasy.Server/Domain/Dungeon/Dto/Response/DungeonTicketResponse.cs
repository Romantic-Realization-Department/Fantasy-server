namespace Fantasy.Server.Domain.Dungeon.Dto.Response;

public record DungeonTicketResponse(
    int TicketCount,
    DateOnly LastDailyGrantDate,
    DateOnly? DailyAdRewardClaimedDate
);
