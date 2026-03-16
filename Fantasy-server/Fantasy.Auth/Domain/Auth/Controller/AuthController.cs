using Fantasy.Auth.Domain.Auth.Dto.Request;
using Fantasy.Auth.Domain.Auth.Service.Interface;
using Fantasy.Common.Domain.Auth.Exception;
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
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        try
        {
            var response = await _loginService.ExecuteAsync(request);
            return Ok(response);
        }
        catch (InvalidCredentialsException)
        {
            return Unauthorized();
        }
    }
}