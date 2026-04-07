using System.ComponentModel.DataAnnotations;
using Fantasy.Server.Domain.Player.Enum;

namespace Fantasy.Server.Domain.Player.Dto.Request;

public record UpdatePlayerWeaponRequest(
    [Required] JobType JobType,
    [Required] List<WeaponChangeItem> Weapons
);
