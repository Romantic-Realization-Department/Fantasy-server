using Fantasy.Server.Domain.Dungeon.Dto.Request;
using Fantasy.Server.Domain.Dungeon.Dto.Response;
using Fantasy.Server.Domain.Player.Enum;

namespace Fantasy.Server.Domain.Dungeon.Service.Interface;

public interface INormalDungeonClearService
{
    Task<NormalDungeonClearResponse> ExecuteAsync(JobType jobType, NormalDungeonClearRequest request);
}
