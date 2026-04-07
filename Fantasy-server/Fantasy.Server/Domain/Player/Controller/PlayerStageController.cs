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
public class PlayerStageController : ControllerBase
{
    private readonly IUpdatePlayerStageService _updatePlayerStageService;

    public PlayerStageController(IUpdatePlayerStageService updatePlayerStageService)
    {
        _updatePlayerStageService = updatePlayerStageService;
    }

    [HttpPatch("stage")]
    public async Task<CommonApiResponse> UpdateStage([FromBody] UpdatePlayerStageRequest request)
    {
        await _updatePlayerStageService.ExecuteAsync(request);
        return CommonApiResponse.Success("스테이지가 업데이트되었습니다.");
    }
}
