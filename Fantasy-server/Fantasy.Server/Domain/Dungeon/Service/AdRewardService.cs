using Fantasy.Server.Domain.Dungeon.Dto.Response;
using Fantasy.Server.Domain.Dungeon.Entity;
using Fantasy.Server.Domain.Dungeon.Repository.Interface;
using Fantasy.Server.Domain.Dungeon.Service.Interface;
using Fantasy.Server.Global.Infrastructure;
using Fantasy.Server.Global.Security.Provider;
using Gamism.SDK.Extensions.AspNetCore.Exceptions;

namespace Fantasy.Server.Domain.Dungeon.Service;

public class AdRewardService : IAdRewardService
{
    private readonly IAccountDungeonTicketRepository _accountDungeonTicketRepository;
    private readonly IAppDbTransactionRunner _transactionRunner;
    private readonly ICurrentUserProvider _currentUserProvider;

    public AdRewardService(
        IAccountDungeonTicketRepository accountDungeonTicketRepository,
        IAppDbTransactionRunner transactionRunner,
        ICurrentUserProvider currentUserProvider)
    {
        _accountDungeonTicketRepository = accountDungeonTicketRepository;
        _transactionRunner = transactionRunner;
        _currentUserProvider = currentUserProvider;
    }

    public async Task<DungeonTicketResponse> ExecuteAsync()
    {
        var accountId = _currentUserProvider.GetAccountId();
        var todayKst = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(9));

        var ticket = await _accountDungeonTicketRepository.FindByAccountIdAsync(accountId);
        var isNew = ticket is null;

        if (isNew)
        {
            ticket = AccountDungeonTicket.Create(accountId, todayKst);
        }
        else if (ticket!.LastDailyGrantDate < todayKst)
        {
            ticket.GrantDaily(todayKst);
        }

        if (ticket.DailyAdRewardClaimedDate == todayKst)
            throw new ConflictException("오늘 이미 광고 보상을 받았습니다.");

        ticket.GrantAdReward(todayKst);

        await _transactionRunner.ExecuteAsync(async () =>
        {
            if (isNew)
                await _accountDungeonTicketRepository.SaveAsync(ticket);
            else
                await _accountDungeonTicketRepository.UpdateAsync(ticket);
        });

        return new DungeonTicketResponse(ticket.TicketCount, ticket.LastDailyGrantDate, ticket.DailyAdRewardClaimedDate);
    }
}
