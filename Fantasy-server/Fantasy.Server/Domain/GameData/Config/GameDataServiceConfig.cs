using Fantasy.Server.Domain.GameData.Repository;
using Fantasy.Server.Domain.GameData.Repository.Interface;
using Fantasy.Server.Domain.GameData.Service;
using Fantasy.Server.Domain.GameData.Service.Interface;

namespace Fantasy.Server.Domain.GameData.Config;

public static class GameDataServiceConfig
{
    public static IServiceCollection AddGameDataServices(this IServiceCollection services)
    {
        services.AddScoped<IGameDataRepository, GameDataRepository>();
        services.AddScoped<IGameDataCacheService, GameDataCacheService>();
        return services;
    }
}
