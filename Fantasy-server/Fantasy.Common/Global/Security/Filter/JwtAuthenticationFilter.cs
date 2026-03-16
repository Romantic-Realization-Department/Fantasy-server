using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Fantasy.Common.Global.Security.Filter;

public class JwtAuthenticationFilter : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var user = context.HttpContext.User;

        if (user.Identity?.IsAuthenticated != true)
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        var accountIdClaim = user.FindFirstValue(JwtRegisteredClaimNames.Sub);
        if (accountIdClaim is null || !long.TryParse(accountIdClaim, out var accountId))
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        context.HttpContext.Items["AccountId"] = accountId;
        context.HttpContext.Items["Email"] = user.FindFirstValue(JwtRegisteredClaimNames.Email);
        context.HttpContext.Items["Role"] = user.FindFirstValue(ClaimTypes.Role);

        await next();
    }
}
