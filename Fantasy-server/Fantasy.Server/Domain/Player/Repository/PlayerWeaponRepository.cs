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
        List<WeaponChangeItem> normalizedItems = items
            .GroupBy(item => item.WeaponId)
            .Select(group => group.Last())
            .ToList();

        List<int> weaponIds = normalizedItems.Select(item => item.WeaponId).ToList();
        Dictionary<int, PlayerWeapon> existing = await _db.PlayerWeapons
            .Where(weapon => weapon.PlayerId == playerId && weaponIds.Contains(weapon.WeaponId))
            .ToDictionaryAsync(weapon => weapon.WeaponId);

        foreach (WeaponChangeItem item in normalizedItems)
        {
            if (existing.TryGetValue(item.WeaponId, out PlayerWeapon? weapon))
            {
                weapon.Update(item.Count, item.EnhancementLevel, item.AwakeningCount);
                _db.PlayerWeapons.Update(weapon);
                continue;
            }

            await _db.PlayerWeapons.AddAsync(
                PlayerWeapon.Create(playerId, item.WeaponId, item.Count, item.EnhancementLevel, item.AwakeningCount));
        }

        await _db.SaveChangesAsync();
    }
}
