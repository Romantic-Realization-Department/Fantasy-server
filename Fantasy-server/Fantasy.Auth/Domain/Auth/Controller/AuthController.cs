using Fantasy.Auth.Domain.Auth.Dto.Request;
using Fantasy.Auth.Domain.Auth.Service.Interface;
using Fantasy.Common.Domain.Auth.Dto.Response;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Fantasy.Auth.Domain.Auth.Controller;

[ApiController]
[Route("v1/auth")]
public class AuthController : ControllerBase
{
    private readonly ILoginService _loginService;
    private readonly ILogoutService _logoutService;

    public AuthController(
        ILoginService loginService,
        ILogoutService logoutService)
    {
        _loginService = loginService;
        _logoutService = logoutService;
    }

    [HttpPost("login")]
    [EnableRateLimiting("login")]
    public async Task<TokenResponse> Login([FromBody] LoginRequest request)
    {
        return await _loginService.ExecuteAsync(request);
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task Logout()
    {
        await _logoutService.ExecuteAsync();
    }
}
