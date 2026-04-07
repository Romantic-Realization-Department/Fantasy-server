using System.ComponentModel.DataAnnotations;

namespace Fantasy.Server.Domain.Player.Dto.Request;

public record WeaponChangeItem(
    [Required] int WeaponId,
    [Range(0, long.MaxValue)] long Count,
    [Range(0, long.MaxValue)] long EnhancementLevel,
    [Range(0, long.MaxValue)] long AwakeningCount
);

public record SkillChangeItem(
    [Required] int SkillId,
    [Required] bool IsUnlocked
);
