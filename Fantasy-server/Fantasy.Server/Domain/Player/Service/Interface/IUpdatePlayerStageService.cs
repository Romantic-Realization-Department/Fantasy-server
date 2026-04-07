using Fantasy.Server.Domain.Player.Dto.Request;

namespace Fantasy.Server.Domain.Player.Service.Interface;

public interface IUpdatePlayerStageService
{
    Task ExecuteAsync(UpdatePlayerStageRequest request);
}
