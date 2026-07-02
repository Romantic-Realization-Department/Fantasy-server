using Fantasy.Server.Domain.Dungeon.Dto.Response;
using Fantasy.Server.Domain.GameData.Service.Interface;
using Fantasy.Server.Domain.Player.Constant;
using Fantasy.Server.Domain.Player.Dto.Response;
using Fantasy.Server.Domain.Player.Entity;
using Fantasy.Server.Domain.Player.Repository.Interface;
using Fantasy.Server.Domain.Weapon.Dto.Response;
using Fantasy.Server.Domain.Weapon.Service.Interface;
using Fantasy.Server.Global.Infrastructure;
using Fantasy.Server.Global.Security.Provider;
using Gamism.SDK.Extensions.AspNetCore.Exceptions;

namespace Fantasy.Server.Domain.Weapon.Service;

public class SynthesizeWeaponService : ISynthesizeWeaponService
{
    private readonly IPlayerRepository _playerRepository;
    private readonly IPlayerResourceRepository _playerResourceRepository;
    private readonly IPlayerStageRepository _playerStageRepository;
    private readonly IPlayerSessionRepository _playerSessionRepository;
    private readonly IPlayerWeaponRepository _playerWeaponRepository;
    private readonly IPlayerSkillRepository _playerSkillRepository;
    private readonly IPlayerRedisRepository _playerRedisRepository;
    private readonly IRewardTransactionRepository _rewardTransactionRepository;
    private readonly IGameDataCacheService _gameDataCacheService;
    private readonly IAppDbTransactionRunner _transactionRunner;
    private readonly ICurrentUserProvider _currentUserProvider;

    public SynthesizeWeaponService(
        IPlayerRepository playerRepository,
        IPlayerResourceRepository playerResourceRepository,
        IPlayerStageRepository playerStageRepository,
        IPlayerSessionRepository playerSessionRepository,
        IPlayerWeaponRepository playerWeaponRepository,
        IPlayerSkillRepository playerSkillRepository,
        IPlayerRedisRepository playerRedisRepository,
        IRewardTransactionRepository rewardTransactionRepository,
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
        _rewardTransactionRepository = rewardTransactionRepository;
        _gameDataCacheService = gameDataCacheService;
        _transactionRunner = transactionRunner;
        _currentUserProvider = currentUserProvider;
    }

    public async Task<WeaponSynthesizeResponse> ExecuteAsync(int weaponId)
    {
        var accountId = _currentUserProvider.GetAccountId();

        var player = await _playerRepository.FindByAccountAsync(accountId)
            ?? throw new NotFoundException("플레이어 데이터를 찾을 수 없습니다.");

        var weaponData = await _gameDataCacheService.GetWeaponDataAsync(weaponId)
            ?? throw new NotFoundException("무기 데이터를 찾을 수 없습니다.");

        if (weaponData.SynthesizeRequiredCount is null || weaponData.SynthesizeResultWeaponId is null)
            throw new BadRequestException("합성할 수 없는 무기입니다.");

        var requiredCount = weaponData.SynthesizeRequiredCount.Value;
        var resultWeaponId = weaponData.SynthesizeResultWeaponId.Value;

        var material = await _playerWeaponRepository.FindByPlayerIdAndWeaponIdAsync(player.Id, weaponId);
        if (material is null || material.Count < 1)
            throw new NotFoundException("보유하지 않은 무기입니다.");

        if (material.Count < requiredCount)
            throw new BadRequestException("합성 재료가 부족합니다.");

        material.ConsumeCount(requiredCount);

        var result = await _playerWeaponRepository.FindByPlayerIdAndWeaponIdAsync(player.Id, resultWeaponId);
        var isNewResult = result is null;
        if (result is null)
            result = PlayerWeapon.Create(player.Id, resultWeaponId, 1, 0, 0);
        else
            result.AddCount(1);

        var rewardTransactions = new List<RewardTransaction>();
        if (requiredCount > 0)
            rewardTransactions.Add(RewardTransaction.Create(
                player.Id, RewardSourceTypes.WeaponSynthesize, null, RewardTypes.Weapon, weaponId.ToString(), -requiredCount));
        rewardTransactions.Add(RewardTransaction.Create(
            player.Id, RewardSourceTypes.WeaponSynthesize, null, RewardTypes.Weapon, resultWeaponId.ToString(), 1));

        await _transactionRunner.ExecuteAsync(async () =>
        {
            await _playerWeaponRepository.UpdateAsync(material);

            if (isNewResult)
                await _playerWeaponRepository.SaveAsync(result);
            else
                await _playerWeaponRepository.UpdateAsync(result);

            await _rewardTransactionRepository.SaveRangeAsync(rewardTransactions);
        });

        await _playerRedisRepository.DeleteAsync(accountId);

        var resource = await _playerResourceRepository.FindByPlayerIdAsync(player.Id)
            ?? throw new NotFoundException("플레이어 재화 데이터를 찾을 수 없습니다.");

        var stage = await _playerStageRepository.FindByPlayerIdAsync(player.Id)
            ?? throw new NotFoundException("플레이어 스테이지 데이터를 찾을 수 없습니다.");

        var session = await _playerSessionRepository.FindByPlayerIdAsync(player.Id)
            ?? throw new NotFoundException("플레이어 세션 데이터를 찾을 수 없습니다.");

        var weapons = await _playerWeaponRepository.FindAllByPlayerIdAsync(player.Id);
        var skills = await _playerSkillRepository.FindAllByPlayerIdAsync(player.Id);

        var playerResponse = PlayerDataResponseBuilder.Build(player, resource, stage, session, weapons, skills);

        var changes = new ChangesDto(
            Gold: 0,
            Exp: 0,
            Sp: 0,
            Mithril: 0,
            EnhancementScroll: 0,
            DungeonTickets: 0,
            LevelUps: [],
            UnlockedSkillIds: [],
            AcquiredWeaponIds: [resultWeaponId],
            MaxStage: 0
        );

        return new WeaponSynthesizeResponse(weaponId, resultWeaponId, changes, playerResponse);
    }
}
