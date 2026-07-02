using Fantasy.Server.Domain.Weapon.Dto.Response;

namespace Fantasy.Server.Domain.Weapon.Service.Interface;

public interface ISynthesizeWeaponService
{
    Task<WeaponSynthesizeResponse> ExecuteAsync(int weaponId);
}
