using Fantasy.Server.Domain.Dungeon.Dto.Request;
using Fantasy.Server.Domain.Dungeon.Dto.Response;
using Fantasy.Server.Domain.Dungeon.Entity;
using Fantasy.Server.Domain.Dungeon.Enum;
using Fantasy.Server.Domain.Dungeon.Repository.Interface;
using Fantasy.Server.Domain.Dungeon.Service.Interface;
using Fantasy.Server.Domain.Player.Dto.Response;
using Fantasy.Server.Domain.Player.Repository.Interface;
using Fantasy.Server.Global.Infrastructure;
using Fantasy.Server.Global.Security.Provider;
using Gamism.SDK.Extensions.AspNetCore.Exceptions;

namespace Fantasy.Server.Domain.Dungeon.Service;

public class GoldDungeonClaimService : IGoldDungeonClaimService
{
    private const long GoldPerClick = 10L;
    private const int MithrilDropRatePercent = 2;

    private readonly IPlayerRepository _playerRepository;
    private readonly IPlayerResourceRepository _playerResourceRepository;
    private readonly IPlayerStageRepository _playerStageRepository;
    private readonly IPlayerSessionRepository _playerSessionRepository;
    private readonly IPlayerWeaponRepository _playerWeaponRepository;
    private readonly IPlayerSkillRepository _playerSkillRepository;
    private readonly IPlayerRedisRepository _playerRedisRepository;
    private readonly IGoldDungeonRunRepository _goldDungeonRunRepository;
    private readonly IPlayerDungeonProgressRepository _playerDungeonProgressRepository;
    private readonly IRandomProvider _randomProvider;
    private readonly IAppDbTransactionRunner _transactionRunner;
    private readonly ICurrentUserProvider _currentUserProvider;
    private readonly TimeProvider _timeProvider;

    public GoldDungeonClaimService(
        IPlayerRepository playerRepository,
        IPlayerResourceRepository playerResourceRepository,
        IPlayerStageRepository playerStageRepository,
        IPlayerSessionRepository playerSessionRepository,
        IPlayerWeaponRepository playerWeaponRepository,
        IPlayerSkillRepository playerSkillRepository,
        IPlayerRedisRepository playerRedisRepository,
        IGoldDungeonRunRepository goldDungeonRunRepository,
        IPlayerDungeonProgressRepository playerDungeonProgressRepository,
        IRandomProvider randomProvider,
        IAppDbTransactionRunner transactionRunner,
        ICurrentUserProvider currentUserProvider,
        TimeProvider timeProvider)
    {
        _playerRepository = playerRepository;
        _playerResourceRepository = playerResourceRepository;
        _playerStageRepository = playerStageRepository;
        _playerSessionRepository = playerSessionRepository;
        _playerWeaponRepository = playerWeaponRepository;
        _playerSkillRepository = playerSkillRepository;
        _playerRedisRepository = playerRedisRepository;
        _goldDungeonRunRepository = goldDungeonRunRepository;
        _playerDungeonProgressRepository = playerDungeonProgressRepository;
        _randomProvider = randomProvider;
        _transactionRunner = transactionRunner;
        _currentUserProvider = currentUserProvider;
        _timeProvider = timeProvider;
    }

