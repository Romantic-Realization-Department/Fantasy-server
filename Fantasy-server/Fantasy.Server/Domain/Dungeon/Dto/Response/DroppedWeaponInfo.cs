using Fantasy.Server.Domain.GameData.Enum;

namespace Fantasy.Server.Domain.Dungeon.Dto.Response;

public record DroppedWeaponInfo(int WeaponId, string Name, WeaponGrade Grade);
