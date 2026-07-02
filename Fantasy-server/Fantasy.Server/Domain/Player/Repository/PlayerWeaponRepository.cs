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

    public async Task GrantWeaponsAsync(long playerId, List<int> weaponIds)
    {
        List<int> distinctIds = weaponIds.Distinct().ToList();
        Dictionary<int, PlayerWeapon> existing = await _db.PlayerWeapons
            .Where(weapon => weapon.PlayerId == playerId && distinctIds.Contains(weapon.WeaponId))
            .ToDictionaryAsync(weapon => weapon.WeaponId);

        foreach (int weaponId in weaponIds)
        {
            if (existing.TryGetValue(weaponId, out PlayerWeapon? weapon))
            {
                weapon.AddCount(1);
                continue;
            }

            PlayerWeapon created = PlayerWeapon.Create(playerId, weaponId, 1, 0, 0);
            await _db.PlayerWeapons.AddAsync(created);
            existing[weaponId] = created;
        }

        await _db.SaveChangesAsync();
    }

    public async Task<PlayerWeapon?> FindByPlayerIdAndWeaponIdAsync(long playerId, int weaponId)
        => await _db.PlayerWeapons
            .FirstOrDefaultAsync(w => w.PlayerId == playerId && w.WeaponId == weaponId);

    public async Task SaveAsync(PlayerWeapon weapon)
    {
        _db.PlayerWeapons.Add(weapon);
        await _db.SaveChangesAsync();
    }

    public async Task UpdateAsync(PlayerWeapon weapon)
    {
        _db.PlayerWeapons.Update(weapon);
        await _db.SaveChangesAsync();
    }
}
