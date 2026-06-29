using Fantasy.Server.Domain.Player.Dto.Request;
using Fantasy.Server.Domain.Player.Dto.Response;

namespace Fantasy.Server.Domain.Player.Service.Interface;

public interface ICreatePlayerService
{
    Task<PlayerDataResponse> ExecuteAsync(CreatePlayerRequest request);
}
