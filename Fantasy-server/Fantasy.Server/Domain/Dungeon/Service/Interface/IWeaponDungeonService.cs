using Fantasy.Server.Domain.Dungeon.Dto.Response;
using Fantasy.Server.Domain.Player.Enum;

namespace Fantasy.Server.Domain.Dungeon.Service.Interface;

public interface IWeaponDungeonService
{
    Task<WeaponDungeonResponse> ExecuteAsync(JobType jobType);
}
