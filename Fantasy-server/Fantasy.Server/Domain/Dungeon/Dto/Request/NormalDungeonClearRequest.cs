using System.ComponentModel.DataAnnotations;

namespace Fantasy.Server.Domain.Dungeon.Dto.Request;

public record NormalDungeonClearRequest(
    [Required][Range(1, long.MaxValue)] long Stage
);
