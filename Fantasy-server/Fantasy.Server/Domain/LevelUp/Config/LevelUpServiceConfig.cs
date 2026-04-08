using Fantasy.Server.Domain.LevelUp.Service;
using Fantasy.Server.Domain.LevelUp.Service.Interface;

namespace Fantasy.Server.Domain.LevelUp.Config;

public static class LevelUpServiceConfig
{
    public static IServiceCollection AddLevelUpServices(this IServiceCollection services)
    {
        services.AddScoped<ILevelUpService, LevelUpService>();
        return services;
    }
}
