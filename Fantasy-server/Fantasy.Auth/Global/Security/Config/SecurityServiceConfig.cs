using Fantasy.Auth.Global.Security.Jwt;
using Fantasy.Auth.Global.Security.Provider;
using Fantasy.Common.Global.Security.Filter;

namespace Fantasy.Auth.Global.Security.Config;

public static class SecurityServiceConfig
{
    public static IServiceCollection AddSecurityServices(this IServiceCollection services)
    {
        services.AddSingleton<IJwtProvider, JwtProvider>();
        services.AddSingleton<JwtAuthenticationFilter>();
        services.AddScoped<ICurrentUserProvider, CurrentUserProvider>();
        return services;
    }
}
