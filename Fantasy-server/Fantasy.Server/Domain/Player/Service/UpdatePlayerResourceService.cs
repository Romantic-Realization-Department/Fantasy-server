using Fantasy.Server.Domain.Player.Dto.Request;
using Fantasy.Server.Domain.Player.Repository.Interface;
using Fantasy.Server.Domain.Player.Service.Interface;
using Fantasy.Server.Global.Security.Provider;
using Gamism.SDK.Extensions.AspNetCore.Exceptions;

namespace Fantasy.Server.Domain.Player.Service;

public class UpdatePlayerResourceService : IUpdatePlayerResourceService
{
    private readonly IPlayerRepository _playerRepository;
    private readonly IPlayerResourceRepository _playerResourceRepository;
    private readonly IPlayerRedisRepository _playerRedisRepository;
    private readonly ICurrentUserProvider _currentUserProvider;

    public UpdatePlayerResourceService(
        IPlayerRepository playerRepository,
        IPlayerResourceRepository playerResourceRepository,
        IPlayerRedisRepository playerRedisRepository,
        ICurrentUserProvider currentUserProvider)
    {
        _playerRepository = playerRepository;
        _playerResourceRepository = playerResourceRepository;
        _playerRedisRepository = playerRedisRepository;
        _currentUserProvider = currentUserProvider;
    }

    public async Task ExecuteAsync(UpdatePlayerResourceRequest request)
    {
        var accountId = _currentUserProvider.GetAccountId();

        var player = await _playerRepository.FindByAccountAndJobAsync(accountId, request.JobType)
            ?? throw new NotFoundException("플레이어 데이터를 찾을 수 없습니다.");

        var resource = await _playerResourceRepository.FindByPlayerIdAsync(player.Id)
            ?? throw new NotFoundException("플레이어 재화 데이터를 찾을 수 없습니다.");

        resource.UpdateChangeData(request.EnhancementScroll, request.Mithril, request.Sp);
        await _playerResourceRepository.UpdateAsync(resource);

        await _playerRedisRepository.DeleteAsync(accountId, request.JobType);
    }
}
