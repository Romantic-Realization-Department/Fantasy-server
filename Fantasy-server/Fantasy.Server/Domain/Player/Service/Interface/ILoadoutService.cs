using Fantasy.Server.Domain.Player.Dto.Request;
using Fantasy.Server.Domain.Player.Dto.Response;

namespace Fantasy.Server.Domain.Player.Service.Interface;

public interface ILoadoutService
{
    Task<LoadoutResponse> ExecuteAsync(LoadoutRequest request);
}
