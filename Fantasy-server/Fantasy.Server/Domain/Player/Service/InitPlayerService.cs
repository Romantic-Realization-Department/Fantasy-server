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
    private readonly IAppDbTransactionRunner _transactionRunner;

    public InitPlayerService(
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

    public async Task<(PlayerDataResponse Data, bool IsNew)> ExecuteAsync(InitPlayerRequest request)
    {
        long accountId = _currentUserProvider.GetAccountId();

        PlayerDataResponse? cached = await _playerRedisRepository.GetPlayerDataAsync(accountId, request.JobType);
        if (cached != null)
            return (cached, false);

        PlayerEntity? player = await _playerRepository.FindByAccountAndJobAsync(accountId, request.JobType);
        if (player == null)
        {
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

            List<Entity.PlayerWeapon> createdWeapons = await _playerWeaponRepository.FindAllByPlayerIdAsync(created.Player.Id);
            List<Entity.PlayerSkill> createdSkills = await _playerSkillRepository.FindAllByPlayerIdAsync(created.Player.Id);

            PlayerDataResponse createdResponse = BuildResponse(
                created.Player,
                created.Resource,
                created.Stage,
                created.Session,
                createdWeapons,
                createdSkills);

            await _playerRedisRepository.SetPlayerDataAsync(accountId, request.JobType, createdResponse);
            return (createdResponse, true);
        }

        PlayerResource existingResource = await _playerResourceRepository.FindByPlayerIdAsync(player.Id)
            ?? throw new NotFoundException("?뚮젅?댁뼱 ?ы솕 ?곗씠?곕? 李얠쓣 ???놁뒿?덈떎.");
        PlayerStage existingStage = await _playerStageRepository.FindByPlayerIdAsync(player.Id)
            ?? throw new NotFoundException("?뚮젅?댁뼱 ?ㅽ뀒?댁? ?곗씠?곕? 李얠쓣 ???놁뒿?덈떎.");
        PlayerSession existingSession = await _playerSessionRepository.FindByPlayerIdAsync(player.Id)
            ?? throw new NotFoundException("?뚮젅?댁뼱 ?몄뀡 ?곗씠?곕? 李얠쓣 ???놁뒿?덈떎.");

        List<Entity.PlayerWeapon> weapons = await _playerWeaponRepository.FindAllByPlayerIdAsync(player.Id);
        List<Entity.PlayerSkill> skills = await _playerSkillRepository.FindAllByPlayerIdAsync(player.Id);

        PlayerDataResponse response = BuildResponse(
            player,
            existingResource,
            existingStage,
            existingSession,
            weapons,
            skills);

        await _playerRedisRepository.SetPlayerDataAsync(accountId, request.JobType, response);
        return (response, false);
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
