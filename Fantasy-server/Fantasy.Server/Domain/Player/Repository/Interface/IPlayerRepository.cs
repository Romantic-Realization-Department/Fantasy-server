using PlayerEntity = Fantasy.Server.Domain.Player.Entity.Player;

namespace Fantasy.Server.Domain.Player.Repository.Interface;

public interface IPlayerRepository
{
    Task<PlayerEntity?> FindByAccountAsync(long accountId);
    Task<PlayerEntity> SaveAsync(PlayerEntity player);
    Task UpdateAsync(PlayerEntity player);
}
