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
    private readonly IGetPlayerService _getPlayerService;
    private readonly ICreatePlayerService _createPlayerService;
    private readonly ILoadoutService _loadoutService;
    private readonly ISkillUnlockService _skillUnlockService;

    public PlayerController(
        IGetPlayerService getPlayerService,
        ICreatePlayerService createPlayerService,
        ILoadoutService loadoutService,
        ISkillUnlockService skillUnlockService)
    {
        _getPlayerService = getPlayerService;
        _createPlayerService = createPlayerService;
        _loadoutService = loadoutService;
        _skillUnlockService = skillUnlockService;
    }

    [HttpGet]
    public async Task<PlayerDataResponse> Get() => await _getPlayerService.ExecuteAsync();

    [HttpPost]
    public async Task<CommonApiResponse<PlayerDataResponse>> Create([FromBody] CreatePlayerRequest request)
    {
        var data = await _createPlayerService.ExecuteAsync(request);
        return CommonApiResponse.Created("플레이어가 생성되었습니다.", data);
    }

    [HttpPost("loadout")]
    public async Task<CommonApiResponse<LoadoutResponse>> Loadout([FromBody] LoadoutRequest request)
    {
        var result = await _loadoutService.ExecuteAsync(request);
        return CommonApiResponse.Success("로드아웃이 저장되었습니다.", result);
    }

    [HttpPost("skill/unlock")]
    public async Task<CommonApiResponse<SkillUnlockResponse>> UnlockSkill([FromBody] SkillUnlockRequest request)
    {
        var result = await _skillUnlockService.ExecuteAsync(request);
        return CommonApiResponse.Success("스킬 해금이 처리되었습니다.", result);
    }
}
