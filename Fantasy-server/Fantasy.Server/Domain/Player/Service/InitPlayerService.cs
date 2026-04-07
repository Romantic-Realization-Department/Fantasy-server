using Fantasy.Server.Domain.Player.Dto.Request;
using Fantasy.Server.Domain.Player.Dto.Response;
using Fantasy.Server.Domain.Player.Entity;
using Fantasy.Server.Domain.Player.Repository.Interface;
using Fantasy.Server.Domain.Player.Service.Interface;
using Fantasy.Server.Global.Security.Provider;
using Gamism.SDK.Extensions.AspNetCore.Exceptions;
using PlayerEntity = Fantasy.Server.Domain.Player.Entity.Player;

namespace Fantasy.Server.Domain.Player.Service;

public class InitPlayerService : IInitPlayerService
{
    private readonly IPlayerRepository _playerRepository;
    private readonly IPlayerResourceRepository _playerResourceRepository;
    private readonly IPlayerStageRepository _playerStageRepository;
    private readonly IPlayerSessionRepository _playerSessionRepository;
    private readonly IPlayerWeaponRepository _playerWeaponRepository;
    private readonly IPlayerSkillRepository _playerSkillRepository;
    private readonly IPlayerRedisRepository _playerRedisRepository;
    private readonly ICurrentUserProvider _currentUserProvider;

    public InitPlayerService(
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

    public async Task<(PlayerDataResponse Data, bool IsNew)> ExecuteAsync(InitPlayerRequest request)
    {
        var accountId = _currentUserProvider.GetAccountId();

        var cached = await _playerRedisRepository.GetPlayerDataAsync(accountId, request.JobType);
        if (cached != null)
            return (cached, false);

        var isNew = false;
        var player = await _playerRepository.FindByAccountAndJobAsync(accountId, request.JobType);
        PlayerResource resource;
        PlayerStage stage;
        PlayerSession session;

        if (player == null)
        {
            player = PlayerEntity.Create(accountId, request.JobType);
            await _playerRepository.SaveAsync(player);

            resource = PlayerResource.Create(player.Id);
            await _playerResourceRepository.SaveAsync(resource);

            stage = PlayerStage.Create(player.Id);
            await _playerStageRepository.SaveAsync(stage);

            session = PlayerSession.Create(player.Id);
            await _playerSessionRepository.SaveAsync(session);

            isNew = true;
        }
        else
        {
            resource = await _playerResourceRepository.FindByPlayerIdAsync(player.Id)
                       ?? throw new NotFoundException("플레이어 재화 데이터를 찾을 수 없습니다.");
            stage = await _playerStageRepository.FindByPlayerIdAsync(player.Id)
                    ?? throw new NotFoundException("플레이어 스테이지 데이터를 찾을 수 없습니다.");
            session = await _playerSessionRepository.FindByPlayerIdAsync(player.Id)
                      ?? throw new NotFoundException("플레이어 세션 데이터를 찾을 수 없습니다.");
        }

        var weapons = await _playerWeaponRepository.FindAllByPlayerIdAsync(player.Id);
        var skills = await _playerSkillRepository.FindAllByPlayerIdAsync(player.Id);

        var response = BuildResponse(player, resource, stage, session, weapons, skills);
        await _playerRedisRepository.SetPlayerDataAsync(accountId, request.JobType, response);

        return (response, isNew);
    }

    private static PlayerDataResponse BuildResponse(
        PlayerEntity player,
        PlayerResource resource,
        PlayerStage stage,
        PlayerSession session,
        List<Entity.PlayerWeapon> weapons,
        List<Entity.PlayerSkill> skills) =>
        new(
            player.JobType,
            player.Level,
            stage.MaxStage,
            session.LastWeaponId,
            session.ActiveSkills,
            resource.Gold,
            player.Exp,
            resource.EnhancementScroll,
            resource.Mithril,
            resource.Sp,
            weapons.Select(w => new WeaponInfoResponse(w.WeaponId, w.Count, w.EnhancementLevel, w.AwakeningCount)).ToList(),
            skills.Select(s => new SkillInfoResponse(s.SkillId, s.IsUnlocked)).ToList()
        );
}