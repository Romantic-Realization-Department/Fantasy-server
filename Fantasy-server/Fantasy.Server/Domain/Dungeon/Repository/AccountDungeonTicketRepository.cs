using Fantasy.Server.Domain.Dungeon.Entity;
using Fantasy.Server.Domain.Dungeon.Repository.Interface;
using Fantasy.Server.Global.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Fantasy.Server.Domain.Dungeon.Repository;

public class AccountDungeonTicketRepository : IAccountDungeonTicketRepository
{
    private readonly AppDbContext _dbContext;

    public AccountDungeonTicketRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<AccountDungeonTicket?> FindByAccountIdAsync(long accountId) =>
        await _dbContext.AccountDungeonTickets
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.AccountId == accountId);

    public async Task SaveAsync(AccountDungeonTicket ticket)
    {
        _dbContext.AccountDungeonTickets.Add(ticket);
        await _dbContext.SaveChangesAsync();
    }

    public async Task UpdateAsync(AccountDungeonTicket ticket)
    {
        _dbContext.AccountDungeonTickets.Update(ticket);
        await _dbContext.SaveChangesAsync();
    }
}
