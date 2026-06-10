using Fantasy.Server.Domain.Dungeon.Dto.Response;
using Fantasy.Server.Domain.GameData.Service.Interface;
using Fantasy.Server.Domain.Player.Dto.Request;
using Fantasy.Server.Domain.Player.Dto.Response;
using Fantasy.Server.Domain.Player.Repository.Interface;
using Fantasy.Server.Domain.Player.Service.Interface;
using Fantasy.Server.Global.Infrastructure;
using Fantasy.Server.Global.Security.Provider;
using Gamism.SDK.Extensions.AspNetCore.Exceptions;

namespace Fantasy.Server.Domain.Player.Service;

public class SkillUnlockService : ISkillUnlockService
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

    public SkillUnlockService(
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

    public async Task<SkillUnlockResponse> ExecuteAsync(SkillUnlockRequest request)
    {
        var accountId = _currentUserProvider.GetAccountId();

        var player = await _playerRepository.FindByAccountAsync(accountId)
            ?? throw new NotFoundException("플레이어 데이터를 찾을 수 없습니다.");

        var skillData = await _gameDataCacheService.GetSkillDataAsync(request.SkillId)
            ?? throw new NotFoundException("존재하지 않는 스킬입니다.");

        if (skillData.JobType != player.JobType)
            throw new BadRequestException("해당 직업의 스킬이 아닙니다.");

        var playerSkill = await _playerSkillRepository.FindByPlayerIdAndSkillIdAsync(player.Id, request.SkillId);

        if (playerSkill?.IsUnlocked == true)
        {
            var resource = await _playerResourceRepository.FindByPlayerIdAsync(player.Id)
                ?? throw new NotFoundException("플레이어 재화 데이터를 찾을 수 없습니다.");
            var stage = await _playerStageRepository.FindByPlayerIdAsync(player.Id)
                ?? throw new NotFoundException("플레이어 스테이지 데이터를 찾을 수 없습니다.");
            var session = await _playerSessionRepository.FindByPlayerIdAsync(player.Id)
                ?? throw new NotFoundException("플레이어 세션 데이터를 찾을 수 없습니다.");
            var weapons = await _playerWeaponRepository.FindAllByPlayerIdAsync(player.Id);
            var skills = await _playerSkillRepository.FindAllByPlayerIdAsync(player.Id);

            var cached = PlayerDataResponseBuilder.Build(player, resource, stage, session, weapons, skills);
            return new SkillUnlockResponse(true, ChangesDto.Empty(), cached);
        }

        if (skillData.PrereqSkillId.HasValue)
        {
            var prereq = await _playerSkillRepository.FindByPlayerIdAndSkillIdAsync(player.Id, skillData.PrereqSkillId.Value);
            if (prereq?.IsUnlocked != true)
                throw new BadRequestException("선행 스킬이 해금되지 않았습니다.");
        }

        var existingResource = await _playerResourceRepository.FindByPlayerIdAsync(player.Id)
            ?? throw new NotFoundException("플레이어 재화 데이터를 찾을 수 없습니다.");

        if (existingResource.Sp < skillData.SpCost)
            throw new BadRequestException("SP가 부족합니다.");

        existingResource.UpdateChangeData(null, null, existingResource.Sp - skillData.SpCost);

        await _playerSkillRepository.UnlockAsync(player.Id, request.SkillId);

        await _transactionRunner.ExecuteAsync(async () =>
        {
            await _playerRepository.UpdateAsync(player);
            await _playerResourceRepository.UpdateAsync(existingResource);
        });

        var existingStage = await _playerStageRepository.FindByPlayerIdAsync(player.Id)
            ?? throw new NotFoundException("플레이어 스테이지 데이터를 찾을 수 없습니다.");
        var existingSession = await _playerSessionRepository.FindByPlayerIdAsync(player.Id)
            ?? throw new NotFoundException("플레이어 세션 데이터를 찾을 수 없습니다.");
        var allWeapons = await _playerWeaponRepository.FindAllByPlayerIdAsync(player.Id);
        var allSkills = await _playerSkillRepository.FindAllByPlayerIdAsync(player.Id);

        var playerResponse = PlayerDataResponseBuilder.Build(player, existingResource, existingStage, existingSession, allWeapons, allSkills);
        await _playerRedisRepository.SetPlayerDataAsync(accountId, playerResponse);

        var changes = new ChangesDto(
            Gold: 0,
            Exp: 0,
            Sp: -skillData.SpCost,
            Mithril: 0,
            EnhancementScroll: 0,
            DungeonTickets: 0,
            LevelUps: [],
            UnlockedSkillIds: [request.SkillId],
            AcquiredWeaponIds: [],
            MaxStage: 0
        );

        return new SkillUnlockResponse(false, changes, playerResponse);
    }
}
