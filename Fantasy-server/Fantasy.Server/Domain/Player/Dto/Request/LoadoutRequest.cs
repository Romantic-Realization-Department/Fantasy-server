using System.ComponentModel.DataAnnotations;

namespace Fantasy.Server.Domain.Player.Dto.Request;

public record LoadoutRequest(
    int? WeaponId,
    [Required] int[] ActiveSkills
);
