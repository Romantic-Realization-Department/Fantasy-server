using Fantasy.Server.Domain.Player.Dto.Request;

namespace Fantasy.Server.Domain.Player.Service.Interface;

public interface IEndPlayerSessionService
{
    Task ExecuteAsync(EndPlayerSessionRequest request);
}
