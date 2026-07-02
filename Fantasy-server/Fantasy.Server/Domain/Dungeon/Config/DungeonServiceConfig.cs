using Fantasy.Server.Domain.Dungeon.Repository;
using Fantasy.Server.Domain.Dungeon.Repository.Interface;
using Fantasy.Server.Domain.Dungeon.Service;
using Fantasy.Server.Domain.Dungeon.Service.Interface;
using Fantasy.Server.Global.Infrastructure;

namespace Fantasy.Server.Domain.Dungeon.Config;

public static class DungeonServiceConfig
{
    public static IServiceCollection AddDungeonServices(this IServiceCollection services)
    {
        services.AddScoped<ICombatStatCalculator, CombatStatCalculator>();
        services.AddScoped<IIdleRewardSettler, IdleRewardSettler>();
        services.AddScoped<IBasicDungeonStateService, BasicDungeonStateService>();
        services.AddScoped<IBasicDungeonClaimService, BasicDungeonClaimService>();
        services.AddScoped<IWeaponDungeonService, WeaponDungeonService>();
        services.AddScoped<IBossDungeonService, BossDungeonService>();
        services.AddScoped<IAccountDungeonTicketRepository, AccountDungeonTicketRepository>();
        services.AddScoped<IGoldDungeonRunRepository, GoldDungeonRunRepository>();
        services.AddScoped<IPlayerDungeonProgressRepository, PlayerDungeonProgressRepository>();
        services.AddScoped<IGoldDungeonRunService, GoldDungeonRunService>();
        services.AddScoped<IGoldDungeonClaimService, GoldDungeonClaimService>();
        services.AddScoped<IDungeonTicketService, DungeonTicketService>();
        services.AddScoped<IAdRewardService, AdRewardService>();
        services.AddSingleton<IRandomProvider, RandomProvider>();
        return services;
    }
}
