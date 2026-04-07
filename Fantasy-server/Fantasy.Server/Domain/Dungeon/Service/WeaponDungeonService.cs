using Fantasy.Server.Domain.Dungeon.Dto.Response;
using Fantasy.Server.Domain.Dungeon.Service.Interface;
using Fantasy.Server.Domain.GameData.Entity;
using Fantasy.Server.Domain.GameData.Enum;
using Fantasy.Server.Domain.GameData.Service.Interface;
using Fantasy.Server.Domain.Player.Dto.Request;
using Fantasy.Server.Domain.Player.Enum;
using Fantasy.Server.Domain.Player.Repository.Interface;
using Fantasy.Server.Global.Security.Provider;
using Gamism.SDK.Extensions.AspNetCore.Exceptions;

namespace Fantasy.Server.Domain.Dungeon.Service;

public class WeaponDungeonService : IWeaponDungeonService
{
    private const int WeaponDungeonStage = 1; // 무기 던전 고정 스테이지 기준
    private const int BGradeDropRatePercent = 20;
    private const int CGradeDropRatePercent = 70;
    private const int ScrollDropRatePercent = 30;

    private readonly IPlayerRepository _playerRepository;
    private readonly IPlayerResourceRepository _playerResourceRepository;
    private readonly IPlayerStageRepository _playerStageRepository;
    private readonly IPlayerSessionRepository _playerSessionRepository;
    private readonly IPlayerWeaponRepository _playerWeaponRepository;
    private readonly IPlayerSkillRepository _playerSkillRepository;
    private readonly IPlayerRedisRepository _playerRedisRepository;
    private readonly IGameDataCacheService _gameDataCacheService;
    private readonly ICurrentUserProvider _currentUserProvider;

    public WeaponDungeonService(
        IPlayerRepository playerRepository,
        IPlayerResourceRepository playerResourceRepository,
        IPlayerStageRepository playerStageRepository,
        IPlayerSessionRepository playerSessionRepository,
        IPlayerWeaponRepository playerWeaponRepository,
        IPlayerSkillRepository playerSkillRepository,
        IPlayerRedisRepository playerRedisRepository,
        IGameDataCacheService gameDataCacheService,
        ICurrentUserProvider currentUserProvider)
    {
        _playerRepository = playerRepository;
        _playerResourceRepository = playerResourceRepository;
        _playerStageRepository = playerStageRepository;
        _playerSessionRepository = playerSessionRepository;
        _playerWeaponRepository = playerWeaponRepository;
        _playerSkillRepository = playerSkillRepository;
        _playerRedisRepository = playerRedisRepository;
        _gameDataCacheService = gameDataCacheService;
        _currentUserProvider = currentUserProvider;
    }

    public async Task<WeaponDungeonResponse> ExecuteAsync(JobType jobType)
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

        var calculator = new CombatStatCalculator();
        var combatStat = calculator.Calculate(player.Level, jobStat, weaponData, weaponEnhancement, unlockedPassiveSkills);

        // 던전 몬스터 스탯: 현재 MaxStage 기준
        var stageData = await _gameDataCacheService.GetStageDataAsync(stage.MaxStage);
        if (stageData is null)
            throw new NotFoundException("스테이지 데이터를 찾을 수 없습니다.");

        var dps = calculator.CalculateDps(combatStat);
        var cleared = dps > stageData.MonsterHp;

        var droppedWeapons = new List<DroppedWeaponInfo>();
        long droppedScrolls = 0;

        if (cleared)
        {
            // B등급 드랍 시도
            if (Random.Shared.Next(0, 100) < BGradeDropRatePercent)
            {
                var bWeapons = await _gameDataCacheService.GetWeaponDataByGradeAsync(WeaponGrade.B);
                var bJobWeapons = bWeapons.Where(w => w.JobType == jobType).ToList();
                if (bJobWeapons.Count > 0)
                {
                    var dropped = bJobWeapons[Random.Shared.Next(bJobWeapons.Count)];
                    droppedWeapons.Add(new DroppedWeaponInfo(dropped.WeaponId, dropped.Name, dropped.Grade));
                }
            }
            // C등급 드랍 시도
            else if (Random.Shared.Next(0, 100) < CGradeDropRatePercent)
            {
                var cWeapons = await _gameDataCacheService.GetWeaponDataByGradeAsync(WeaponGrade.C);
                var cJobWeapons = cWeapons.Where(w => w.JobType == jobType).ToList();
                if (cJobWeapons.Count > 0)
                {
                    var dropped = cJobWeapons[Random.Shared.Next(cJobWeapons.Count)];
                    droppedWeapons.Add(new DroppedWeaponInfo(dropped.WeaponId, dropped.Name, dropped.Grade));
                }
            }

            // 스크롤 드랍 시도
            if (Random.Shared.Next(0, 100) < ScrollDropRatePercent)
                droppedScrolls = 1;
        }

        if (droppedWeapons.Count > 0 || droppedScrolls > 0)
        {
            var weaponChanges = droppedWeapons
                .Select(w =>
                {
                    var existing = weapons.FirstOrDefault(pw => pw.WeaponId == w.WeaponId);
                    return new WeaponChangeItem(w.WeaponId, (existing?.Count ?? 0) + 1,
                        existing?.EnhancementLevel ?? 0, existing?.AwakeningCount ?? 0);
                })
                .ToList();

            if (weaponChanges.Count > 0)
                await _playerWeaponRepository.UpsertRangeAsync(player.Id, weaponChanges);

            if (droppedScrolls > 0)
            {
                resource.UpdateChangeData(resource.EnhancementScroll + droppedScrolls, null, null);
                await _playerResourceRepository.UpdateAsync(resource);
            }

            await _playerRedisRepository.DeleteAsync(accountId, jobType);
        }

        return new WeaponDungeonResponse(cleared, droppedWeapons, droppedScrolls);
    }
}
