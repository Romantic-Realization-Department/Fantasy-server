using Fantasy.Server.Domain.Account.Entity;

namespace Fantasy.Server.Global.Security.Provider;

public interface ICurrentUserProvider
{
    Task<Account> GetAccountAsync();
    string GetEmail();
}
