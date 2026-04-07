using Fantasy.Server.Domain.Player.Dto.Request;

namespace Fantasy.Server.Domain.Player.Service.Interface;

public interface IUpdatePlayerWeaponService
{
    Task ExecuteAsync(UpdatePlayerWeaponRequest request);
}
