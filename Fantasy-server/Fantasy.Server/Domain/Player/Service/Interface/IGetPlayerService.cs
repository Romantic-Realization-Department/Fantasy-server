using Fantasy.Server.Domain.Player.Dto.Response;

namespace Fantasy.Server.Domain.Player.Service.Interface;

public interface IGetPlayerService
{
    Task<PlayerDataResponse> ExecuteAsync();
}
