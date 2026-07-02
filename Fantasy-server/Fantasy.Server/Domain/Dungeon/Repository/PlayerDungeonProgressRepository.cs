using Fantasy.Server.Domain.Dungeon.Entity;
using Fantasy.Server.Domain.Dungeon.Enum;
using Fantasy.Server.Domain.Dungeon.Repository.Interface;
using Fantasy.Server.Global.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Fantasy.Server.Domain.Dungeon.Repository;

public class PlayerDungeonProgressRepository : IPlayerDungeonProgressRepository
{
    private readonly AppDbContext _db;

    public PlayerDungeonProgressRepository(AppDbContext db) => _db = db;

    public async Task<PlayerDungeonProgress?> FindByPlayerIdAndDungeonTypeAsync(long playerId, DungeonType dungeonType)
        => await _db.PlayerDungeonProgresses
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.PlayerId == playerId && p.DungeonType == dungeonType);

    public async Task SaveAsync(PlayerDungeonProgress progress)
    {
        await _db.PlayerDungeonProgresses.AddAsync(progress);
        await _db.SaveChangesAsync();
    }

    public async Task UpdateAsync(PlayerDungeonProgress progress)
    {
        _db.PlayerDungeonProgresses.Update(progress);
        await _db.SaveChangesAsync();
    }
}
