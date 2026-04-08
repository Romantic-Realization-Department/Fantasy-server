using Fantasy.Server.Domain.Dungeon.Dto.Response;
using Fantasy.Server.Domain.Player.Enum;

namespace Fantasy.Server.Domain.Dungeon.Service.Interface;

public interface IBasicDungeonClaimService
{
    Task<BasicDungeonClaimResponse> ExecuteAsync(JobType jobType);
}
