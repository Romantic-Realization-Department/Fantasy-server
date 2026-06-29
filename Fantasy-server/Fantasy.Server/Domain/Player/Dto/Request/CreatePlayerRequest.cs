using System.ComponentModel.DataAnnotations;
using Fantasy.Server.Domain.Player.Enum;

namespace Fantasy.Server.Domain.Player.Dto.Request;

public record CreatePlayerRequest(
    [Required][EnumDataType(typeof(JobType))] JobType JobType
);
