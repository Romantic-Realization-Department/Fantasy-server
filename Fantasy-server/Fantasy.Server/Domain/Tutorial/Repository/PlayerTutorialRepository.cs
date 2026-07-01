using Fantasy.Server.Domain.Tutorial.Entity;
using Fantasy.Server.Domain.Tutorial.Repository.Interface;
using Fantasy.Server.Global.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Fantasy.Server.Domain.Tutorial.Repository;

public class PlayerTutorialRepository : IPlayerTutorialRepository
{
    private readonly AppDbContext _db;

    public PlayerTutorialRepository(AppDbContext db) => _db = db;

    public async Task<List<PlayerTutorial>> FindAllByPlayerIdAsync(long playerId)
        => await _db.PlayerTutorials
            .AsNoTracking()
            .Where(t => t.PlayerId == playerId)
            .ToListAsync();

    public async Task<PlayerTutorial?> FindByPlayerIdAndTutorialIdAsync(long playerId, string tutorialId)
        => await _db.PlayerTutorials
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.PlayerId == playerId && t.TutorialId == tutorialId);

    public async Task<PlayerTutorial> SaveAsync(PlayerTutorial tutorial)
    {
        _db.PlayerTutorials.Add(tutorial);
        await _db.SaveChangesAsync();
        return tutorial;
    }
}
