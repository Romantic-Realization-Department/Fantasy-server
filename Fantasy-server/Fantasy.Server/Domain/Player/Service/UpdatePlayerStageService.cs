using Fantasy.Server.Domain.Player.Dto.Request;
using Fantasy.Server.Domain.Player.Repository.Interface;
using Fantasy.Server.Domain.Player.Service.Interface;
using Fantasy.Server.Global.Security.Provider;
using Gamism.SDK.Extensions.AspNetCore.Exceptions;

namespace Fantasy.Server.Domain.Player.Service;

public class UpdatePlayerStageService : IUpdatePlayerStageService
{
    private readonly IPlayerRepository _playerRepository;
    private readonly IPlayerStageRepository _playerStageRepository;
    private readonly IPlayerRedisRepository _playerRedisRepository;
    private readonly ICurrentUserProvider _currentUserProvider;

    public UpdatePlayerStageService(
        IPlayerRepository playerRepository,
        IPlayerStageRepository playerStageRepository,
        IPlayerRedisRepository playerRedisRepository,
        ICurrentUserProvider currentUserProvider)
    {
        _playerRepository = playerRepository;
        _playerStageRepository = playerStageRepository;
        _playerRedisRepository = playerRedisRepository;
        _currentUserProvider = currentUserProvider;
    }

    public async Task ExecuteAsync(UpdatePlayerStageRequest request)
    {
        var accountId = _currentUserProvider.GetAccountId();

        var player = await _playerRepository.FindByAccountAndJobAsync(accountId, request.JobType)
            ?? throw new NotFoundException("플레이어 데이터를 찾을 수 없습니다.");

        var stage = await _playerStageRepository.FindByPlayerIdAsync(player.Id)
            ?? throw new NotFoundException("플레이어 스테이지 데이터를 찾을 수 없습니다.");

        stage.Update(request.MaxStage);
        await _playerStageRepository.UpdateAsync(stage);

        await _playerRedisRepository.DeleteAsync(accountId, request.JobType);
    }
}
