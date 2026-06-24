using Fantasy.Server.Domain.Dungeon.Dto.Response;
using Fantasy.Server.Domain.Dungeon.Entity;
using Fantasy.Server.Domain.Dungeon.Repository.Interface;
using Fantasy.Server.Domain.Dungeon.Service.Interface;
using Fantasy.Server.Domain.Player.Dto.Response;
using Fantasy.Server.Domain.Player.Repository.Interface;
using Fantasy.Server.Global.Infrastructure;
using Fantasy.Server.Global.Security.Provider;
using Gamism.SDK.Extensions.AspNetCore.Exceptions;

namespace Fantasy.Server.Domain.Dungeon.Service;

public class GoldDungeonRunService : IGoldDungeonRunService
{
    private const int DurationSeconds = 30;
    private const int MaxClicksPerSecond = 15;

    private readonly IPlayerRepository _playerRepository;
    private readonly IPlayerResourceRepository _playerResourceRepository;
    private readonly IPlayerStageRepository _playerStageRepository;
    private readonly IPlayerSessionRepository _playerSessionRepository;
    private readonly IPlayerWeaponRepository _playerWeaponRepository;
    private readonly IPlayerSkillRepository _playerSkillRepository;
    private readonly IAccountDungeonTicketRepository _accountDungeonTicketRepository;
    private readonly IGoldDungeonRunRepository _goldDungeonRunRepository;
    private readonly IAppDbTransactionRunner _transactionRunner;
    private readonly ICurrentUserProvider _currentUserProvider;

    public GoldDungeonRunService(
        IPlayerRepository playerRepository,
        IPlayerResourceRepository playerResourceRepository,
        IPlayerStageRepository playerStageRepository,
        IPlayerSessionRepository playerSessionRepository,
        IPlayerWeaponRepository playerWeaponRepository,
        IPlayerSkillRepository playerSkillRepository,
        IAccountDungeonTicketRepository accountDungeonTicketRepository,
        IGoldDungeonRunRepository goldDungeonRunRepository,
        IAppDbTransactionRunner transactionRunner,
        ICurrentUserProvider currentUserProvider)
    {
        _playerRepository = playerRepository;
        _playerResourceRepository = playerResourceRepository;
        _playerStageRepository = playerStageRepository;
        _playerSessionRepository = playerSessionRepository;
        _playerWeaponRepository = playerWeaponRepository;
        _playerSkillRepository = playerSkillRepository;
        _accountDungeonTicketRepository = accountDungeonTicketRepository;
        _goldDungeonRunRepository = goldDungeonRunRepository;
        _transactionRunner = transactionRunner;
        _currentUserProvider = currentUserProvider;
    }

    public async Task<StartGoldDungeonResponse> ExecuteAsync()
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

        var (ticket, isNew) = await GetOrCreateTicketWithLazyGrantAsync(accountId);

        if (ticket.TicketCount <= 0)
            throw new BadRequestException("골드 던전 티켓이 부족합니다.");

        ticket.Consume();

        var run = GoldDungeonRun.Create(accountId, DurationSeconds, MaxClicksPerSecond);

        await _transactionRunner.ExecuteAsync(async () =>
        {
            if (isNew)
                await _accountDungeonTicketRepository.SaveAsync(ticket);
            else
                await _accountDungeonTicketRepository.UpdateAsync(ticket);

            await _goldDungeonRunRepository.SaveAsync(run);
        });

        var playerResponse = PlayerDataResponseBuilder.Build(player, resource, stage, session, weapons, skills);

        return new StartGoldDungeonResponse(
            RunId: run.Id,
            StartedAt: run.StartedAt,
            DurationSeconds: run.DurationSeconds,
            ExpiresAt: run.ExpiresAt,
            MaxClicks: run.MaxClicks,
            Player: playerResponse
        );
    }

    private async Task<(AccountDungeonTicket Ticket, bool IsNew)> GetOrCreateTicketWithLazyGrantAsync(long accountId)
    {
        var todayKst = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(9));
        var ticket = await _accountDungeonTicketRepository.FindByAccountIdAsync(accountId);

        if (ticket is null)
        {
            ticket = AccountDungeonTicket.Create(accountId, todayKst);
            return (ticket, true);
        }

        if (ticket.LastDailyGrantDate < todayKst)
        {
            ticket.GrantDaily(todayKst);
        }

        return (ticket, false);
    }
}
