using Fantasy.Server.Domain.Player.Dto.Request;
using Fantasy.Server.Domain.Player.Dto.Response;

namespace Fantasy.Server.Domain.Player.Service.Interface;

public interface ISkillUnlockService
{
    Task<SkillUnlockResponse> ExecuteAsync(SkillUnlockRequest request);
}
