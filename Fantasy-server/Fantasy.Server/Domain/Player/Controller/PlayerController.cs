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

    public PlayerController(IInitPlayerService initPlayerService)
    {
        _initPlayerService = initPlayerService;
    }

    [HttpPost("init")]
    public async Task<CommonApiResponse<PlayerDataResponse>> Init([FromBody] InitPlayerRequest request)
    {
        var (data, isNew) = await _initPlayerService.ExecuteAsync(request);
        return isNew
            ? CommonApiResponse.Created("플레이어가 생성되었습니다.", data)
            : CommonApiResponse.Success("플레이어 데이터를 불러왔습니다.", data);
    }
}
