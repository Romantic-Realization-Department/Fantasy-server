using Fantasy.Server.Domain.Player.Dto.Response;

namespace Fantasy.Server.Domain.Dungeon.Dto.Response;

public record BasicDungeonClaimResponse(ChangesDto Changes, PlayerDataResponse Player);
