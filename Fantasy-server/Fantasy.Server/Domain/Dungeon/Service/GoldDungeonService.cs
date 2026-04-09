using Fantasy.Server.Domain.Dungeon.Dto.Request;
using Fantasy.Server.Domain.Dungeon.Dto.Response;
using Fantasy.Server.Domain.Dungeon.Service.Interface;
using Fantasy.Server.Domain.Player.Enum;
using Fantasy.Server.Domain.Player.Repository.Interface;
using Fantasy.Server.Global.Security.Provider;
using Gamism.SDK.Extensions.AspNetCore.Exceptions;

namespace Fantasy.Server.Domain.Dungeon.Service;

public class GoldDungeonService : IGoldDungeonService
{
    private const long GoldPerClick = 10;
    private const int MithrilDropRatePercent = 2;

    private readonly IPlayerRepository _playerRepository;
    private readonly IPlayerResourceRepository _playerResourceRepository;
    private readonly IPlayerRedisRepository _playerRedisRepository;
    private readonly ICurrentUserProvider _currentUserProvider;

    public GoldDungeonService(
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

    public async Task<GoldDungeonResponse> ExecuteAsync(JobType jobType, GoldDungeonRequest request)
    {
        var accountId = _currentUserProvider.GetAccountId();

        var player = await _playerRepository.FindByAccountAndJobAsync(accountId, jobType)
            ?? throw new NotFoundException("플레이어 데이터를 찾을 수 없습니다.");

        var resource = await _playerResourceRepository.FindByPlayerIdAsync(player.Id)
            ?? throw new NotFoundException("플레이어 재화 데이터를 찾을 수 없습니다.");

        var earnedGold = request.Clicks * GoldPerClick;
        var mithrilDropped = Random.Shared.Next(0, 100) < MithrilDropRatePercent;

        resource.UpdateGold(resource.Gold + earnedGold);
        if (mithrilDropped)
            resource.UpdateChangeData(null, resource.Mithril + 1, null);

        await _playerResourceRepository.UpdateAsync(resource);
        await _playerRedisRepository.DeleteAsync(accountId, jobType);

        return new GoldDungeonResponse(earnedGold, mithrilDropped);
    }
}
