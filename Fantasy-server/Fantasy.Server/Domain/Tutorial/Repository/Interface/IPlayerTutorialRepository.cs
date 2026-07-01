using Fantasy.Server.Domain.Tutorial.Entity;

namespace Fantasy.Server.Domain.Tutorial.Repository.Interface;

public interface IPlayerTutorialRepository
{
    Task<List<PlayerTutorial>> FindAllByPlayerIdAsync(long playerId);
    Task<PlayerTutorial?> FindByPlayerIdAndTutorialIdAsync(long playerId, string tutorialId);
    Task<PlayerTutorial> SaveAsync(PlayerTutorial tutorial);
}
