using Fantasy.Server.Domain.Dungeon.Entity;
using Fantasy.Server.Domain.Dungeon.Enum;

namespace Fantasy.Server.Domain.Dungeon.Repository.Interface;

public interface IPlayerDungeonProgressRepository
{
    Task<PlayerDungeonProgress?> FindByPlayerIdAndDungeonTypeAsync(long playerId, DungeonType dungeonType);
    Task SaveAsync(PlayerDungeonProgress progress);
    Task UpdateAsync(PlayerDungeonProgress progress);
}
