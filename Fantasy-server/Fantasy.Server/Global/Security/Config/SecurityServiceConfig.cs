using Fantasy.Server.Global.Security.Jwt;
using Fantasy.Server.Global.Security.Provider;

namespace Fantasy.Server.Global.Security.Config;

public static class SecurityServiceConfig
{
    public static IServiceCollection AddSecurityServices(this IServiceCollection services)
    {
        services.AddSingleton<IJwtProvider, JwtProvider>();
        services.AddScoped<ICurrentUserProvider, CurrentUserProvider>();
        return services;
    }
}
