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
public class PlayerResourceController : ControllerBase
{
    private readonly IUpdatePlayerResourceService _updatePlayerResourceService;

    public PlayerResourceController(IUpdatePlayerResourceService updatePlayerResourceService)
    {
        _updatePlayerResourceService = updatePlayerResourceService;
    }

    [HttpPatch("resource")]
    public async Task<CommonApiResponse> UpdateResource([FromBody] UpdatePlayerResourceRequest request)
    {
        await _updatePlayerResourceService.ExecuteAsync(request);
        return CommonApiResponse.Success("재화가 업데이트되었습니다.");
    }
}
