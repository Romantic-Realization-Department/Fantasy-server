using Fantasy.Server.Domain.Player.Entity;
using Fantasy.Server.Domain.Player.Repository.Interface;
using Fantasy.Server.Global.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Fantasy.Server.Domain.Player.Repository;

public class PlayerSessionRepository : IPlayerSessionRepository
{
    private readonly AppDbContext _db;

    public PlayerSessionRepository(AppDbContext db) => _db = db;

    public async Task<PlayerSession?> FindByPlayerIdAsync(long playerId)
        => await _db.PlayerSessions
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.PlayerId == playerId);

    public async Task<PlayerSession> SaveAsync(PlayerSession session)
    {
        if (_db.PlayerSessions.Entry(session).State == EntityState.Detached)
            await _db.PlayerSessions.AddAsync(session);
        await _db.SaveChangesAsync();
        return session;
    }

    public async Task UpdateAsync(PlayerSession session)
    {
        _db.PlayerSessions.Update(session);
        await _db.SaveChangesAsync();
    }
}