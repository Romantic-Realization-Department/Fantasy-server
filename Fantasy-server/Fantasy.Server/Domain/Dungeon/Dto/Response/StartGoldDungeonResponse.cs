using Fantasy.Server.Domain.Player.Dto.Response;

namespace Fantasy.Server.Domain.Dungeon.Dto.Response;

public record StartGoldDungeonResponse(
    Guid RunId,
    DateTime StartedAt,
    int DurationSeconds,
    DateTime ExpiresAt,
    int MaxClicks,
    PlayerDataResponse Player
);
