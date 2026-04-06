using Fantasy.Server.Domain.Auth.Repository;
using Fantasy.Server.Domain.Auth.Repository.Interface;
using Fantasy.Server.Domain.Auth.Service;
using Fantasy.Server.Domain.Auth.Service.Interface;

namespace Fantasy.Server.Domain.Auth.Config;

public static class AuthServiceConfig
{
    public static IServiceCollection AddAuthServices(this IServiceCollection services)
    {
        services.AddScoped<IRefreshTokenRedisRepository, RefreshTokenRedisRepository>();
        services.AddScoped<ILoginService, LoginService>();
        services.AddScoped<ILogoutService, LogoutService>();
        services.AddScoped<IRefreshTokenService, RefreshTokenService>();
        return services;
    }
}
