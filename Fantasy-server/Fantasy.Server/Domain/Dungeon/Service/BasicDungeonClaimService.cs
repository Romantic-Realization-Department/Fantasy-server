using Fantasy.Server.Domain.Dungeon.Dto.Response;
using Fantasy.Server.Domain.Dungeon.Service.Interface;
using Fantasy.Server.Domain.Player.Dto.Response;
using Fantasy.Server.Domain.Player.Repository.Interface;
using Fantasy.Server.Global.Infrastructure;
using Fantasy.Server.Global.Security.Provider;
using Gamism.SDK.Extensions.AspNetCore.Exceptions;

namespace Fantasy.Server.Domain.Dungeon.Service;

public class BasicDungeonClaimService : IBasicDungeonClaimService
{
    private readonly IPlayerRepository _playerRepository;
    private readonly IPlayerResourceRepository _playerResourceRepository;
    private readonly IPlayerStageRepository _playerStageRepository;
    private readonly IPlayerSessionRepository _playerSessionRepository;
    private readonly IPlayerWeaponRepository _playerWeaponRepository;
    private readonly IPlayerSkillRepository _playerSkillRepository;
    private readonly IPlayerRedisRepository _playerRedisRepository;
    private readonly IIdleRewardSettler _idleRewardSettler;
    private readonly IAppDbTransactionRunner _transactionRunner;
    private readonly ICurrentUserProvider _currentUserProvider;

    public BasicDungeonClaimService(
        IPlayerRepository playerRepository,
        IPlayerResourceRepository playerResourceRepository,
        IPlayerStageRepository playerStageRepository,
        IPlayerSessionRepository playerSessionRepository,
        IPlayerWeaponRepository playerWeaponRepository,
        IPlayerSkillRepository playerSkillRepository,
        IPlayerRedisRepository playerRedisRepository,
        IIdleRewardSettler idleRewardSettler,
        IAppDbTransactionRunner transactionRunner,
        ICurrentUserProvider currentUserProvider)
    {
        _playerRepository = playerRepository;
        _playerResourceRepository = playerResourceRepository;
        _playerStageRepository = playerStageRepository;
        _playerSessionRepository = playerSessionRepository;
        _playerWeaponRepository = playerWeaponRepository;
        _playerSkillRepository = playerSkillRepository;
        _playerRedisRepository = playerRedisRepository;
        _idleRewardSettler = idleRewardSettler;
        _transactionRunner = transactionRunner;
        _currentUserProvider = currentUserProvider;
    }

    public async Task<BasicDungeonClaimResponse> ExecuteAsync()
    {
        var accountId = _currentUserProvider.GetAccountId();

        var player = await _playerRepository.FindByAccountAsync(accountId)
            ?? throw new NotFoundException("플레이어 데이터를 찾을 수 없습니다.");

        var resource = await _playerResourceRepository.FindByPlayerIdAsync(player.Id)
            ?? throw new NotFoundException("플레이어 재화 데이터를 찾을 수 없습니다.");

        var stage = await _playerStageRepository.FindByPlayerIdAsync(player.Id)
            ?? throw new NotFoundException("플레이어 스테이지 데이터를 찾을 수 없습니다.");

        var session = await _playerSessionRepository.FindByPlayerIdAsync(player.Id)
            ?? throw new NotFoundException("플레이어 세션 데이터를 찾을 수 없습니다.");

        var weapons = await _playerWeaponRepository.FindAllByPlayerIdAsync(player.Id);
        var skills = await _playerSkillRepository.FindAllByPlayerIdAsync(player.Id);

        var reward = await _idleRewardSettler.SettleAsync(player, resource, stage, session, weapons, skills);

        await _transactionRunner.ExecuteAsync(async () =>
        {
            await _playerRepository.UpdateAsync(player);
            await _playerResourceRepository.UpdateAsync(resource);
            await _playerStageRepository.UpdateAsync(stage);
        });

        var playerResponse = PlayerDataResponseBuilder.Build(player, resource, stage, session, weapons, skills);
        await _playerRedisRepository.SetPlayerDataAsync(accountId, playerResponse);

        var changes = new ChangesDto(
            Gold: reward.EarnedGold,
            Exp: reward.EarnedXp,
            Sp: reward.LevelUps.Sum(l => l.EarnedSp),
            Mithril: 0,
            EnhancementScroll: 0,
            DungeonTickets: 0,
            LevelUps: reward.LevelUps.Select(l => l.NewLevel).ToList(),
            UnlockedSkillIds: [],
            AcquiredWeaponIds: [],
            MaxStage: reward.NewMaxStage
        );

        return new BasicDungeonClaimResponse(changes, playerResponse);
    }
}
