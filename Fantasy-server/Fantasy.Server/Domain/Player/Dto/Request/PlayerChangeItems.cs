using System.ComponentModel.DataAnnotations;

namespace Fantasy.Server.Domain.Player.Dto.Request;

public record SkillChangeItem(
    [Required] int SkillId,
    [Required] bool IsUnlocked
);
