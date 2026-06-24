using Fantasy.Server.Domain.Player.Entity;
using Fantasy.Server.Domain.Player.Repository.Interface;
using Fantasy.Server.Global.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Fantasy.Server.Domain.Player.Repository;

public class PlayerStageRepository : IPlayerStageRepository
{
    private readonly AppDbContext _db;

    public PlayerStageRepository(AppDbContext db) => _db = db;

    public async Task<PlayerStage?> FindByPlayerIdAsync(long playerId)
        => await _db.PlayerStages
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.PlayerId == playerId);

    public async Task<PlayerStage> SaveAsync(PlayerStage stage)
    {
        if (_db.PlayerStages.Entry(stage).State == EntityState.Detached)
            await _db.PlayerStages.AddAsync(stage);
        await _db.SaveChangesAsync();
        return stage;
    }

    public async Task UpdateAsync(PlayerStage stage)
    {
        _db.PlayerStages.Update(stage);
        await _db.SaveChangesAsync();
    }
}