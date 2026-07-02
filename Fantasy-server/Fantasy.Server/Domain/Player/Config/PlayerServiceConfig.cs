using Fantasy.Server.Domain.Player.Repository;
using Fantasy.Server.Domain.Player.Repository.Interface;
using Fantasy.Server.Domain.Player.Service;
using Fantasy.Server.Domain.Player.Service.Interface;

namespace Fantasy.Server.Domain.Player.Config;

public static class PlayerServiceConfig
{
    public static IServiceCollection AddPlayerServices(this IServiceCollection services)
    {
        services.AddScoped<IPlayerRepository, PlayerRepository>();
        services.AddScoped<IPlayerResourceRepository, PlayerResourceRepository>();
        services.AddScoped<IPlayerStageRepository, PlayerStageRepository>();
        services.AddScoped<IPlayerSessionRepository, PlayerSessionRepository>();
        services.AddScoped<IPlayerWeaponRepository, PlayerWeaponRepository>();
        services.AddScoped<IPlayerSkillRepository, PlayerSkillRepository>();
        services.AddScoped<IPlayerRedisRepository, PlayerRedisRepository>();
        services.AddScoped<IRewardTransactionRepository, RewardTransactionRepository>();

        services.AddScoped<IGetPlayerService, GetPlayerService>();
        services.AddScoped<ICreatePlayerService, CreatePlayerService>();
        services.AddScoped<ILoadoutService, LoadoutService>();
        services.AddScoped<ISkillUnlockService, SkillUnlockService>();

        return services;
    }
}
