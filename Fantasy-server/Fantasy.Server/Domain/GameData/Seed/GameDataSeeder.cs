using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Fantasy.Server.Domain.GameData.Entity;
using Fantasy.Server.Domain.GameData.Enum;
using Fantasy.Server.Domain.Player.Enum;
using Fantasy.Server.Global.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Fantasy.Server.Domain.GameData.Seed;

public static class GameDataSeeder
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public record JobBaseStatSeed(
        JobType JobType, long BaseHp, long BaseAtk, double CritRate,
        double CritDmgMultiplier, double HpPerLevel, double AtkPerLevel);

    public record LevelSeed(long Level, long RequiredExp, long RewardSp);

    public record StageSeed(
        long Stage, long MonsterHp, long MonsterAtk,
        long XpPerSecond, long GoldPerSecond, bool IsBossStage);

    public record SkillSeed(
        int SkillId, JobType JobType, bool IsActive, long SpCost,
        int? PrereqSkillId, SkillEffectType EffectType, double EffectValue);

    public record WeaponSeed(
        int WeaponId, string Name, WeaponGrade Grade, JobType JobType,
        long BaseAtk, long AtkPerEnhancement);

    public record AllSeeds(
        IReadOnlyList<JobBaseStatSeed> JobBaseStats,
        IReadOnlyList<LevelSeed> Levels,
        IReadOnlyList<StageSeed> Stages,
        IReadOnlyList<SkillSeed> Skills,
        IReadOnlyList<WeaponSeed> Weapons);

    public static IReadOnlyList<JobBaseStatSeed> LoadJobBaseStatSeeds() => Load<JobBaseStatSeed>("jobBaseStats.json");
    public static IReadOnlyList<LevelSeed> LoadLevelSeeds() => Load<LevelSeed>("levels.json");
    public static IReadOnlyList<StageSeed> LoadStageSeeds() => Load<StageSeed>("stages.json");
    public static IReadOnlyList<SkillSeed> LoadSkillSeeds() => Load<SkillSeed>("skills.json");
    public static IReadOnlyList<WeaponSeed> LoadWeaponSeeds() => Load<WeaponSeed>("weapons.json");

    public static AllSeeds LoadAllSeeds() => new(
        LoadJobBaseStatSeeds(),
        LoadLevelSeeds(),
        LoadStageSeeds(),
        LoadSkillSeeds(),
        LoadWeaponSeeds());

    public static async Task SeedAsync(AppDbContext db, ILogger? logger = null)
    {
        var seeds = LoadAllSeeds();

        SeedTable(db.JobBaseStats, seeds.JobBaseStats
            .Select(s => JobBaseStat.Create(s.JobType, s.BaseHp, s.BaseAtk, s.CritRate, s.CritDmgMultiplier, s.HpPerLevel, s.AtkPerLevel))
            .ToList(), await db.JobBaseStats.CountAsync(), "job_base_stat", logger);

        SeedTable(db.LevelTables, seeds.Levels
            .Select(s => LevelTable.Create(s.Level, s.RequiredExp, s.RewardSp))
            .ToList(), await db.LevelTables.CountAsync(), "level_table", logger);

        SeedTable(db.StageDatas, seeds.Stages
            .Select(s => StageData.Create(s.Stage, s.MonsterHp, s.MonsterAtk, s.XpPerSecond, s.GoldPerSecond, s.IsBossStage))
            .ToList(), await db.StageDatas.CountAsync(), "stage_data", logger);

        SeedTable(db.SkillDatas, seeds.Skills
            .Select(s => SkillData.Create(s.SkillId, s.JobType, s.IsActive, s.SpCost, s.PrereqSkillId, s.EffectType, s.EffectValue))
            .ToList(), await db.SkillDatas.CountAsync(), "skill_data", logger);

        SeedTable(db.WeaponDatas, seeds.Weapons
            .Select(s => WeaponData.Create(s.WeaponId, s.Name, s.Grade, s.JobType, s.BaseAtk, s.AtkPerEnhancement))
            .ToList(), await db.WeaponDatas.CountAsync(), "weapon_data", logger);

        await db.SaveChangesAsync();
    }

    private static void SeedTable<TEntity>(
        DbSet<TEntity> set,
        IReadOnlyList<TEntity> entities,
        int existingCount,
        string tableName,
        ILogger? logger) where TEntity : class
    {
        if (existingCount == 0)
        {
            set.AddRange(entities);
            return;
        }

        if (existingCount == entities.Count)
            return;

        logger?.LogWarning(
            "게임 데이터 '{Table}' 행 수가 일치하지 않습니다 (현재 {Existing}, 기대 {Expected}). " +
            "참조 데이터 손상 가능성이 있으나 자동 수정하지 않고 건너뜁니다. 운영자 수동 확인이 필요합니다.",
            tableName, existingCount, entities.Count);
    }

    private static IReadOnlyList<T> Load<T>(string fileName)
    {
        var assembly = typeof(GameDataSeeder).Assembly;
        var resourceName = assembly.GetManifestResourceNames()
            .Single(n => n.EndsWith($".{fileName}", StringComparison.Ordinal));

        using var stream = assembly.GetManifestResourceStream(resourceName)!;
        return JsonSerializer.Deserialize<List<T>>(stream, JsonOptions)
            ?? throw new InvalidOperationException($"Seed 데이터 '{fileName}' 역직렬화 결과가 null입니다.");
    }
}
