using AccountEntity = Fantasy.Server.Domain.Account.Entity.Account;

namespace Fantasy.Server.Domain.Account.Repository.Interface;

public interface IAccountRepository
{
    Task<AccountEntity?> FindByEmailAsync(string email);
    Task<AccountEntity?> FindByIdAsync(long id);
    Task<AccountEntity> SaveAsync(AccountEntity account);
    Task<bool> ExistsByEmailAsync(string email);
    Task DeleteAsync(AccountEntity account);
}
