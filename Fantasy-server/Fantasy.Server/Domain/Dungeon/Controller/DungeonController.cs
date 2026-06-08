using Fantasy.Server.Domain.Dungeon.Dto.Request;
using Fantasy.Server.Domain.Dungeon.Dto.Response;
using Fantasy.Server.Domain.Dungeon.Service.Interface;
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
    private readonly IGoldDungeonService _goldDungeonService;
    private readonly IWeaponDungeonService _weaponDungeonService;
    private readonly IBossDungeonService _bossDungeonService;

    public DungeonController(
        IBasicDungeonClaimService basicDungeonClaimService,
        IGoldDungeonService goldDungeonService,
        IWeaponDungeonService weaponDungeonService,
        IBossDungeonService bossDungeonService)
    {
        _basicDungeonClaimService = basicDungeonClaimService;
        _goldDungeonService = goldDungeonService;
        _weaponDungeonService = weaponDungeonService;
        _bossDungeonService = bossDungeonService;
    }

    [HttpPost("basic/claim")]
    public async Task<CommonApiResponse<BasicDungeonClaimResponse>> BasicClaim()
    {
        var result = await _basicDungeonClaimService.ExecuteAsync();
        return CommonApiResponse.Success("기본 던전 정산이 완료되었습니다.", result);
    }

    [HttpPost("gold")]
    public async Task<CommonApiResponse<GoldDungeonResponse>> Gold([FromBody] GoldDungeonRequest request)
    {
        var result = await _goldDungeonService.ExecuteAsync(request);
        return CommonApiResponse.Success("골드 던전이 완료되었습니다.", result);
    }

    [HttpPost("weapon")]
    public async Task<CommonApiResponse<WeaponDungeonResponse>> Weapon()
    {
        var result = await _weaponDungeonService.ExecuteAsync();
        return CommonApiResponse.Success("무기 던전이 완료되었습니다.", result);
    }

    [HttpPost("boss")]
    public async Task<CommonApiResponse<BossDungeonResponse>> Boss()
    {
        var result = await _bossDungeonService.ExecuteAsync();
        return CommonApiResponse.Success("보스 던전이 완료되었습니다.", result);
    }
}
