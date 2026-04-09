using Fantasy.Server.Domain.Dungeon.Dto.Request;
using Fantasy.Server.Domain.Dungeon.Dto.Response;
using Fantasy.Server.Domain.Dungeon.Service.Interface;
using Fantasy.Server.Domain.GameData.Service.Interface;
using Fantasy.Server.Domain.LevelUp.Service.Interface;
using Fantasy.Server.Domain.Player.Enum;
using Fantasy.Server.Domain.Player.Repository.Interface;
using Fantasy.Server.Global.Infrastructure;
using Fantasy.Server.Global.Security.Provider;
using Gamism.SDK.Extensions.AspNetCore.Exceptions;

namespace Fantasy.Server.Domain.Dungeon.Service;

public class NormalDungeonClearService : INormalDungeonClearService
{
    private const long ClearBonusSeconds = 60;

    private readonly IPlayerRepository _playerRepository;
    private readonly IPlayerResourceRepository _playerResourceRepository;
    private readonly IPlayerStageRepository _playerStageRepository;
    private readonly IPlayerRedisRepository _playerRedisRepository;
    private readonly IGameDataCacheService _gameDataCacheService;
    private readonly ILevelUpService _levelUpService;
    private readonly IAppDbTransactionRunner _transactionRunner;
    private readonly ICurrentUserProvider _currentUserProvider;

    public NormalDungeonClearService(
        IPlayerRepository playerRepository,
        IPlayerResourceRepository playerResourceRepository,
        IPlayerStageRepository playerStageRepository,
        IPlayerRedisRepository playerRedisRepository,
        IGameDataCacheService gameDataCacheService,
        ILevelUpService levelUpService,
        IAppDbTransactionRunner transactionRunner,
        ICurrentUserProvider currentUserProvider)
    {
        _playerRepository = playerRepository;
        _playerResourceRepository = playerResourceRepository;
        _playerStageRepository = playerStageRepository;
        _playerRedisRepository = playerRedisRepository;
        _gameDataCacheService = gameDataCacheService;
        _levelUpService = levelUpService;
        _transactionRunner = transactionRunner;
        _currentUserProvider = currentUserProvider;
    }

    public async Task<NormalDungeonClearResponse> ExecuteAsync(JobType jobType, NormalDungeonClearRequest request)
    {
        var accountId = _currentUserProvider.GetAccountId();

        var player = await _playerRepository.FindByAccountAndJobAsync(accountId, jobType)
            ?? throw new NotFoundException("플레이어 데이터를 찾을 수 없습니다.");

        var resource = await _playerResourceRepository.FindByPlayerIdAsync(player.Id)
            ?? throw new NotFoundException("플레이어 재화 데이터를 찾을 수 없습니다.");

        var stage = await _playerStageRepository.FindByPlayerIdAsync(player.Id)
            ?? throw new NotFoundException("플레이어 스테이지 데이터를 찾을 수 없습니다.");

        if (request.Stage > stage.MaxStage + 1)
            throw new BadRequestException("아직 도달하지 않은 스테이지입니다.");

        var stageData = await _gameDataCacheService.GetStageDataAsync(request.Stage)
            ?? throw new NotFoundException("스테이지 데이터를 찾을 수 없습니다.");

        var earnedGold = stageData.GoldPerSecond * ClearBonusSeconds;
        var earnedXp = stageData.XpPerSecond * ClearBonusSeconds;

        var levelUps = await _levelUpService.ExecuteAsync(player, resource, earnedXp);
        resource.UpdateGold(resource.Gold + earnedGold);

        if (request.Stage > stage.MaxStage)
            stage.Update(request.Stage);

        stage.UpdateLastCalculatedAt();

        await _transactionRunner.ExecuteAsync(async () =>
        {
            await _playerRepository.UpdateAsync(player);
            await _playerResourceRepository.UpdateAsync(resource);
            await _playerStageRepository.UpdateAsync(stage);
        });

        await _playerRedisRepository.DeleteAsync(accountId, jobType);

        return new NormalDungeonClearResponse(earnedGold, earnedXp, stage.MaxStage, player.Level, levelUps);
    }
}
