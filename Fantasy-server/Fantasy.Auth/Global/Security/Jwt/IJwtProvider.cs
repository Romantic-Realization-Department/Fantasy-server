using Fantasy.Common.Domain.Account.Entity;

namespace Fantasy.Auth.Global.Security.Jwt;

public interface IJwtProvider
{
    string GenerateAccessToken(Account account);
    string GenerateRefreshToken();
}
