using Fantasy.Server.Domain.Dungeon.Dto.Response;
using Fantasy.Server.Domain.GameData.Service.Interface;
using Fantasy.Server.Domain.Player.Dto.Response;
using Fantasy.Server.Domain.Player.Repository.Interface;
using Fantasy.Server.Domain.Weapon.Dto.Response;
using Fantasy.Server.Domain.Weapon.Service.Interface;
using Fantasy.Server.Global.Infrastructure;
using Fantasy.Server.Global.Security.Provider;
using Gamism.SDK.Extensions.AspNetCore.Exceptions;

namespace Fantasy.Server.Domain.Weapon.Service;

public class UpgradeWeaponService : IUpgradeWeaponService
{
    private readonly IPlayerRepository _playerRepository;
    private readonly IPlayerResourceRepository _playerResourceRepository;
    private readonly IPlayerStageRepository _playerStageRepository;
    private readonly IPlayerSessionRepository _playerSessionRepository;
    private readonly IPlayerWeaponRepository _playerWeaponRepository;
    private readonly IPlayerSkillRepository _playerSkillRepository;
    private readonly IPlayerRedisRepository _playerRedisRepository;
    private readonly IGameDataCacheService _gameDataCacheService;
    private readonly IAppDbTransactionRunner _transactionRunner;
    private readonly ICurrentUserProvider _currentUserProvider;

    public UpgradeWeaponService(
        IPlayerRepository playerRepository,
        IPlayerResourceRepository playerResourceRepository,
        IPlayerStageRepository playerStageRepository,
        IPlayerSessionRepository playerSessionRepository,
        IPlayerWeaponRepository playerWeaponRepository,
        IPlayerSkillRepository playerSkillRepository,
        IPlayerRedisRepository playerRedisRepository,
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
        _gameDataCacheService = gameDataCacheService;
        _transactionRunner = transactionRunner;
        _currentUserProvider = currentUserProvider;
    }

    public async Task<WeaponUpgradeResponse> ExecuteAsync(int weaponId)
    {
        var accountId = _currentUserProvider.GetAccountId();

        var player = await _playerRepository.FindByAccountAsync(accountId)
            ?? throw new NotFoundException("플레이어 데이터를 찾을 수 없습니다.");

        var weaponData = await _gameDataCacheService.GetWeaponDataAsync(weaponId)
            ?? throw new NotFoundException("무기 데이터를 찾을 수 없습니다.");

        var playerWeapon = await _playerWeaponRepository.FindByPlayerIdAndWeaponIdAsync(player.Id, weaponId);
        if (playerWeapon is null || playerWeapon.Count < 1)
            throw new NotFoundException("보유하지 않은 무기입니다.");

        if (playerWeapon.EnhancementLevel >= weaponData.MaxEnhancementLevel)
            throw new BadRequestException("이미 최대 강화 레벨입니다.");

        var cost = await _gameDataCacheService.GetWeaponEnhancementCostAsync(weaponId, playerWeapon.EnhancementLevel)
            ?? throw new NotFoundException("강화 비용 데이터를 찾을 수 없습니다.");

        var resource = await _playerResourceRepository.FindByPlayerIdAsync(player.Id)
            ?? throw new NotFoundException("플레이어 재화 데이터를 찾을 수 없습니다.");

        if (resource.Gold < cost.RequiredGold || resource.EnhancementScroll < cost.RequiredScroll)
            throw new BadRequestException("재화가 부족합니다.");

        resource.UpdateGold(resource.Gold - cost.RequiredGold);
        resource.UpdateChangeData(resource.EnhancementScroll - cost.RequiredScroll, null, null);
        playerWeapon.Enhance();

        await _transactionRunner.ExecuteAsync(async () =>
        {
            await _playerResourceRepository.UpdateAsync(resource);
            await _playerWeaponRepository.UpdateAsync(playerWeapon);
        });

        await _playerRedisRepository.DeleteAsync(accountId);

        var stage = await _playerStageRepository.FindByPlayerIdAsync(player.Id)
            ?? throw new NotFoundException("플레이어 스테이지 데이터를 찾을 수 없습니다.");

        var session = await _playerSessionRepository.FindByPlayerIdAsync(player.Id)
            ?? throw new NotFoundException("플레이어 세션 데이터를 찾을 수 없습니다.");

        var weapons = await _playerWeaponRepository.FindAllByPlayerIdAsync(player.Id);
        var skills = await _playerSkillRepository.FindAllByPlayerIdAsync(player.Id);

        var playerResponse = PlayerDataResponseBuilder.Build(player, resource, stage, session, weapons, skills);

        var changes = new ChangesDto(
            Gold: -cost.RequiredGold,
            Exp: 0,
            Sp: 0,
            Mithril: 0,
            EnhancementScroll: -cost.RequiredScroll,
            DungeonTickets: 0,
            LevelUps: [],
            UnlockedSkillIds: [],
            AcquiredWeaponIds: [],
            MaxStage: 0
        );

        return new WeaponUpgradeResponse(weaponId, playerWeapon.EnhancementLevel, changes, playerResponse);
    }
}
