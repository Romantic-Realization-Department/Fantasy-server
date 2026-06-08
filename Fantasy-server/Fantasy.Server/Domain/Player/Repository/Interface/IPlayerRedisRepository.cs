using Fantasy.Server.Domain.Player.Dto.Response;

namespace Fantasy.Server.Domain.Player.Repository.Interface;

public interface IPlayerRedisRepository
{
    Task SetPlayerDataAsync(long accountId, PlayerDataResponse data);
    Task<PlayerDataResponse?> GetPlayerDataAsync(long accountId);
    Task DeleteAsync(long accountId);
}
