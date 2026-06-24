using Fantasy.Server.Domain.Player.Entity;
using Fantasy.Server.Domain.Player.Repository.Interface;
using Fantasy.Server.Global.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Fantasy.Server.Domain.Player.Repository;

public class PlayerResourceRepository : IPlayerResourceRepository
{
    private readonly AppDbContext _db;

    public PlayerResourceRepository(AppDbContext db) => _db = db;

    public async Task<PlayerResource?> FindByPlayerIdAsync(long playerId)
        => await _db.PlayerResources
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.PlayerId == playerId);

    public async Task<PlayerResource> SaveAsync(PlayerResource resource)
    {
        if (_db.PlayerResources.Entry(resource).State == EntityState.Detached)
            await _db.PlayerResources.AddAsync(resource);
        await _db.SaveChangesAsync();
        return resource;
    }

    public async Task UpdateAsync(PlayerResource resource)
    {
        _db.PlayerResources.Update(resource);
        await _db.SaveChangesAsync();
    }
}
