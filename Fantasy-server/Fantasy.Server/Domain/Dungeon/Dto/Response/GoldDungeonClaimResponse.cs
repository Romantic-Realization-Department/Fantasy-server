using Fantasy.Server.Domain.Player.Dto.Response;

namespace Fantasy.Server.Domain.Dungeon.Dto.Response;

public record GoldDungeonClaimResponse(
    Guid RunId,
    long EarnedGold,
    int EarnedMithril,
    ChangesDto Changes,
    PlayerDataResponse Player
);
