using Fantasy.Server.Domain.Dungeon.Entity;

namespace Fantasy.Server.Domain.Dungeon.Repository.Interface;

public interface IGoldDungeonRunRepository
{
    Task<GoldDungeonRun?> FindByIdAsync(Guid runId);
    Task SaveAsync(GoldDungeonRun run);
    Task UpdateAsync(GoldDungeonRun run);
}
