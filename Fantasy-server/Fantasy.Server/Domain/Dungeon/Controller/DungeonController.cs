using Fantasy.Server.Domain.Dungeon.Dto.Request;
using Fantasy.Server.Domain.Dungeon.Dto.Response;
using Fantasy.Server.Domain.Dungeon.Service.Interface;
using Gamism.SDK.Core.Network;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Fantasy.Server.Domain.Dungeon.Controller;

[ApiController]
[Route("v1/dungeons")]
[Authorize]
[EnableRateLimiting("game")]
public class DungeonController : ControllerBase
{
    private readonly IBasicDungeonStateService _basicDungeonStateService;
    private readonly IBasicDungeonClaimService _basicDungeonClaimService;
    private readonly IWeaponDungeonService _weaponDungeonService;
    private readonly IBossDungeonService _bossDungeonService;
    private readonly IGoldDungeonRunService _goldDungeonRunService;
    private readonly IGoldDungeonClaimService _goldDungeonClaimService;
    private readonly IDungeonTicketService _dungeonTicketService;
    private readonly IAdRewardService _adRewardService;

    public DungeonController(
        IBasicDungeonStateService basicDungeonStateService,
        IBasicDungeonClaimService basicDungeonClaimService,
        IWeaponDungeonService weaponDungeonService,
        IBossDungeonService bossDungeonService,
        IGoldDungeonRunService goldDungeonRunService,
        IGoldDungeonClaimService goldDungeonClaimService,
        IDungeonTicketService dungeonTicketService,
        IAdRewardService adRewardService)
    {
        _basicDungeonStateService = basicDungeonStateService;
        _basicDungeonClaimService = basicDungeonClaimService;
        _weaponDungeonService = weaponDungeonService;
        _bossDungeonService = bossDungeonService;
        _goldDungeonRunService = goldDungeonRunService;
        _goldDungeonClaimService = goldDungeonClaimService;
        _dungeonTicketService = dungeonTicketService;
        _adRewardService = adRewardService;
    }

    [HttpGet("basic/state")]
    public async Task<CommonApiResponse<BasicDungeonStateResponse>> BasicState()
    {
        var result = await _basicDungeonStateService.ExecuteAsync();
        return CommonApiResponse.Success("기본 던전 상태를 조회했습니다.", result);
    }

    [HttpPost("basic/claim")]
    public async Task<CommonApiResponse<BasicDungeonClaimResponse>> BasicClaim()
    {
        var result = await _basicDungeonClaimService.ExecuteAsync();
        return CommonApiResponse.Success("기본 던전 정산이 완료되었습니다.", result);
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

    [HttpPost("gold-runs")]
    public async Task<CommonApiResponse<StartGoldDungeonResponse>> StartGoldRun()
    {
        var result = await _goldDungeonRunService.ExecuteAsync();
        return CommonApiResponse.Success("골드 던전이 시작되었습니다.", result);
    }

    [HttpPost("gold-runs/{runId}/claim")]
    public async Task<CommonApiResponse<GoldDungeonClaimResponse>> ClaimGoldRun(
        Guid runId,
        [FromBody] GoldDungeonClaimRequest request)
    {
        var result = await _goldDungeonClaimService.ExecuteAsync(runId, request);
        return CommonApiResponse.Success("골드 던전 보상을 수령했습니다.", result);
    }

    [HttpGet("tickets")]
    public async Task<CommonApiResponse<DungeonTicketResponse>> GetTickets()
    {
        var result = await _dungeonTicketService.ExecuteAsync();
        return CommonApiResponse.Success("던전 티켓 정보를 조회했습니다.", result);
    }

    [HttpPost("gold-tickets/ad-reward")]
    public async Task<CommonApiResponse<DungeonTicketResponse>> ClaimAdReward()
    {
        var result = await _adRewardService.ExecuteAsync();
        return CommonApiResponse.Success("광고 보상 티켓을 수령했습니다.", result);
    }
}
