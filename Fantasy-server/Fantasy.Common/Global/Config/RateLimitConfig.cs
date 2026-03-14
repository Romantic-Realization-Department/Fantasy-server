using Microsoft.AspNetCore.RateLimiting;

namespace Fantasy.Common.Global.Config;

public static class RateLimitConfig
{
    public static IServiceCollection AddRateLimit(
        this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.AddFixedWindowLimiter("login", opt =>
            {
                opt.PermitLimit = 5;
                opt.Window = TimeSpan.FromMinutes(1);
            });

            options.AddFixedWindowLimiter("game", opt =>
            {
                opt.PermitLimit = 30;
                opt.Window = TimeSpan.FromSeconds(1);
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