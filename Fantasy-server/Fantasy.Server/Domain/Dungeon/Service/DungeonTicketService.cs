using Fantasy.Server.Domain.Dungeon.Dto.Response;
using Fantasy.Server.Domain.Dungeon.Entity;
using Fantasy.Server.Domain.Dungeon.Repository.Interface;
using Fantasy.Server.Domain.Dungeon.Service.Interface;
using Fantasy.Server.Global.Infrastructure;
using Fantasy.Server.Global.Security.Provider;

namespace Fantasy.Server.Domain.Dungeon.Service;

public class DungeonTicketService : IDungeonTicketService
{
    private readonly IAccountDungeonTicketRepository _accountDungeonTicketRepository;
    private readonly IAppDbTransactionRunner _transactionRunner;
    private readonly ICurrentUserProvider _currentUserProvider;

    public DungeonTicketService(
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

        if (ticket is null)
        {
            ticket = AccountDungeonTicket.Create(accountId, todayKst);
            await _accountDungeonTicketRepository.SaveAsync(ticket);
        }
        else if (ticket.LastDailyGrantDate < todayKst)
        {
            ticket.GrantDaily(todayKst);
            await _accountDungeonTicketRepository.UpdateAsync(ticket);
        }

        return new DungeonTicketResponse(ticket.TicketCount, ticket.LastDailyGrantDate, ticket.DailyAdRewardClaimedDate);
    }
}
