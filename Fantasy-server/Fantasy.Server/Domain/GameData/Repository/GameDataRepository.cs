using Fantasy.Server.Domain.GameData.Entity;
using Fantasy.Server.Domain.GameData.Repository.Interface;
using Fantasy.Server.Global.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Fantasy.Server.Domain.GameData.Repository;

public class GameDataRepository : IGameDataRepository
{
    private readonly AppDbContext _db;

    public GameDataRepository(AppDbContext db) => _db = db;

    public async Task<List<LevelTable>> GetAllLevelTablesAsync()
        => await _db.LevelTables.AsNoTracking().OrderBy(l => l.Level).ToListAsync();

    public async Task<List<WeaponData>> GetAllWeaponDatasAsync()
        => await _db.WeaponDatas.AsNoTracking().ToListAsync();

    public async Task<List<SkillData>> GetAllSkillDatasAsync()
        => await _db.SkillDatas.AsNoTracking().ToListAsync();

    public async Task<List<StageData>> GetAllStageDatasAsync()
        => await _db.StageDatas.AsNoTracking().OrderBy(s => s.Stage).ToListAsync();

    public async Task<List<JobBaseStat>> GetAllJobBaseStatsAsync()
        => await _db.JobBaseStats.AsNoTracking().ToListAsync();
}
