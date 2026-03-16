using Fantasy.Auth.Domain.Auth.Dto.Request;
using Fantasy.Common.Domain.Auth.Dto.Response;

namespace Fantasy.Auth.Domain.Auth.Service.Interface;

public interface ILoginService
{
    Task<TokenResponse> ExecuteAsync(LoginRequest request);
}