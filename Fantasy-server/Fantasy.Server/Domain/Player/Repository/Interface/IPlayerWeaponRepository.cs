using Fantasy.Server.Domain.Player.Dto.Request;
using Fantasy.Server.Domain.Player.Entity;

namespace Fantasy.Server.Domain.Player.Repository.Interface;

public interface IPlayerWeaponRepository
{
    Task<List<PlayerWeapon>> FindAllByPlayerIdAsync(long playerId);
    Task UpsertRangeAsync(long playerId, List<WeaponChangeItem> items);
}
