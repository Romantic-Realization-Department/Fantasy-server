using Fantasy.Server.Domain.Account.Entity;

namespace Fantasy.Server.Global.Security.Jwt;

public interface IJwtProvider
{
    string GenerateAccessToken(Account account);
    string GenerateRefreshToken();
}