    public async Task<GoldDungeonClaimResponse> ExecuteAsync(Guid runId, GoldDungeonClaimRequest request)
    {
        var accountId = _currentUserProvider.GetAccountId();

        var run = await _goldDungeonRunRepository.FindByIdAsync(runId)
            ?? throw new NotFoundException("골드 던전 런을 찾을 수 없습니다.");

        if (run.AccountId != accountId)
            throw new ForbiddenException("접근 권한이 없습니다.");

        if (run.IsClaimed)
        {
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

            var playerResponse = PlayerDataResponseBuilder.Build(player, resource, stage, session, weapons, skills);

            var existingProgress = await _playerDungeonProgressRepository.FindByPlayerIdAndDungeonTypeAsync(player.Id, DungeonType.Gold);

            var idempotentChanges = new ChangesDto(
                Gold: run.EarnedGold!.Value,
                Exp: 0,
                Sp: 0,
                Mithril: run.EarnedMithril!.Value,
                EnhancementScroll: 0,
                DungeonTickets: 0,
                LevelUps: [],
                UnlockedSkillIds: [],
                AcquiredWeaponIds: [],
                MaxStage: 0
            );

            return new GoldDungeonClaimResponse(run.Id, run.EarnedGold!.Value, run.EarnedMithril!.Value,
                existingProgress?.HighScore ?? 0, idempotentChanges, playerResponse);
        }

        var now = _timeProvider.GetUtcNow().UtcDateTime;

        if (now > run.ExpiresAt)
            throw new BadRequestException("골드 던전 제한 시간이 초과되었습니다.");

        if (request.Clicks > run.MaxClicks)
            throw new BadRequestException("비정상적인 클릭 횟수입니다.");

        var elapsedSeconds = Math.Clamp((now - run.StartedAt).TotalSeconds, 0, run.DurationSeconds);
        var maxAllowedClicks = (int)Math.Ceiling(elapsedSeconds * run.MaxClicks / run.DurationSeconds);
        if (request.Clicks > maxAllowedClicks)
            throw new BadRequestException("경과 시간 대비 비정상적인 클릭 횟수입니다.");

        var earnedGold = request.Clicks * GoldPerClick;
        var mithrilDropped = _randomProvider.Next(0, 100) < MithrilDropRatePercent;

        var claimPlayer = await _playerRepository.FindByAccountAsync(accountId)
            ?? throw new NotFoundException("플레이어 데이터를 찾을 수 없습니다.");

        var claimResource = await _playerResourceRepository.FindByPlayerIdAsync(claimPlayer.Id)
            ?? throw new NotFoundException("플레이어 재화 데이터를 찾을 수 없습니다.");

        var claimStage = await _playerStageRepository.FindByPlayerIdAsync(claimPlayer.Id)
            ?? throw new NotFoundException("플레이어 스테이지 데이터를 찾을 수 없습니다.");

        var claimSession = await _playerSessionRepository.FindByPlayerIdAsync(claimPlayer.Id)
            ?? throw new NotFoundException("플레이어 세션 데이터를 찾을 수 없습니다.");

        var claimWeapons = await _playerWeaponRepository.FindAllByPlayerIdAsync(claimPlayer.Id);
        var claimSkills = await _playerSkillRepository.FindAllByPlayerIdAsync(claimPlayer.Id);

        claimResource.UpdateGold(claimResource.Gold + earnedGold);
        if (mithrilDropped)
            claimResource.UpdateChangeData(null, claimResource.Mithril + 1, null);

        run.Claim(request.Clicks, earnedGold, mithrilDropped ? 1 : 0);

        var progress = await _playerDungeonProgressRepository.FindByPlayerIdAndDungeonTypeAsync(claimPlayer.Id, DungeonType.Gold);
        var isNewProgress = progress is null;
        progress ??= PlayerDungeonProgress.Create(claimPlayer.Id, DungeonType.Gold);
        progress.UpdateHighScore(earnedGold);

        await _transactionRunner.ExecuteAsync(async () =>
        {
            await _playerResourceRepository.UpdateAsync(claimResource);
            await _goldDungeonRunRepository.UpdateAsync(run);

            if (isNewProgress)
                await _playerDungeonProgressRepository.SaveAsync(progress);
            else
                await _playerDungeonProgressRepository.UpdateAsync(progress);
        });

        var claimPlayerResponse = PlayerDataResponseBuilder.Build(claimPlayer, claimResource, claimStage, claimSession, claimWeapons, claimSkills);
        await _playerRedisRepository.SetPlayerDataAsync(accountId, claimPlayerResponse);

        var changes = new ChangesDto(
            Gold: earnedGold,
            Exp: 0,
            Sp: 0,
            Mithril: mithrilDropped ? 1 : 0,
            EnhancementScroll: 0,
            DungeonTickets: 0,
            LevelUps: [],
            UnlockedSkillIds: [],
            AcquiredWeaponIds: [],
            MaxStage: 0
        );

        return new GoldDungeonClaimResponse(run.Id, earnedGold, mithrilDropped ? 1 : 0, progress.HighScore, changes, claimPlayerResponse);
    }
}
