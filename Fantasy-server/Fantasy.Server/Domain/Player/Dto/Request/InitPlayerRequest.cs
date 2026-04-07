using System.ComponentModel.DataAnnotations;
using Fantasy.Server.Domain.Player.Enum;

namespace Fantasy.Server.Domain.Player.Dto.Request;

public record InitPlayerRequest(
    [Required] JobType JobType
);
