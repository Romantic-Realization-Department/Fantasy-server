using Fantasy.Server.Domain.Dungeon.Dto.Response;
using Fantasy.Server.Domain.Dungeon.Service.Interface;
using Fantasy.Server.Domain.GameData.Entity;
using Fantasy.Server.Domain.GameData.Service.Interface;
using Fantasy.Server.Domain.Player.Repository.Interface;
using Fantasy.Server.Global.Security.Provider;
using Gamism.SDK.Extensions.AspNetCore.Exceptions;

namespace Fantasy.Server.Domain.Dungeon.Service;

public class BasicDungeonStateService : IBasicDungeonStateService
{
    private const int MaxOfflineSeconds = 8 * 60 * 60;

    private readonly IPlayerRepository _playerRepository;
    private readonly IPlayerStageRepository _playerStageRepository;
    private readonly IPlayerSessionRepository _playerSessionRepository;
    private readonly IPlayerWeaponRepository _playerWeaponRepository;
    private readonly IPlayerSkillRepository _playerSkillRepository;
    private readonly IGameDataCacheService _gameDataCacheService;
    private readonly ICombatStatCalculator _calculator;
    private readonly ICurrentUserProvider _currentUserProvider;

    public BasicDungeonStateService(
        IPlayerRepository playerRepository,
        IPlayerStageRepository playerStageRepository,
        IPlayerSessionRepository playerSessionRepository,
        IPlayerWeaponRepository playerWeaponRepository,
        IPlayerSkillRepository playerSkillRepository,
        IGameDataCacheService gameDataCacheService,
        ICombatStatCalculator calculator,
        ICurrentUserProvider currentUserProvider)
    {
        _playerRepository = playerRepository;
        _playerStageRepository = playerStageRepository;
        _playerSessionRepository = playerSessionRepository;
        _playerWeaponRepository = playerWeaponRepository;
        _playerSkillRepository = playerSkillRepository;
        _gameDataCacheService = gameDataCacheService;
        _calculator = calculator;
        _currentUserProvider = currentUserProvider;
    }

    public async Task<BasicDungeonStateResponse> ExecuteAsync()
    {
        var accountId = _currentUserProvider.GetAccountId();

        var player = await _playerRepository.FindByAccountAsync(accountId)
            ?? throw new NotFoundException("플레이어 데이터를 찾을 수 없습니다.");

        var stage = await _playerStageRepository.FindByPlayerIdAsync(player.Id)
            ?? throw new NotFoundException("플레이어 스테이지 데이터를 찾을 수 없습니다.");

        var session = await _playerSessionRepository.FindByPlayerIdAsync(player.Id)
            ?? throw new NotFoundException("플레이어 세션 데이터를 찾을 수 없습니다.");

        var weapons = await _playerWeaponRepository.FindAllByPlayerIdAsync(player.Id);
        var skills = await _playerSkillRepository.FindAllByPlayerIdAsync(player.Id);

        var stageData = await _gameDataCacheService.GetStageDataAsync(stage.MaxStage)
            ?? throw new NotFoundException("스테이지 데이터를 찾을 수 없습니다.");

        var jobStat = await _gameDataCacheService.GetJobBaseStatAsync(player.JobType)
            ?? throw new NotFoundException("직업 기본 스탯 데이터를 찾을 수 없습니다.");

        WeaponData? weaponData = null;
        long weaponEnhancement = 0;
        if (session.LastWeaponId.HasValue)
        {
            weaponData = await _gameDataCacheService.GetWeaponDataAsync(session.LastWeaponId.Value);
            weaponEnhancement = weapons.FirstOrDefault(w => w.WeaponId == session.LastWeaponId.Value)?.EnhancementLevel ?? 0;
        }

        var jobSkillData = await _gameDataCacheService.GetSkillDataByJobAsync(player.JobType);
        var unlockedPassiveSkills = skills
            .Where(s => s.IsUnlocked)
            .Select(s => jobSkillData.FirstOrDefault(sd => sd.SkillId == s.SkillId))
            .Where(sd => sd is not null && !sd.IsActive)
            .Select(sd => (Skill: sd!, IsPassive: true));

        var combatStat = _calculator.Calculate(player.Level, jobStat, weaponData, weaponEnhancement, unlockedPassiveSkills);
        var dps = _calculator.CalculateDps(combatStat);

        return new BasicDungeonStateResponse(
            Stage: stage.MaxStage,
            LastCalculatedAt: stage.LastCalculatedAt,
            ServerNow: DateTime.UtcNow,
            MaxOfflineSeconds: MaxOfflineSeconds,
            CombatPower: dps,
            GoldPerSecond: stageData.GoldPerSecond,
            XpPerSecond: stageData.XpPerSecond
        );
    }
}
