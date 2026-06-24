using Fantasy.Server.Domain.Player.Entity;

namespace Fantasy.Server.Domain.Player.Repository.Interface;

public interface IPlayerStageRepository
{
    Task<PlayerStage?> FindByPlayerIdAsync(long playerId);
    Task<PlayerStage> SaveAsync(PlayerStage stage);
    Task UpdateAsync(PlayerStage stage);
}