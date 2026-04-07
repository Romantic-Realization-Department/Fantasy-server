using Fantasy.Server.Domain.Player.Enum;
using PlayerEntity = Fantasy.Server.Domain.Player.Entity.Player;

namespace Fantasy.Server.Domain.Player.Repository.Interface;

public interface IPlayerRepository
{
    Task<PlayerEntity?> FindByAccountAndJobAsync(long accountId, JobType jobType);
    Task<PlayerEntity> SaveAsync(PlayerEntity player);
    Task UpdateAsync(PlayerEntity player);
}
