using Fantasy.Server.Domain.Player.Dto.Request;
using Fantasy.Server.Domain.Player.Repository.Interface;
using Fantasy.Server.Domain.Player.Service.Interface;
using Fantasy.Server.Global.Security.Provider;
using Gamism.SDK.Extensions.AspNetCore.Exceptions;

namespace Fantasy.Server.Domain.Player.Service;

public class EndPlayerSessionService : IEndPlayerSessionService
{
    private readonly IPlayerRepository _playerRepository;
    private readonly IPlayerResourceRepository _playerResourceRepository;
    private readonly IPlayerSessionRepository _playerSessionRepository;
    private readonly IPlayerRedisRepository _playerRedisRepository;
    private readonly ICurrentUserProvider _currentUserProvider;

    public EndPlayerSessionService(
        IPlayerRepository playerRepository,
        IPlayerResourceRepository playerResourceRepository,
        IPlayerSessionRepository playerSessionRepository,
        IPlayerRedisRepository playerRedisRepository,
        ICurrentUserProvider currentUserProvider)
    {
        _playerRepository = playerRepository;
        _playerResourceRepository = playerResourceRepository;
        _playerSessionRepository = playerSessionRepository;
        _playerRedisRepository = playerRedisRepository;
        _currentUserProvider = currentUserProvider;
    }

    public async Task ExecuteAsync(EndPlayerSessionRequest request)
    {
        var accountId = _currentUserProvider.GetAccountId();

        var player = await _playerRepository.FindByAccountAndJobAsync(accountId, request.JobType)
            ?? throw new NotFoundException("플레이어 데이터를 찾을 수 없습니다.");

        var session = await _playerSessionRepository.FindByPlayerIdAsync(player.Id)
            ?? throw new NotFoundException("플레이어 세션 데이터를 찾을 수 없습니다.");

        session.Update(request.LastWeaponId, request.ActiveSkills);
        await _playerSessionRepository.UpdateAsync(session);

        if (request.Exp.HasValue)
        {
            player.UpdateExp(request.Exp.Value);
            await _playerRepository.UpdateAsync(player);
        }

        if (request.Gold.HasValue)
        {
            var resource = await _playerResourceRepository.FindByPlayerIdAsync(player.Id)
                ?? throw new NotFoundException("플레이어 재화 데이터를 찾을 수 없습니다.");
            resource.UpdateGold(request.Gold.Value);
            await _playerResourceRepository.UpdateAsync(resource);
        }

        await _playerRedisRepository.DeleteAsync(accountId, request.JobType);
    }
}
