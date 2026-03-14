namespace Fantasy.Common.Domain.Account.Entity.Repository;

public interface IAccountRepository
{
    Task<Account?> FindByEmailAsync(string email);
    Task<Account> SaveAsync(Account account);
    Task<bool> ExistsByEmailAsync(string email);
    Task DeleteAsync(Account account);
}