using System.ComponentModel.DataAnnotations;
using Fantasy.Server.Domain.Player.Enum;

namespace Fantasy.Server.Domain.Player.Dto.Request;

public record UpdatePlayerSkillRequest(
    [Required] JobType JobType,
    [Required] List<SkillChangeItem> Skills
);
