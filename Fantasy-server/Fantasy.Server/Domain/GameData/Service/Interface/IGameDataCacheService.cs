using Fantasy.Server.Domain.GameData.Entity;
using Fantasy.Server.Domain.GameData.Enum;
using Fantasy.Server.Domain.Player.Enum;

namespace Fantasy.Server.Domain.GameData.Service.Interface;

public interface IGameDataCacheService
{
    Task<Dictionary<long, LevelTable>> GetLevelTableAsync();
    Task<List<WeaponData>> GetWeaponDataByGradeAsync(WeaponGrade grade);
    Task<List<WeaponData>> GetWeaponDataByJobAsync(JobType jobType);
    Task<WeaponData?> GetWeaponDataAsync(int weaponId);
    Task<List<SkillData>> GetSkillDataByJobAsync(JobType jobType);
    Task<SkillData?> GetSkillDataAsync(int skillId);
    Task<StageData?> GetStageDataAsync(long stage);
    Task<JobBaseStat?> GetJobBaseStatAsync(JobType jobType);
}
