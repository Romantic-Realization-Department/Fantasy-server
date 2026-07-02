using Fantasy.Server.Domain.GameData.Dto.Response;
using Fantasy.Server.Domain.GameData.Service.Interface;
using Fantasy.Server.Domain.Player.Enum;

namespace Fantasy.Server.Domain.GameData.Service;

public class GameDataQueryService : IGameDataQueryService
{
    private readonly IGameDataCacheService _cache;

    public GameDataQueryService(IGameDataCacheService cache)
    {
        _cache = cache;
    }

    public async Task<List<SkillDataResponse>> GetSkillsByJobAsync(JobType jobType)
    {
        var skills = await _cache.GetSkillDataByJobAsync(jobType);
        return skills.Select(s => new SkillDataResponse(
            s.SkillId,
            s.JobType.ToString(),
            s.IsActive,
            s.SpCost,
            s.PrereqSkillId,
            s.EffectType.ToString(),
            s.EffectValue
        )).ToList();
    }

    public async Task<List<WeaponDataResponse>> GetWeaponsByJobAsync(JobType jobType)
    {
        var weapons = await _cache.GetWeaponDataByJobAsync(jobType);
        return weapons.Select(w => new WeaponDataResponse(
            w.WeaponId,
            w.Name,
            w.Grade.ToString(),
            w.JobType.ToString(),
            w.BaseAtk,
            w.AtkPerEnhancement,
            w.MaxEnhancementLevel,
            w.MaxAwakeningLevel,
            w.SynthesizeRequiredCount,
            w.SynthesizeResultWeaponId
        )).ToList();
    }

    public async Task<List<LevelDataResponse>> GetLevelsAsync()
    {
        var table = await _cache.GetLevelTableAsync();
        return table.Values
            .OrderBy(l => l.Level)
            .Select(l => new LevelDataResponse(l.Level, l.RequiredExp, l.RewardSp))
            .ToList();
    }

    public async Task<List<StageDataResponse>> GetStagesAsync()
    {
        var stages = await _cache.GetAllStageDataAsync();
        return stages.Select(s => new StageDataResponse(
            s.Stage,
            s.MonsterHp,
            s.GoldPerSecond,
            s.XpPerSecond
        )).ToList();
    }
}
