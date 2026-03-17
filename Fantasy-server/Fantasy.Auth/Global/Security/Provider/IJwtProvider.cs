using Fantasy.Common.Domain.Account.Entity;

namespace Fantasy.Auth.Global.Security.Provider;

public interface IJwtProvider
{
    string GenerateAccessToken(Account account);
    string GenerateRefreshToken();
}
