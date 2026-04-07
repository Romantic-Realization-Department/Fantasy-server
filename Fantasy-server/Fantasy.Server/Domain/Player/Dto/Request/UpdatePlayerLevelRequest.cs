using System.ComponentModel.DataAnnotations;
using Fantasy.Server.Domain.Player.Enum;

namespace Fantasy.Server.Domain.Player.Dto.Request;

public record UpdatePlayerLevelRequest(
    [Required] JobType JobType,
    [Range(1, long.MaxValue)] long Level
);
