using System.ComponentModel.DataAnnotations;

namespace Fantasy.Server.Domain.Player.Dto.Request;

public record EndPlayerSessionRequest(
    [Required] int LastWeaponId,
    [Required] int[] ActiveSkills
);
