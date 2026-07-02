using Fantasy.Server.Domain.Weapon.Service;
using Fantasy.Server.Domain.Weapon.Service.Interface;

namespace Fantasy.Server.Domain.Weapon.Config;

public static class WeaponServiceConfig
{
    public static IServiceCollection AddWeaponServices(this IServiceCollection services)
    {
        services.AddScoped<IUpgradeWeaponService, UpgradeWeaponService>();
        services.AddScoped<ISynthesizeWeaponService, SynthesizeWeaponService>();
        services.AddScoped<IAwakenWeaponService, AwakenWeaponService>();
        return services;
    }
}
