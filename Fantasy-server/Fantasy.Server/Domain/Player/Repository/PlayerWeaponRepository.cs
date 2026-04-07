using Fantasy.Server.Domain.Player.Dto.Request;
using Fantasy.Server.Domain.Player.Entity;
using Fantasy.Server.Domain.Player.Repository.Interface;
using Fantasy.Server.Global.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Fantasy.Server.Domain.Player.Repository;

public class PlayerWeaponRepository : IPlayerWeaponRepository
{
    private readonly AppDbContext _db;

    public PlayerWeaponRepository(AppDbContext db) => _db = db;

    public async Task<List<PlayerWeapon>> FindAllByPlayerIdAsync(long playerId)
        => await _db.PlayerWeapons
            .AsNoTracking()
            .Where(w => w.PlayerId == playerId)
            .ToListAsync();

    public async Task UpsertRangeAsync(long playerId, List<WeaponChangeItem> items)
    {
        var weaponIds = items.Select(i => i.WeaponId).ToList();
        var existing = await _db.PlayerWeapons
            .Where(w => w.PlayerId == playerId && weaponIds.Contains(w.WeaponId))
            .ToListAsync();

        foreach (var item in items)
        {
            var weapon = existing.FirstOrDefault(w => w.WeaponId == item.WeaponId);
            if (weapon != null)
            {
                weapon.Update(item.Count, item.EnhancementLevel, item.AwakeningCount);
                _db.PlayerWeapons.Update(weapon);
            }
            else
            {
                await _db.PlayerWeapons.AddAsync(
                    PlayerWeapon.Create(playerId, item.WeaponId, item.Count, item.EnhancementLevel, item.AwakeningCount));
            }
        }

        await _db.SaveChangesAsync();
    }
}
