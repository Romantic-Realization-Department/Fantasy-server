using Fantasy.Server.Domain.Player.Dto.Request;

namespace Fantasy.Server.Domain.Player.Service.Interface;

public interface IUpdatePlayerSkillService
{
    Task ExecuteAsync(UpdatePlayerSkillRequest request);
}
