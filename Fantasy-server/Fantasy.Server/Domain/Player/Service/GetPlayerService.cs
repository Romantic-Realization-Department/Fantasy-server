using Fantasy.Server.Domain.Player.Dto.Response;
using Fantasy.Server.Domain.Player.Entity;
using Fantasy.Server.Domain.Player.Repository.Interface;
using Fantasy.Server.Domain.Player.Service.Interface;
using Fantasy.Server.Global.Security.Provider;
using Gamism.SDK.Extensions.AspNetCore.Exceptions;
using PlayerEntity = Fantasy.Server.Domain.Player.Entity.Player;

namespace Fantasy.Server.Domain.Player.Service;

public class GetPlayerService : IGetPlayerService
{
    private readonly IPlayerRepository _playerRepository;
    private readonly IPlayerResourceRepository _playerResourceRepository;
    private readonly IPlayerStageRepository _playerStageRepository;
    private readonly IPlayerSessionRepository _playerSessionRepository;
    private readonly IPlayerWeaponRepository _playerWeaponRepository;
    private readonly IPlayerSkillRepository _playerSkillRepository;
    private readonly IPlayerRedisRepository _playerRedisRepository;
    private readonly ICurrentUserProvider _currentUserProvider;

    public GetPlayerService(
        IPlayerRepository playerRepository,
        IPlayerResourceRepository playerResourceRepository,
        IPlayerStageRepository playerStageRepository,
        IPlayerSessionRepository playerSessionRepository,
        IPlayerWeaponRepository playerWeaponRepository,
        IPlayerSkillRepository playerSkillRepository,
        IPlayerRedisRepository playerRedisRepository,
        ICurrentUserProvider currentUserProvider)
    {
        _playerRepository = playerRepository;
        _playerResourceRepository = playerResourceRepository;
        _playerStageRepository = playerStageRepository;
        _playerSessionRepository = playerSessionRepository;
        _playerWeaponRepository = playerWeaponRepository;
        _playerSkillRepository = playerSkillRepository;
        _playerRedisRepository = playerRedisRepository;
        _currentUserProvider = currentUserProvider;
    }

    public async Task<PlayerDataResponse> ExecuteAsync()
    {
        long accountId = _currentUserProvider.GetAccountId();

        PlayerDataResponse? cached = await _playerRedisRepository.GetPlayerDataAsync(accountId);
        if (cached != null)
            return cached;

        PlayerEntity player = await _playerRepository.FindByAccountAsync(accountId)
            ?? throw new NotFoundException("플레이어를 찾을 수 없습니다.");

        PlayerResource resource = await _playerResourceRepository.FindByPlayerIdAsync(player.Id)
            ?? throw new InvalidOperationException("플레이어 재화 데이터를 찾을 수 없습니다.");
        PlayerStage stage = await _playerStageRepository.FindByPlayerIdAsync(player.Id)
            ?? throw new InvalidOperationException("플레이어 스테이지 데이터를 찾을 수 없습니다.");
        PlayerSession session = await _playerSessionRepository.FindByPlayerIdAsync(player.Id)
            ?? throw new InvalidOperationException("플레이어 세션 데이터를 찾을 수 없습니다.");

        List<Entity.PlayerWeapon> weapons = await _playerWeaponRepository.FindAllByPlayerIdAsync(player.Id);
        List<Entity.PlayerSkill> skills = await _playerSkillRepository.FindAllByPlayerIdAsync(player.Id);

        PlayerDataResponse response = PlayerDataResponseBuilder.Build(player, resource, stage, session, weapons, skills);

        await _playerRedisRepository.SetPlayerDataAsync(accountId, response);
        return response;
    }
}
