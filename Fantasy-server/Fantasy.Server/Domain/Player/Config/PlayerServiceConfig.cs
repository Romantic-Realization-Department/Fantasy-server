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

        services.AddScoped<IInitPlayerService, InitPlayerService>();
        services.AddScoped<IEndPlayerSessionService, EndPlayerSessionService>();
        services.AddScoped<IUpdatePlayerLevelService, UpdatePlayerLevelService>();
        services.AddScoped<IUpdatePlayerStageService, UpdatePlayerStageService>();
        services.AddScoped<IUpdatePlayerResourceService, UpdatePlayerResourceService>();
        services.AddScoped<IUpdatePlayerWeaponService, UpdatePlayerWeaponService>();
        services.AddScoped<IUpdatePlayerSkillService, UpdatePlayerSkillService>();

        return services;
    }
}
