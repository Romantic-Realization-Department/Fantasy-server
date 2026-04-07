using Fantasy.Server.Domain.Player.Entity;

namespace Fantasy.Server.Domain.Player.Repository.Interface;

public interface IPlayerSessionRepository
{
    Task<PlayerSession?> FindByPlayerIdAsync(long playerId);
    Task<PlayerSession> SaveAsync(PlayerSession session);
    Task UpdateAsync(PlayerSession session);
}