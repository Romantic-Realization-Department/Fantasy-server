using Fantasy.Auth.Domain.Auth.Dto.Request;
using Fantasy.Common.Domain.Auth.Dto.Response;

namespace Fantasy.Auth.Domain.Auth.Service.Interface;

public interface IRefreshTokenService
{
    Task<TokenResponse> ExecuteAsync(RefreshTokenRequest request);
}