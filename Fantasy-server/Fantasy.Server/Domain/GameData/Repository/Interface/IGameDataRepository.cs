using Fantasy.Server.Domain.GameData.Entity;

namespace Fantasy.Server.Domain.GameData.Repository.Interface;

public interface IGameDataRepository
{
    Task<List<LevelTable>> GetAllLevelTablesAsync();
    Task<List<WeaponData>> GetAllWeaponDatasAsync();
    Task<List<SkillData>> GetAllSkillDatasAsync();
    Task<List<StageData>> GetAllStageDatasAsync();
    Task<List<JobBaseStat>> GetAllJobBaseStatsAsync();
}
