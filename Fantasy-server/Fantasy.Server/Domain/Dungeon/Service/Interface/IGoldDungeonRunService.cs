using Fantasy.Server.Domain.Dungeon.Dto.Response;

namespace Fantasy.Server.Domain.Dungeon.Service.Interface;

public interface IGoldDungeonRunService
{
    Task<StartGoldDungeonResponse> ExecuteAsync();
}
