using Fantasy.Server.Domain.Account.Repository.Interface;
using Fantasy.Server.Global.Infrastructure;
using Microsoft.EntityFrameworkCore;
using AccountEntity = Fantasy.Server.Domain.Account.Entity.Account;

namespace Fantasy.Server.Domain.Account.Repository;

public class AccountRepository : IAccountRepository
{
    private readonly AppDbContext _db;

    public AccountRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<AccountEntity?> FindByEmailAsync(string email)
        => await _db.Accounts
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Email == email);

    public async Task<AccountEntity?> FindByIdAsync(long id)
        => await _db.Accounts
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == id);

    public async Task<AccountEntity> SaveAsync(AccountEntity account)
    {
        var entry = _db.Accounts.Entry(account);

        if (entry.State == EntityState.Detached)
            await _db.Accounts.AddAsync(account);

        await _db.SaveChangesAsync();
        return account;
    }

    public async Task<bool> ExistsByEmailAsync(string email)
        => await _db.Accounts.AnyAsync(a => a.Email == email);

    public async Task DeleteAsync(AccountEntity account)
    {
        _db.Accounts.Remove(account);
        await _db.SaveChangesAsync();
    }
}
