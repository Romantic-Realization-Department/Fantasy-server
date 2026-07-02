using Fantasy.Server.Domain.Weapon.Dto.Response;

namespace Fantasy.Server.Domain.Weapon.Service.Interface;

public interface IAwakenWeaponService
{
    Task<WeaponAwakenResponse> ExecuteAsync(int weaponId);
}
