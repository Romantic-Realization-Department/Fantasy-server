using Fantasy.Server.Domain.Auth.Dto.Request;
using Fantasy.Server.Domain.Auth.Dto.Response;
using Fantasy.Server.Domain.Auth.Service.Interface;
using Gamism.SDK.Core.Network;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Fantasy.Server.Domain.Auth.Controller;

[ApiController]
[Route("v1/auth")]
public class AuthController : ControllerBase
{
    private readonly ILoginService _loginService;
    private readonly ILogoutService _logoutService;
    private readonly IRefreshTokenService _refreshTokenService;

    public AuthController(
        ILoginService loginService,
        ILogoutService logoutService,
        IRefreshTokenService refreshTokenService)
    {
        _loginService = loginService;
        _logoutService = logoutService;
        _refreshTokenService = refreshTokenService;
    }

    [HttpPost("login")]
    [EnableRateLimiting("login")]
    public async Task<CommonApiResponse<TokenResponse>> Login([FromBody] LoginRequest request)
    {
        var result = await _loginService.ExecuteAsync(request);
        return CommonApiResponse.Success("로그인 성공.", result);
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<CommonApiResponse> Logout()
    {
        await _logoutService.ExecuteAsync();
        return CommonApiResponse.Success("로그아웃 성공.");
    }

    [Authorize]
    [HttpPost("refresh")]
    public async Task<CommonApiResponse<TokenResponse>> Refresh([FromBody] RefreshTokenRequest request)
    {
        var result = await _refreshTokenService.ExecuteAsync(request);
        return CommonApiResponse.Success("토큰 갱신 성공.", result);
    }
}
