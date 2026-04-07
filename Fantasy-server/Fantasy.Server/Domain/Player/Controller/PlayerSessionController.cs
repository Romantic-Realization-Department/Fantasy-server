using Fantasy.Server.Domain.Player.Dto.Request;
using Fantasy.Server.Domain.Player.Service.Interface;
using Gamism.SDK.Core.Network;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Fantasy.Server.Domain.Player.Controller;

[ApiController]
[Route("v1/player")]
[Authorize]
[EnableRateLimiting("game")]
public class PlayerSessionController : ControllerBase
{
    private readonly IEndPlayerSessionService _endPlayerSessionService;

    public PlayerSessionController(IEndPlayerSessionService endPlayerSessionService)
    {
        _endPlayerSessionService = endPlayerSessionService;
    }

    [HttpPatch("session/end")]
    public async Task<CommonApiResponse> EndSession([FromBody] EndPlayerSessionRequest request)
    {
        await _endPlayerSessionService.ExecuteAsync(request);
        return CommonApiResponse.Success("게임 종료 데이터가 저장되었습니다.");
    }
}
