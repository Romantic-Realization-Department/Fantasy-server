using Fantasy.Server.Domain.Player.Enum;
using Fantasy.Server.Domain.Player.Repository.Interface;
using Fantasy.Server.Global.Infrastructure;
using Microsoft.EntityFrameworkCore;
using PlayerEntity = Fantasy.Server.Domain.Player.Entity.Player;

namespace Fantasy.Server.Domain.Player.Repository;

public class PlayerRepository : IPlayerRepository
{
    private readonly AppDbContext _db;

    public PlayerRepository(AppDbContext db) => _db = db;

    public async Task<PlayerEntity?> FindByAccountAndJobAsync(long accountId, JobType jobType)
        => await _db.Players
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.AccountId == accountId && p.JobType == jobType);

    public async Task<PlayerEntity> SaveAsync(PlayerEntity player)
    {
        if (_db.Players.Entry(player).State == EntityState.Detached)
            await _db.Players.AddAsync(player);
        await _db.SaveChangesAsync();
        return player;
    }

    public async Task UpdateAsync(PlayerEntity player)
    {
        _db.Players.Update(player);
        await _db.SaveChangesAsync();
    }
}
