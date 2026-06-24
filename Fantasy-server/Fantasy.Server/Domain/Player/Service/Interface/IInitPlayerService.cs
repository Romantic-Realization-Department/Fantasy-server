using Fantasy.Server.Domain.Player.Dto.Request;
using Fantasy.Server.Domain.Player.Dto.Response;

namespace Fantasy.Server.Domain.Player.Service.Interface;

public interface IInitPlayerService
{
    Task<(PlayerDataResponse Data, bool IsNew)> ExecuteAsync(InitPlayerRequest request);
}
