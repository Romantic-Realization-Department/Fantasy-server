using Fantasy.Server.Domain.Player.Dto.Request;
using Fantasy.Server.Domain.Player.Repository.Interface;
using Fantasy.Server.Domain.Player.Service.Interface;
using Fantasy.Server.Global.Security.Provider;
using Gamism.SDK.Extensions.AspNetCore.Exceptions;

namespace Fantasy.Server.Domain.Player.Service;

public class UpdatePlayerWeaponService : IUpdatePlayerWeaponService
{
    private readonly IPlayerRepository _playerRepository;
    private readonly IPlayerWeaponRepository _playerWeaponRepository;
    private readonly IPlayerRedisRepository _playerRedisRepository;
    private readonly ICurrentUserProvider _currentUserProvider;

    public UpdatePlayerWeaponService(
        IPlayerRepository playerRepository,
        IPlayerWeaponRepository playerWeaponRepository,
        IPlayerRedisRepository playerRedisRepository,
        ICurrentUserProvider currentUserProvider)
    {
        _playerRepository = playerRepository;
        _playerWeaponRepository = playerWeaponRepository;
        _playerRedisRepository = playerRedisRepository;
        _currentUserProvider = currentUserProvider;
    }

    public async Task ExecuteAsync(UpdatePlayerWeaponRequest request)
    {
        var accountId = _currentUserProvider.GetAccountId();

        var player = await _playerRepository.FindByAccountAndJobAsync(accountId, request.JobType)
            ?? throw new NotFoundException("플레이어 데이터를 찾을 수 없습니다.");

        await _playerWeaponRepository.UpsertRangeAsync(player.Id, request.Weapons);

        await _playerRedisRepository.DeleteAsync(accountId, request.JobType);
    }
}
