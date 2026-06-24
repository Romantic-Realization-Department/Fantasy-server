using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace Fantasy.Server.Global.Config;

public static class RateLimitConfig
{
    public static IServiceCollection AddRateLimit(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            // 로그인 전 API: IP 기준 partition
            options.AddPolicy("login", context =>
            {
                var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                return RateLimitPartition.GetFixedWindowLimiter(ip, _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 5,
                    Window = TimeSpan.FromMinutes(1)
                });
            });

            // 게임 API: account ID 기준 partition (인증 후 JWT sub claim 사용)
            options.AddPolicy("game", context =>
            {
                var accountId = context.User?.FindFirst("sub")?.Value
                    ?? context.Connection.RemoteIpAddress?.ToString()
                    ?? "unknown";

                return RateLimitPartition.GetFixedWindowLimiter(accountId, _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 30,
                    Window = TimeSpan.FromSeconds(1)
                });
            });

            options.OnRejected = async (context, token) =>
            {
                context.HttpContext.Response.StatusCode = 429;
                await context.HttpContext.Response.WriteAsync("Too Many Requests", token);
            };
        });

        return services;
    }
}
