using Fantasy.Server.Domain.Dungeon.Dto.Response;
using Fantasy.Server.Domain.Player.Dto.Response;

namespace Fantasy.Server.Domain.Weapon.Dto.Response;

public record WeaponAwakenResponse(
    int WeaponId,
    long AwakeningCount,
    ChangesDto Changes,
    PlayerDataResponse Player
);
