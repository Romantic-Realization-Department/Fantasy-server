using Fantasy.Server.Domain.Player.Dto.Response;
using Fantasy.Server.Domain.Player.Enum;

namespace Fantasy.Server.Domain.Player.Repository.Interface;

public interface IPlayerRedisRepository
{
    Task SetPlayerDataAsync(long accountId, JobType jobType, PlayerDataResponse data);
    Task<PlayerDataResponse?> GetPlayerDataAsync(long accountId, JobType jobType);
    Task DeleteAsync(long accountId, JobType jobType);
}
