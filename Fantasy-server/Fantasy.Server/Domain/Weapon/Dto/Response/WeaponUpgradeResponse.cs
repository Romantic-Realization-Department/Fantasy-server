using Fantasy.Server.Domain.Dungeon.Dto.Response;
using Fantasy.Server.Domain.Player.Dto.Response;

namespace Fantasy.Server.Domain.Weapon.Dto.Response;

public record WeaponUpgradeResponse(
    int WeaponId,
    long EnhancementLevel,
    ChangesDto Changes,
    PlayerDataResponse Player
);
