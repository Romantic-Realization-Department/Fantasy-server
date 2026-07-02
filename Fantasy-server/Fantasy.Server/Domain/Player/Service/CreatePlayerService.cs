using Fantasy.Server.Domain.Player.Dto.Request;
using Fantasy.Server.Domain.Player.Dto.Response;
using Fantasy.Server.Domain.Player.Entity;
using Fantasy.Server.Domain.Player.Repository.Interface;
using Fantasy.Server.Domain.Player.Service.Interface;
using Fantasy.Server.Global.Infrastructure;
using Fantasy.Server.Global.Security.Provider;
using Gamism.SDK.Extensions.AspNetCore.Exceptions;
using PlayerEntity = Fantasy.Server.Domain.Player.Entity.Player;

namespace Fantasy.Server.Domain.Player.Service;

public class CreatePlayerService : ICreatePlayerService
{
    private readonly IPlayerRepository _playerRepository;
    private readonly IPlayerResourceRepository _playerResourceRepository;
    private readonly IPlayerStageRepository _playerStageRepository;
    private readonly IPlayerSessionRepository _playerSessionRepository;
    private readonly IPlayerWeaponRepository _playerWeaponRepository;
    private readonly IPlayerSkillRepository _playerSkillRepository;
    private readonly IPlayerRedisRepository _playerRedisRepository;
    private readonly ICurrentUserProvider _currentUserProvider;
    private readonly IAppDbTransactionRunner _transactionRunner;

    public CreatePlayerService(
        IPlayerRepository playerRepository,
        IPlayerResourceRepository playerResourceRepository,
        IPlayerStageRepository playerStageRepository,
        IPlayerSessionRepository playerSessionRepository,
        IPlayerWeaponRepository playerWeaponRepository,
        IPlayerSkillRepository playerSkillRepository,
        IPlayerRedisRepository playerRedisRepository,
        ICurrentUserProvider currentUserProvider,
        IAppDbTransactionRunner transactionRunner)
    {
        _playerRepository = playerRepository;
        _playerResourceRepository = playerResourceRepository;
        _playerStageRepository = playerStageRepository;
        _playerSessionRepository = playerSessionRepository;
        _playerWeaponRepository = playerWeaponRepository;
        _playerSkillRepository = playerSkillRepository;
        _playerRedisRepository = playerRedisRepository;
        _currentUserProvider = currentUserProvider;
        _transactionRunner = transactionRunner;
    }

    public async Task<PlayerDataResponse> ExecuteAsync(CreatePlayerRequest request)
    {
        long accountId = _currentUserProvider.GetAccountId();

        PlayerEntity? existing = await _playerRepository.FindByAccountAsync(accountId);
        if (existing != null)
            throw new ConflictException("이미 플레이어가 존재합니다.");

        var created = await _transactionRunner.ExecuteAsync(async () =>
        {
            PlayerEntity newPlayer = PlayerEntity.Create(accountId, request.JobType);
            await _playerRepository.SaveAsync(newPlayer);

            PlayerResource resource = PlayerResource.Create(newPlayer.Id);
            await _playerResourceRepository.SaveAsync(resource);

            PlayerStage stage = PlayerStage.Create(newPlayer.Id);
            await _playerStageRepository.SaveAsync(stage);

            PlayerSession session = PlayerSession.Create(newPlayer.Id);
            await _playerSessionRepository.SaveAsync(session);

            return (Player: newPlayer, Resource: resource, Stage: stage, Session: session);
        });

        List<Entity.PlayerWeapon> weapons = await _playerWeaponRepository.FindAllByPlayerIdAsync(created.Player.Id);
        List<Entity.PlayerSkill> skills = await _playerSkillRepository.FindAllByPlayerIdAsync(created.Player.Id);

        PlayerDataResponse response = PlayerDataResponseBuilder.Build(
            created.Player, created.Resource, created.Stage, created.Session, weapons, skills);

        await _playerRedisRepository.SetPlayerDataAsync(accountId, response);
        return response;
    }
}
