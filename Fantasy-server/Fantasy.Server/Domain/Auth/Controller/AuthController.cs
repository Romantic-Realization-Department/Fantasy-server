using Fantasy.Server.Domain.Auth.Dto.Request;
using Fantasy.Server.Domain.Auth.Dto.Response;
using Fantasy.Server.Domain.Auth.Service.Interface;
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
        => await _loginService.ExecuteAsync(request);

    [Authorize]
    [HttpPost("logout")]
    public async Task Logout()
        => await _logoutService.ExecuteAsync();
}
