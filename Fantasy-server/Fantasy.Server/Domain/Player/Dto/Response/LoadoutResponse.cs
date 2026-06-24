using Fantasy.Server.Domain.Dungeon.Dto.Response;

namespace Fantasy.Server.Domain.Player.Dto.Response;

public record LoadoutResponse(ChangesDto Changes, PlayerDataResponse Player);
