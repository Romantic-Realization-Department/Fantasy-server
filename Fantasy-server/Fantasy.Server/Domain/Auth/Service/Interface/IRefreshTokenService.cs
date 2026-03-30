using Fantasy.Server.Domain.Auth.Dto.Request;
using Fantasy.Server.Domain.Auth.Dto.Response;

namespace Fantasy.Server.Domain.Auth.Service.Interface;

public interface IRefreshTokenService
{
    Task<TokenResponse> ExecuteAsync(RefreshTokenRequest request);
}
