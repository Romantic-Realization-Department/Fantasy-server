using Fantasy.Server.Domain.Player.Entity;

namespace Fantasy.Server.Domain.Player.Repository.Interface;

public interface IPlayerResourceRepository
{
    Task<PlayerResource?> FindByPlayerIdAsync(long playerId);
    Task<PlayerResource> SaveAsync(PlayerResource resource);
    Task UpdateAsync(PlayerResource resource);
}
