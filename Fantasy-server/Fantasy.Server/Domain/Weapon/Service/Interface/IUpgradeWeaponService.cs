using Fantasy.Server.Domain.Weapon.Dto.Response;

namespace Fantasy.Server.Domain.Weapon.Service.Interface;

public interface IUpgradeWeaponService
{
    Task<WeaponUpgradeResponse> ExecuteAsync(int weaponId);
}
