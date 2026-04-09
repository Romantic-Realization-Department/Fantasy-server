using Fantasy.Server.Domain.Dungeon.Dto.Request;
using Fantasy.Server.Domain.Dungeon.Dto.Response;
using Fantasy.Server.Domain.Dungeon.Service.Interface;
using Fantasy.Server.Domain.Player.Enum;
using Gamism.SDK.Core.Network;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Fantasy.Server.Domain.Dungeon.Controller;

[ApiController]
[Route("v1/dungeon")]
[Authorize]
[EnableRateLimiting("game")]
public class DungeonController : ControllerBase
{
    private readonly IBasicDungeonClaimService _basicDungeonClaimService;
    private readonly INormalDungeonClearService _normalDungeonClearService;
    private readonly IGoldDungeonService _goldDungeonService;
    private readonly IWeaponDungeonService _weaponDungeonService;
    private readonly IBossDungeonService _bossDungeonService;

    public DungeonController(
        IBasicDungeonClaimService basicDungeonClaimService,
        INormalDungeonClearService normalDungeonClearService,
        IGoldDungeonService goldDungeonService,
        IWeaponDungeonService weaponDungeonService,
        IBossDungeonService bossDungeonService)
    {
        _basicDungeonClaimService = basicDungeonClaimService;
        _normalDungeonClearService = normalDungeonClearService;
        _goldDungeonService = goldDungeonService;
        _weaponDungeonService = weaponDungeonService;
        _bossDungeonService = bossDungeonService;
    }

    [HttpPost("basic/claim")]
    public async Task<CommonApiResponse<BasicDungeonClaimResponse>> BasicClaim([FromQuery] JobType jobType)
    {
        var result = await _basicDungeonClaimService.ExecuteAsync(jobType);
        return CommonApiResponse.Success("기본 던전 정산이 완료되었습니다.", result);
    }

    [HttpPost("normal/clear")]
    public async Task<CommonApiResponse<NormalDungeonClearResponse>> NormalClear(
        [FromQuery] JobType jobType,
        [FromBody] NormalDungeonClearRequest request)
    {
        var result = await _normalDungeonClearService.ExecuteAsync(jobType, request);
        return CommonApiResponse.Success("스테이지 클리어 보상이 지급되었습니다.", result);
    }

    [HttpPost("gold")]
    public async Task<CommonApiResponse<GoldDungeonResponse>> Gold(
        [FromQuery] JobType jobType,
        [FromBody] GoldDungeonRequest request)
    {
        var result = await _goldDungeonService.ExecuteAsync(jobType, request);
        return CommonApiResponse.Success("골드 던전이 완료되었습니다.", result);
    }

    [HttpPost("weapon")]
    public async Task<CommonApiResponse<WeaponDungeonResponse>> Weapon([FromQuery] JobType jobType)
    {
        var result = await _weaponDungeonService.ExecuteAsync(jobType);
        return CommonApiResponse.Success("무기 던전이 완료되었습니다.", result);
    }

    [HttpPost("boss")]
    public async Task<CommonApiResponse<BossDungeonResponse>> Boss([FromQuery] JobType jobType)
    {
        var result = await _bossDungeonService.ExecuteAsync(jobType);
        return CommonApiResponse.Success("보스 던전이 완료되었습니다.", result);
    }
}
