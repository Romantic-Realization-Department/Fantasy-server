using Fantasy.Server.Domain.Dungeon.Entity;
using Fantasy.Server.Domain.Dungeon.Repository.Interface;
using Fantasy.Server.Global.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Fantasy.Server.Domain.Dungeon.Repository;

public class GoldDungeonRunRepository : IGoldDungeonRunRepository
{
    private readonly AppDbContext _dbContext;

    public GoldDungeonRunRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<GoldDungeonRun?> FindByIdAsync(Guid runId) =>
        await _dbContext.GoldDungeonRuns
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == runId);

    public async Task SaveAsync(GoldDungeonRun run)
    {
        _dbContext.GoldDungeonRuns.Add(run);
        await _dbContext.SaveChangesAsync();
    }

    public async Task UpdateAsync(GoldDungeonRun run)
    {
        _dbContext.GoldDungeonRuns.Update(run);
        await _dbContext.SaveChangesAsync();
    }
}
