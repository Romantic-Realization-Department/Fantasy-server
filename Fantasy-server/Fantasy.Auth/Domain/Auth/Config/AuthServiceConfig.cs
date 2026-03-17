using Fantasy.Auth.Domain.Auth.Repository;
using Fantasy.Auth.Domain.Auth.Service;
using Fantasy.Auth.Domain.Auth.Service.Interface;
using Fantasy.Common.Domain.Auth.Repository;

namespace Fantasy.Auth.Domain.Auth.Config;

public static class AuthServiceConfig
{
    public static IServiceCollection AddAuthServices(this IServiceCollection services)
    {
        services.AddScoped<IRefreshTokenRedisRepository, RefreshTokenRedisRepository>();
        services.AddScoped<ILoginService, LoginService>();
        services.AddScoped<ILogoutService, LogoutService>();
        return services;
    }
}
