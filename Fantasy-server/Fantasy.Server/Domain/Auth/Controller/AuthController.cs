using Fantasy.Server.Domain.Auth.Dto.Request;
using Fantasy.Server.Domain.Auth.Dto.Response;
using Fantasy.Server.Domain.Auth.Service.Interface;
using Gamism.SDK.Core.Network;
using Gamism.SDK.Extensions.AspNetCore.Exceptions;
using Gamism.SDK.Extensions.AspNetCore.Swagger;
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

    [ApiDoc("로그인", "이메일과 비밀번호로 로그인하고 액세스·리프레시 토큰을 발급합니다.")]
    [ApiError(typeof(UnauthorizedException), "이메일 또는 비밀번호가 올바르지 않습니다.")]
    [HttpPost("login")]
    [EnableRateLimiting("login")]
    public async Task<CommonApiResponse<TokenResponse>> Login([FromBody] LoginRequest request)
    {
        var result = await _loginService.ExecuteAsync(request);
        return CommonApiResponse.Success("로그인 성공.", result);
    }

    [ApiDoc("로그아웃", "현재 계정의 리프레시 토큰을 무효화합니다.")]
    [Authorize]
    [HttpPost("logout")]
    public async Task<CommonApiResponse> Logout()
    {
        await _logoutService.ExecuteAsync();
        return CommonApiResponse.Success("로그아웃 성공.");
    }

    [ApiDoc("토큰 갱신", "리프레시 토큰으로 새 액세스·리프레시 토큰을 발급합니다.")]
    [ApiError(typeof(UnauthorizedException), "리프레시 토큰을 찾을 수 없습니다.")]
    [ApiError(typeof(UnauthorizedException), "유효하지 않은 리프레시 토큰입니다.")]
    [ApiError(typeof(UnauthorizedException), "토큰 재사용이 감지되었습니다.")]
    [ApiError(typeof(UnauthorizedException), "인증 정보를 찾을 수 없습니다.")]
    [HttpPost("refresh")]
    public async Task<CommonApiResponse<TokenResponse>> Refresh([FromBody] RefreshTokenRequest request)
    {
        var result = await _refreshTokenService.ExecuteAsync(request);
        return CommonApiResponse.Success("토큰 갱신 성공.", result);
    }
}
