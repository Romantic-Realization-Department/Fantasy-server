using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Gamism.SDK.Extensions.AspNetCore.Exceptions;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Fantasy.Common.Global.Security.Filter;

public class JwtAuthenticationFilter : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var user = context.HttpContext.User;

        if (user.Identity?.IsAuthenticated != true)
        {
            throw new UnauthorizedException("인증이 필요합니다.");
        }

        var accountIdClaim = user.FindFirstValue(JwtRegisteredClaimNames.Sub);
        if (accountIdClaim is null || !long.TryParse(accountIdClaim, out var accountId))
        {
            throw new UnauthorizedException("유효하지 않은 토큰입니다.");
        }

        context.HttpContext.Items["AccountId"] = accountId;
        context.HttpContext.Items["Email"] = user.FindFirstValue(JwtRegisteredClaimNames.Email);
        context.HttpContext.Items["Role"] = user.FindFirstValue(ClaimTypes.Role);

        await next();
    }
}
