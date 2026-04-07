using System.ComponentModel.DataAnnotations;
using Fantasy.Server.Domain.Player.Enum;

namespace Fantasy.Server.Domain.Player.Dto.Request;

public record UpdatePlayerResourceRequest(
    [Required] JobType JobType,
    [Range(0, long.MaxValue)] long? EnhancementScroll,
    [Range(0, long.MaxValue)] long? Mithril,
    [Range(0, long.MaxValue)] long? Sp
);
