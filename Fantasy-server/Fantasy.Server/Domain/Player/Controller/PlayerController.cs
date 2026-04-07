using Fantasy.Server.Domain.Player.Dto.Request;
using Fantasy.Server.Domain.Player.Dto.Response;
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
public class PlayerController : ControllerBase
{
    private readonly IInitPlayerService _initPlayerService;
    private readonly IUpdatePlayerLevelService _updatePlayerLevelService;

    public PlayerController(
        IInitPlayerService initPlayerService,
        IUpdatePlayerLevelService updatePlayerLevelService)
    {
        _initPlayerService = initPlayerService;
        _updatePlayerLevelService = updatePlayerLevelService;
    }

    [HttpPost("init")]
    public async Task<CommonApiResponse<PlayerDataResponse>> Init([FromBody] InitPlayerRequest request)
    {
        var (data, isNew) = await _initPlayerService.ExecuteAsync(request);
        return isNew
            ? CommonApiResponse.Created("플레이어가 생성되었습니다.", data)
            : CommonApiResponse.Success("플레이어 데이터를 불러왔습니다.", data);
    }

    [HttpPatch("level")]
    public async Task<CommonApiResponse> UpdateLevel([FromBody] UpdatePlayerLevelRequest request)
    {
        await _updatePlayerLevelService.ExecuteAsync(request);
        return CommonApiResponse.Success("레벨이 업데이트되었습니다.");
    }
}
