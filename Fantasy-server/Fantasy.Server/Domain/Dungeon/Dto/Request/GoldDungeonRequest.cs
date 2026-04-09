using System.ComponentModel.DataAnnotations;

namespace Fantasy.Server.Domain.Dungeon.Dto.Request;

public record GoldDungeonRequest(
    [Required][Range(0, int.MaxValue)] int Clicks
);
