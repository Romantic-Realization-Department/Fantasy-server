using System.ComponentModel.DataAnnotations;
using Fantasy.Server.Domain.Player.Enum;

namespace Fantasy.Server.Domain.Player.Dto.Request;

public record EndPlayerSessionRequest(
    [Required] JobType JobType,
    [Required] int LastWeaponId,
    [Required] int[] ActiveSkills,
    [Range(0, long.MaxValue)] long? Gold,
    [Range(0, long.MaxValue)] long? Exp
);
