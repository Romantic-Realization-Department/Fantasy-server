using Fantasy.Server.Domain.Player.Dto.Request;
using Fantasy.Server.Domain.Player.Repository.Interface;
using Fantasy.Server.Domain.Player.Service.Interface;
using Fantasy.Server.Global.Security.Provider;
using Gamism.SDK.Extensions.AspNetCore.Exceptions;

namespace Fantasy.Server.Domain.Player.Service;

public class UpdatePlayerSkillService : IUpdatePlayerSkillService
{
    private readonly IPlayerRepository _playerRepository;
    private readonly IPlayerSkillRepository _playerSkillRepository;
    private readonly IPlayerRedisRepository _playerRedisRepository;
    private readonly ICurrentUserProvider _currentUserProvider;

    public UpdatePlayerSkillService(
        IPlayerRepository playerRepository,
        IPlayerSkillRepository playerSkillRepository,
        IPlayerRedisRepository playerRedisRepository,
        ICurrentUserProvider currentUserProvider)
    {
        _playerRepository = playerRepository;
        _playerSkillRepository = playerSkillRepository;
        _playerRedisRepository = playerRedisRepository;
        _currentUserProvider = currentUserProvider;
    }

    public async Task ExecuteAsync(UpdatePlayerSkillRequest request)
    {
        var accountId = _currentUserProvider.GetAccountId();

        var player = await _playerRepository.FindByAccountAndJobAsync(accountId, request.JobType)
            ?? throw new NotFoundException("플레이어 데이터를 찾을 수 없습니다.");

        await _playerSkillRepository.UpsertRangeAsync(player.Id, request.Skills);

        await _playerRedisRepository.DeleteAsync(accountId, request.JobType);
    }
}
