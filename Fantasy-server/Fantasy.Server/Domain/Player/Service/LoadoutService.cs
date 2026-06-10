using Fantasy.Server.Domain.Dungeon.Dto.Response;
using Fantasy.Server.Domain.Dungeon.Service.Interface;
using Fantasy.Server.Domain.GameData.Service.Interface;
using Fantasy.Server.Domain.Player.Dto.Request;
using Fantasy.Server.Domain.Player.Dto.Response;
using Fantasy.Server.Domain.Player.Repository.Interface;
using Fantasy.Server.Domain.Player.Service.Interface;
using Fantasy.Server.Global.Infrastructure;
using Fantasy.Server.Global.Security.Provider;
using Gamism.SDK.Extensions.AspNetCore.Exceptions;
using PlayerEntity = Fantasy.Server.Domain.Player.Entity.Player;

namespace Fantasy.Server.Domain.Player.Service;

public class LoadoutService : ILoadoutService
{
    private readonly IPlayerRepository _playerRepository;
    private readonly IPlayerResourceRepository _playerResourceRepository;
    private readonly IPlayerStageRepository _playerStageRepository;
    private readonly IPlayerSessionRepository _playerSessionRepository;
    private readonly IPlayerWeaponRepository _playerWeaponRepository;
    private readonly IPlayerSkillRepository _playerSkillRepository;
    private readonly IPlayerRedisRepository _playerRedisRepository;
    private readonly IIdleRewardSettler _idleRewardSettler;
    private readonly IGameDataCacheService _gameDataCacheService;
    private readonly IAppDbTransactionRunner _transactionRunner;
    private readonly ICurrentUserProvider _currentUserProvider;

    public LoadoutService(
        IPlayerRepository playerRepository,
        IPlayerResourceRepository playerResourceRepository,
        IPlayerStageRepository playerStageRepository,
        IPlayerSessionRepository playerSessionRepository,
        IPlayerWeaponRepository playerWeaponRepository,
        IPlayerSkillRepository playerSkillRepository,
        IPlayerRedisRepository playerRedisRepository,
        IIdleRewardSettler idleRewardSettler,
        IGameDataCacheService gameDataCacheService,
        IAppDbTransactionRunner transactionRunner,
        ICurrentUserProvider currentUserProvider)
    {
        _playerRepository = playerRepository;
        _playerResourceRepository = playerResourceRepository;
        _playerStageRepository = playerStageRepository;
        _playerSessionRepository = playerSessionRepository;
        _playerWeaponRepository = playerWeaponRepository;
        _playerSkillRepository = playerSkillRepository;
        _playerRedisRepository = playerRedisRepository;
        _idleRewardSettler = idleRewardSettler;
        _gameDataCacheService = gameDataCacheService;
        _transactionRunner = transactionRunner;
        _currentUserProvider = currentUserProvider;
    }

    public async Task<LoadoutResponse> ExecuteAsync(LoadoutRequest request)
    {
        var accountId = _currentUserProvider.GetAccountId();

        PlayerEntity player = await _playerRepository.FindByAccountAsync(accountId)
            ?? throw new NotFoundException("플레이어 데이터를 찾을 수 없습니다.");

        var resource = await _playerResourceRepository.FindByPlayerIdAsync(player.Id)
            ?? throw new NotFoundException("플레이어 재화 데이터를 찾을 수 없습니다.");

        var stage = await _playerStageRepository.FindByPlayerIdAsync(player.Id)
            ?? throw new NotFoundException("플레이어 스테이지 데이터를 찾을 수 없습니다.");

        var session = await _playerSessionRepository.FindByPlayerIdAsync(player.Id)
            ?? throw new NotFoundException("플레이어 세션 데이터를 찾을 수 없습니다.");

        var weapons = await _playerWeaponRepository.FindAllByPlayerIdAsync(player.Id);
        var skills = await _playerSkillRepository.FindAllByPlayerIdAsync(player.Id);

        var reward = await _idleRewardSettler.SettleAsync(player, resource, stage, session, weapons, skills);

        if (request.WeaponId.HasValue && weapons.All(w => w.WeaponId != request.WeaponId.Value))
            throw new BadRequestException("보유하지 않은 무기입니다.");

        var skillIds = request.ActiveSkills.ToList();

        if (skillIds.Distinct().Count() != skillIds.Count)
            throw new BadRequestException("중복된 스킬 ID가 있습니다.");

        var jobSkills = await _gameDataCacheService.GetSkillDataByJobAsync(player.JobType);
        var jobSkillMap = jobSkills.ToDictionary(s => s.SkillId);
        var unlockedSkillIds = skills.Where(s => s.IsUnlocked).Select(s => s.SkillId).ToHashSet();

        foreach (int skillId in skillIds)
        {
            if (!jobSkillMap.TryGetValue(skillId, out var skillData))
                throw new BadRequestException("유효하지 않은 스킬입니다.");

            if (!skillData.IsActive)
                throw new BadRequestException("패시브 스킬은 장착할 수 없습니다.");

            if (!unlockedSkillIds.Contains(skillId))
                throw new BadRequestException("해금되지 않은 스킬입니다.");
        }

        session.Update(request.WeaponId ?? session.LastWeaponId, request.ActiveSkills);

        await _transactionRunner.ExecuteAsync(async () =>
        {
            await _playerRepository.UpdateAsync(player);
            await _playerResourceRepository.UpdateAsync(resource);
            await _playerStageRepository.UpdateAsync(stage);
            await _playerSessionRepository.UpdateAsync(session);
        });

        var playerResponse = PlayerDataResponseBuilder.Build(player, resource, stage, session, weapons, skills);
        await _playerRedisRepository.SetPlayerDataAsync(accountId, playerResponse);

        var changes = new ChangesDto(
            Gold: reward.EarnedGold,
            Exp: reward.EarnedXp,
            Sp: reward.LevelUps.Sum(l => l.EarnedSp),
            Mithril: 0,
            EnhancementScroll: 0,
            DungeonTickets: 0,
            LevelUps: reward.LevelUps.Select(l => l.NewLevel).ToList(),
            UnlockedSkillIds: [],
            AcquiredWeaponIds: [],
            MaxStage: reward.NewMaxStage
        );

        return new LoadoutResponse(changes, playerResponse);
    }
}
