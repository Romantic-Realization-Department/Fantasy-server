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
    private readonly ILoadoutService _loadoutService;
    private readonly ISkillUnlockService _skillUnlockService;

    public PlayerController(
        IInitPlayerService initPlayerService,
        ILoadoutService loadoutService,
        ISkillUnlockService skillUnlockService)
    {
        _initPlayerService = initPlayerService;
        _loadoutService = loadoutService;
        _skillUnlockService = skillUnlockService;
    }

    [HttpPost("init")]
    public async Task<CommonApiResponse<PlayerDataResponse>> Init([FromBody] InitPlayerRequest request)
    {
        var (data, isNew) = await _initPlayerService.ExecuteAsync(request);
        return isNew
            ? CommonApiResponse.Created("플레이어가 생성되었습니다.", data)
            : CommonApiResponse.Success("플레이어 데이터를 불러왔습니다.", data);
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
