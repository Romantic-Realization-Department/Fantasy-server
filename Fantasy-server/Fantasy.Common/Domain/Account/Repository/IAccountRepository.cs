using AccountEntity = Fantasy.Common.Domain.Account.Entity.Account;

namespace Fantasy.Common.Domain.Account.Repository;

public interface IAccountRepository
{
    Task<AccountEntity?> FindByEmailAsync(string email);
    Task<AccountEntity> SaveAsync(AccountEntity account);
    Task<bool> ExistsByEmailAsync(string email);
    Task DeleteAsync(AccountEntity account);
}