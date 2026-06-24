using Fantasy.Server.Domain.LevelUp.Dto.Response;
using Fantasy.Server.Domain.Player.Entity;
using PlayerEntity = Fantasy.Server.Domain.Player.Entity.Player;

namespace Fantasy.Server.Domain.LevelUp.Service.Interface;

public interface ILevelUpService
{
    Task<List<LevelUpResult>> ExecuteAsync(PlayerEntity player, PlayerResource resource, long earnedExp);
}
