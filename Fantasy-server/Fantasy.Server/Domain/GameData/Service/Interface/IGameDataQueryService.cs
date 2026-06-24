using Fantasy.Server.Domain.GameData.Dto.Response;
using Fantasy.Server.Domain.Player.Enum;

namespace Fantasy.Server.Domain.GameData.Service.Interface;

public interface IGameDataQueryService
{
    Task<List<SkillDataResponse>> GetSkillsByJobAsync(JobType jobType);
    Task<List<WeaponDataResponse>> GetWeaponsByJobAsync(JobType jobType);
    Task<List<LevelDataResponse>> GetLevelsAsync();
    Task<List<StageDataResponse>> GetStagesAsync();
}
