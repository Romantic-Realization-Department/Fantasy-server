using Fantasy.Server.Domain.Dungeon.Dto.Request;
using Fantasy.Server.Domain.Dungeon.Dto.Response;

namespace Fantasy.Server.Domain.Dungeon.Service.Interface;

public interface IGoldDungeonClaimService
{
    Task<GoldDungeonClaimResponse> ExecuteAsync(Guid runId, GoldDungeonClaimRequest request);
}
