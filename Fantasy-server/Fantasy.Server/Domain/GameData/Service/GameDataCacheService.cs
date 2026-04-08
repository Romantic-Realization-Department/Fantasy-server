using System.Text.Json;
using Fantasy.Server.Domain.GameData.Entity;
using Fantasy.Server.Domain.GameData.Enum;
using Fantasy.Server.Domain.GameData.Repository.Interface;
using Fantasy.Server.Domain.GameData.Service.Interface;
using Fantasy.Server.Domain.Player.Enum;
using Microsoft.Extensions.Caching.Distributed;

namespace Fantasy.Server.Domain.GameData.Service;

public class GameDataCacheService : IGameDataCacheService
{
    private const string LevelTableKey = "game_data:level_table";
    private const string WeaponDataKey = "game_data:weapon_data";
    private const string SkillDataKey = "game_data:skill_data";
    private const string StageDataKey = "game_data:stage_data";
    private const string JobBaseStatKey = "game_data:job_base_stat";
    private static readonly TimeSpan CacheTtl = TimeSpan.FromHours(24);

    private readonly IGameDataRepository _repository;
    private readonly IDistributedCache _cache;

    public GameDataCacheService(IGameDataRepository repository, IDistributedCache cache)
    {
        _repository = repository;
        _cache = cache;
    }

    public async Task<Dictionary<long, LevelTable>> GetLevelTableAsync()
    {
        var json = await _cache.GetStringAsync(LevelTableKey);
        if (json is not null)
            return JsonSerializer.Deserialize<Dictionary<long, LevelTable>>(json)!;

        var data = await _repository.GetAllLevelTablesAsync();
        var dict = data.ToDictionary(l => l.Level);
        await _cache.SetStringAsync(LevelTableKey, JsonSerializer.Serialize(dict),
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = CacheTtl });
        return dict;
    }

    public async Task<List<WeaponData>> GetWeaponDataByGradeAsync(WeaponGrade grade)
    {
        var all = await GetAllWeaponDatasAsync();
        return all.Where(w => w.Grade == grade).ToList();
    }

    public async Task<List<WeaponData>> GetWeaponDataByJobAsync(JobType jobType)
    {
        var all = await GetAllWeaponDatasAsync();
        return all.Where(w => w.JobType == jobType).ToList();
    }

    public async Task<WeaponData?> GetWeaponDataAsync(int weaponId)
    {
        var all = await GetAllWeaponDatasAsync();
        return all.FirstOrDefault(w => w.WeaponId == weaponId);
    }

    public async Task<List<SkillData>> GetSkillDataByJobAsync(JobType jobType)
    {
        var all = await GetAllSkillDatasAsync();
        return all.Where(s => s.JobType == jobType).ToList();
    }

    public async Task<SkillData?> GetSkillDataAsync(int skillId)
    {
        var all = await GetAllSkillDatasAsync();
        return all.FirstOrDefault(s => s.SkillId == skillId);
    }

    public async Task<StageData?> GetStageDataAsync(long stage)
    {
        var all = await GetAllStageDatasAsync();
        return all.FirstOrDefault(s => s.Stage == stage);
    }

    public async Task<JobBaseStat?> GetJobBaseStatAsync(JobType jobType)
    {
        var all = await GetAllJobBaseStatsAsync();
        return all.FirstOrDefault(j => j.JobType == jobType);
    }

    private async Task<List<WeaponData>> GetAllWeaponDatasAsync()
    {
        var json = await _cache.GetStringAsync(WeaponDataKey);
        if (json is not null)
            return JsonSerializer.Deserialize<List<WeaponData>>(json)!;

        var data = await _repository.GetAllWeaponDatasAsync();
        await _cache.SetStringAsync(WeaponDataKey, JsonSerializer.Serialize(data),
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = CacheTtl });
        return data;
    }

    private async Task<List<SkillData>> GetAllSkillDatasAsync()
    {
        var json = await _cache.GetStringAsync(SkillDataKey);
        if (json is not null)
            return JsonSerializer.Deserialize<List<SkillData>>(json)!;

        var data = await _repository.GetAllSkillDatasAsync();
        await _cache.SetStringAsync(SkillDataKey, JsonSerializer.Serialize(data),
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = CacheTtl });
        return data;
    }

    private async Task<List<StageData>> GetAllStageDatasAsync()
    {
        var json = await _cache.GetStringAsync(StageDataKey);
        if (json is not null)
            return JsonSerializer.Deserialize<List<StageData>>(json)!;

        var data = await _repository.GetAllStageDatasAsync();
        await _cache.SetStringAsync(StageDataKey, JsonSerializer.Serialize(data),
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = CacheTtl });
        return data;
    }

    private async Task<List<JobBaseStat>> GetAllJobBaseStatsAsync()
    {
        var json = await _cache.GetStringAsync(JobBaseStatKey);
        if (json is not null)
            return JsonSerializer.Deserialize<List<JobBaseStat>>(json)!;

        var data = await _repository.GetAllJobBaseStatsAsync();
        await _cache.SetStringAsync(JobBaseStatKey, JsonSerializer.Serialize(data),
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = CacheTtl });
        return data;
    }
}
