namespace Fantasy.Common.Domain.Account.Repository;

public interface IAccountRepository
{
    Task<Entity.Account?> FindByEmailAsync(string email);
    Task<Entity.Account> SaveAsync(Entity.Account account);
    Task<bool> ExistsByEmailAsync(string email);
    Task DeleteAsync(Entity.Account account);
}