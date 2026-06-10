using Fantasy.Server.Domain.Dungeon.Entity;

namespace Fantasy.Server.Domain.Dungeon.Repository.Interface;

public interface IAccountDungeonTicketRepository
{
    Task<AccountDungeonTicket?> FindByAccountIdAsync(long accountId);
    Task SaveAsync(AccountDungeonTicket ticket);
    Task UpdateAsync(AccountDungeonTicket ticket);
}
