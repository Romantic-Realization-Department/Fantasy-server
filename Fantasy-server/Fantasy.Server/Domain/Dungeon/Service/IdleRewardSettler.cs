using Fantasy.Server.Domain.Dungeon.Service.Interface;
using Fantasy.Server.Domain.GameData.Entity;
using Fantasy.Server.Domain.GameData.Service.Interface;
using Fantasy.Server.Domain.LevelUp.Dto.Response;
using Fantasy.Server.Domain.LevelUp.Service.Interface;
using Fantasy.Server.Domain.Player.Entity;
using Gamism.SDK.Extensions.AspNetCore.Exceptions;
using PlayerEntity = Fantasy.Server.Domain.Player.Entity.Player;

namespace Fantasy.Server.Domain.Dungeon.Service;

public class IdleRewardSettler : IIdleRewardSettler
{
    private const long MaxOfflineSeconds = 8 * 60 * 60;

    private readonly IGameDataCacheService _gameDataCacheService;
    private readonly ILevelUpService _levelUpService;
    private readonly ICombatStatCalculator _calculator;

    public IdleRewardSettler(
        IGameDataCacheService gameDataCacheService,
        ILevelUpService levelUpService,
        ICombatStatCalculator calculator)
    {
        _gameDataCacheService = gameDataCacheService;
        _levelUpService = levelUpService;
        _calculator = calculator;
    }

    public async Task<IdleRewardResult> SettleAsync(
        PlayerEntity player,
        PlayerResource resource,
        PlayerStage stage,
        PlayerSession session,
        List<PlayerWeapon> weapons,
        List<PlayerSkill> skills)
    {
        var elapsedSeconds = Math.Min(
            (long)(DateTime.UtcNow - stage.LastCalculatedAt).TotalSeconds,
            MaxOfflineSeconds);

        if (elapsedSeconds <= 0)
            return new IdleRewardResult(0, 0, stage.MaxStage, []);

        var stageData = await _gameDataCacheService.GetStageDataAsync(stage.MaxStage)
            ?? throw new NotFoundException("스테이지 데이터를 찾을 수 없습니다.");

        var jobStat = await _gameDataCacheService.GetJobBaseStatAsync(player.JobType)
            ?? throw new NotFoundException("직업 기본 스탯 데이터를 찾을 수 없습니다.");

        WeaponData? weaponData = null;
        long weaponEnhancement = 0;
        if (session.LastWeaponId.HasValue)
        {
            weaponData = await _gameDataCacheService.GetWeaponDataAsync(session.LastWeaponId.Value);
            var equippedWeapon = weapons.FirstOrDefault(w => w.WeaponId == session.LastWeaponId.Value);
            weaponEnhancement = equippedWeapon?.EnhancementLevel ?? 0;
        }

        var jobSkillData = await _gameDataCacheService.GetSkillDataByJobAsync(player.JobType);
        var unlockedPassiveSkills = skills
            .Where(s => s.IsUnlocked)
            .Select(s => jobSkillData.FirstOrDefault(sd => sd.SkillId == s.SkillId))
            .Where(sd => sd is not null && !sd.IsActive)
            .Select(sd => (Skill: sd!, IsPassive: true));

        var combatStat = _calculator.Calculate(player.Level, jobStat, weaponData, weaponEnhancement, unlockedPassiveSkills);
        var dps = _calculator.CalculateDps(combatStat);

        var (earnedGold, earnedXp, newMaxStage) = SimulateDungeon(dps, elapsedSeconds, stage.MaxStage, stageData);

        var now = DateTime.UtcNow;
        var levelUps = await _levelUpService.ExecuteAsync(player, resource, earnedXp);
        resource.UpdateGold(resource.Gold + earnedGold);
        stage.Update(newMaxStage);
        stage.UpdateLastCalculatedAt(now);

        return new IdleRewardResult(earnedGold, earnedXp, newMaxStage, levelUps);
    }

    private static (long Gold, long Xp, long NewMaxStage) SimulateDungeon(
        double dps, long elapsedSeconds, long currentMaxStage, StageData stageData)
    {
        var timeToKill = stageData.MonsterHp / Math.Max(dps, 1.0);
        var newMaxStage = timeToKill <= elapsedSeconds
            ? Math.Max(currentMaxStage, currentMaxStage + 1)
            : currentMaxStage;

        return (
            stageData.GoldPerSecond * elapsedSeconds,
            stageData.XpPerSecond * elapsedSeconds,
            newMaxStage
        );
    }
}
