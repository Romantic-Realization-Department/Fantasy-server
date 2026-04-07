using Fantasy.Server.Domain.Dungeon.Dto.Response;
using Fantasy.Server.Domain.Dungeon.Service.Interface;
using Fantasy.Server.Domain.GameData.Entity;
using Fantasy.Server.Domain.GameData.Service.Interface;
using Fantasy.Server.Domain.LevelUp.Service.Interface;
using Fantasy.Server.Domain.Player.Entity;
using Fantasy.Server.Domain.Player.Enum;
using Fantasy.Server.Domain.Player.Repository.Interface;
using Fantasy.Server.Global.Infrastructure;
using Fantasy.Server.Global.Security.Provider;
using Gamism.SDK.Extensions.AspNetCore.Exceptions;

namespace Fantasy.Server.Domain.Dungeon.Service;

public class BasicDungeonClaimService : IBasicDungeonClaimService
{
    private const long MaxOfflineSeconds = 8 * 60 * 60; // 8시간 상한

    private readonly IPlayerRepository _playerRepository;
    private readonly IPlayerResourceRepository _playerResourceRepository;
    private readonly IPlayerStageRepository _playerStageRepository;
    private readonly IPlayerSessionRepository _playerSessionRepository;
    private readonly IPlayerWeaponRepository _playerWeaponRepository;
    private readonly IPlayerSkillRepository _playerSkillRepository;
    private readonly IPlayerRedisRepository _playerRedisRepository;
    private readonly IGameDataCacheService _gameDataCacheService;
    private readonly ILevelUpService _levelUpService;
    private readonly IAppDbTransactionRunner _transactionRunner;
    private readonly ICurrentUserProvider _currentUserProvider;
    private readonly CombatStatCalculator _calculator;

    public BasicDungeonClaimService(
        IPlayerRepository playerRepository,
        IPlayerResourceRepository playerResourceRepository,
        IPlayerStageRepository playerStageRepository,
        IPlayerSessionRepository playerSessionRepository,
        IPlayerWeaponRepository playerWeaponRepository,
        IPlayerSkillRepository playerSkillRepository,
        IPlayerRedisRepository playerRedisRepository,
        IGameDataCacheService gameDataCacheService,
        ILevelUpService levelUpService,
        IAppDbTransactionRunner transactionRunner,
        ICurrentUserProvider currentUserProvider,
        CombatStatCalculator calculator)
    {
        _playerRepository = playerRepository;
        _playerResourceRepository = playerResourceRepository;
        _playerStageRepository = playerStageRepository;
        _playerSessionRepository = playerSessionRepository;
        _playerWeaponRepository = playerWeaponRepository;
        _playerSkillRepository = playerSkillRepository;
        _playerRedisRepository = playerRedisRepository;
        _gameDataCacheService = gameDataCacheService;
        _levelUpService = levelUpService;
        _transactionRunner = transactionRunner;
        _currentUserProvider = currentUserProvider;
        _calculator = calculator;
    }

    public async Task<BasicDungeonClaimResponse> ExecuteAsync(JobType jobType)
    {
        var accountId = _currentUserProvider.GetAccountId();

        var player = await _playerRepository.FindByAccountAndJobAsync(accountId, jobType)
            ?? throw new NotFoundException("플레이어 데이터를 찾을 수 없습니다.");

        var resource = await _playerResourceRepository.FindByPlayerIdAsync(player.Id)
            ?? throw new NotFoundException("플레이어 재화 데이터를 찾을 수 없습니다.");

        var stage = await _playerStageRepository.FindByPlayerIdAsync(player.Id)
            ?? throw new NotFoundException("플레이어 스테이지 데이터를 찾을 수 없습니다.");

        var session = await _playerSessionRepository.FindByPlayerIdAsync(player.Id)
            ?? throw new NotFoundException("플레이어 세션 데이터를 찾을 수 없습니다.");

        var weapons = await _playerWeaponRepository.FindAllByPlayerIdAsync(player.Id);
        var skills = await _playerSkillRepository.FindAllByPlayerIdAsync(player.Id);

        var elapsedSeconds = Math.Min(
            (long)(DateTime.UtcNow - stage.LastCalculatedAt).TotalSeconds,
            MaxOfflineSeconds);

        if (elapsedSeconds <= 0)
            return new BasicDungeonClaimResponse(0, 0, stage.MaxStage, player.Level, []);

        var stageData = await _gameDataCacheService.GetStageDataAsync(stage.MaxStage);
        if (stageData is null)
            throw new NotFoundException("스테이지 데이터를 찾을 수 없습니다.");

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

        var (earnedGold, earnedXp, newMaxStage) = SimulateDungeon(
            dps, combatStat.Hp, elapsedSeconds, stage.MaxStage, stageData);

        var levelUps = await _levelUpService.ApplyExpAsync(player, resource, earnedXp);
        resource.UpdateGold(resource.Gold + earnedGold);
        stage.Update(newMaxStage);
        stage.UpdateLastCalculatedAt();

        await _transactionRunner.ExecuteAsync(async () =>
        {
            await _playerRepository.UpdateAsync(player);
            await _playerResourceRepository.UpdateAsync(resource);
            await _playerStageRepository.UpdateAsync(stage);
        });

        await _playerRedisRepository.DeleteAsync(accountId, jobType);

        return new BasicDungeonClaimResponse(earnedGold, earnedXp, newMaxStage, player.Level, levelUps);
    }

    private static (long Gold, long Xp, long NewMaxStage) SimulateDungeon(
        double dps, long hp, long elapsedSeconds, long currentMaxStage, StageData stageData)
    {
        long earnedGold = 0;
        long earnedXp = 0;
        long newMaxStage = currentMaxStage;
        long remainingSeconds = elapsedSeconds;

        // 현재 스테이지 클리어 가능 여부 확인
        var timeToKill = stageData.MonsterHp / Math.Max(dps, 1.0);
        if (timeToKill <= remainingSeconds)
        {
            // 클리어 가능: 경과 시간 동안 보상 적립
            earnedGold += stageData.GoldPerSecond * remainingSeconds;
            earnedXp += stageData.XpPerSecond * remainingSeconds;
            newMaxStage = Math.Max(newMaxStage, currentMaxStage + 1);
        }
        else
        {
            // 클리어 불가: 경과 시간만큼 보상만
            earnedGold += stageData.GoldPerSecond * remainingSeconds;
            earnedXp += stageData.XpPerSecond * remainingSeconds;
        }

        return (earnedGold, earnedXp, newMaxStage);
    }
}
