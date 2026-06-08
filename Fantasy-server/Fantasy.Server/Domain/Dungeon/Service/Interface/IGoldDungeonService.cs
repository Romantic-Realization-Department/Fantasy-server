using Fantasy.Server.Domain.Dungeon.Dto.Request;
using Fantasy.Server.Domain.Dungeon.Dto.Response;

namespace Fantasy.Server.Domain.Dungeon.Service.Interface;

public interface IGoldDungeonService
{
    Task<GoldDungeonResponse> ExecuteAsync(GoldDungeonRequest request);
}
