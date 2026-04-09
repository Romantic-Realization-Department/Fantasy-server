using Fantasy.Server.Domain.Dungeon.Service;
using Fantasy.Server.Domain.Dungeon.Service.Interface;

namespace Fantasy.Server.Domain.Dungeon.Config;

public static class DungeonServiceConfig
{
    public static IServiceCollection AddDungeonServices(this IServiceCollection services)
    {
        services.AddScoped<ICombatStatCalculator, CombatStatCalculator>();
        services.AddScoped<IBasicDungeonClaimService, BasicDungeonClaimService>();
        services.AddScoped<INormalDungeonClearService, NormalDungeonClearService>();
        services.AddScoped<IGoldDungeonService, GoldDungeonService>();
        services.AddScoped<IWeaponDungeonService, WeaponDungeonService>();
        services.AddScoped<IBossDungeonService, BossDungeonService>();
        return services;
    }
}
