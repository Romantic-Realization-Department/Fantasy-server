using Fantasy.Server.Domain.Dungeon.Dto.Request;
using Fantasy.Server.Domain.Dungeon.Dto.Response;
using Fantasy.Server.Domain.Dungeon.Service.Interface;
using Gamism.SDK.Core.Network;
using Gamism.SDK.Extensions.AspNetCore.Exceptions;
using Gamism.SDK.Extensions.AspNetCore.Swagger;
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

    [ApiDoc("기본 던전 상태 조회", "방치 기본 던전의 현재 누적 상태를 조회합니다.")]
    [ApiError(typeof(NotFoundException), "플레이어 데이터를 찾을 수 없습니다.")]
    [ApiError(typeof(NotFoundException), "플레이어 스테이지 데이터를 찾을 수 없습니다.")]
    [ApiError(typeof(NotFoundException), "플레이어 세션 데이터를 찾을 수 없습니다.")]
    [ApiError(typeof(NotFoundException), "스테이지 데이터를 찾을 수 없습니다.")]
    [ApiError(typeof(NotFoundException), "직업 기본 스탯 데이터를 찾을 수 없습니다.")]
    [HttpGet("basic/state")]
    public async Task<CommonApiResponse<BasicDungeonStateResponse>> BasicState()
    {
        var result = await _basicDungeonStateService.ExecuteAsync();
        return CommonApiResponse.Success("기본 던전 상태를 조회했습니다.", result);
    }

    [ApiDoc("기본 던전 정산", "기본 던전에 누적된 방치 보상을 정산합니다.")]
    [ApiError(typeof(NotFoundException), "플레이어 데이터를 찾을 수 없습니다.")]
    [ApiError(typeof(NotFoundException), "플레이어 재화 데이터를 찾을 수 없습니다.")]
    [ApiError(typeof(NotFoundException), "플레이어 스테이지 데이터를 찾을 수 없습니다.")]
    [ApiError(typeof(NotFoundException), "플레이어 세션 데이터를 찾을 수 없습니다.")]
    [ApiError(typeof(NotFoundException), "스테이지 데이터를 찾을 수 없습니다.")]
    [ApiError(typeof(NotFoundException), "직업 기본 스탯 데이터를 찾을 수 없습니다.")]
    [HttpPost("basic/claim")]
    public async Task<CommonApiResponse<BasicDungeonClaimResponse>> BasicClaim()
    {
        var result = await _basicDungeonClaimService.ExecuteAsync();
        return CommonApiResponse.Success("기본 던전 정산이 완료되었습니다.", result);
    }

    [ApiDoc("무기 던전", "무기 던전을 진행하고 획득 결과를 반환합니다.")]
    [ApiError(typeof(NotFoundException), "플레이어 데이터를 찾을 수 없습니다.")]
    [ApiError(typeof(NotFoundException), "플레이어 재화 데이터를 찾을 수 없습니다.")]
    [ApiError(typeof(NotFoundException), "플레이어 스테이지 데이터를 찾을 수 없습니다.")]
    [ApiError(typeof(NotFoundException), "플레이어 세션 데이터를 찾을 수 없습니다.")]
    [ApiError(typeof(NotFoundException), "직업 기본 스탯 데이터를 찾을 수 없습니다.")]
    [ApiError(typeof(NotFoundException), "스테이지 데이터를 찾을 수 없습니다.")]
    [HttpPost("weapon")]
    public async Task<CommonApiResponse<WeaponDungeonResponse>> Weapon()
    {
        var result = await _weaponDungeonService.ExecuteAsync();
        return CommonApiResponse.Success("무기 던전이 완료되었습니다.", result);
    }

    [ApiDoc("보스 던전", "보스 던전을 진행하고 획득 결과를 반환합니다.")]
    [ApiError(typeof(NotFoundException), "플레이어 데이터를 찾을 수 없습니다.")]
    [ApiError(typeof(NotFoundException), "플레이어 재화 데이터를 찾을 수 없습니다.")]
    [ApiError(typeof(NotFoundException), "플레이어 스테이지 데이터를 찾을 수 없습니다.")]
    [ApiError(typeof(NotFoundException), "플레이어 세션 데이터를 찾을 수 없습니다.")]
    [ApiError(typeof(NotFoundException), "직업 기본 스탯 데이터를 찾을 수 없습니다.")]
    [ApiError(typeof(NotFoundException), "스테이지 데이터를 찾을 수 없습니다.")]
    [HttpPost("boss")]
    public async Task<CommonApiResponse<BossDungeonResponse>> Boss()
    {
        var result = await _bossDungeonService.ExecuteAsync();
        return CommonApiResponse.Success("보스 던전이 완료되었습니다.", result);
    }

    [ApiDoc("골드 던전 시작", "골드 던전 티켓을 소모해 골드 던전 런을 시작합니다.")]
    [ApiError(typeof(NotFoundException), "플레이어 데이터를 찾을 수 없습니다.")]
    [ApiError(typeof(NotFoundException), "플레이어 재화 데이터를 찾을 수 없습니다.")]
    [ApiError(typeof(NotFoundException), "플레이어 스테이지 데이터를 찾을 수 없습니다.")]
    [ApiError(typeof(NotFoundException), "플레이어 세션 데이터를 찾을 수 없습니다.")]
    [ApiError(typeof(BadRequestException), "골드 던전 티켓이 부족합니다.")]
    [HttpPost("gold-runs")]
    public async Task<CommonApiResponse<StartGoldDungeonResponse>> StartGoldRun()
    {
        var result = await _goldDungeonRunService.ExecuteAsync();
        return CommonApiResponse.Success("골드 던전이 시작되었습니다.", result);
    }

    [ApiDoc("골드 던전 보상 수령", "진행 중인 골드 던전 런의 결과를 검증하고 보상을 지급합니다.")]
    [ApiError(typeof(NotFoundException), "골드 던전 런을 찾을 수 없습니다.")]
    [ApiError(typeof(NotFoundException), "플레이어 데이터를 찾을 수 없습니다.")]
    [ApiError(typeof(NotFoundException), "플레이어 재화 데이터를 찾을 수 없습니다.")]
    [ApiError(typeof(NotFoundException), "플레이어 스테이지 데이터를 찾을 수 없습니다.")]
    [ApiError(typeof(NotFoundException), "플레이어 세션 데이터를 찾을 수 없습니다.")]
    [ApiError(typeof(ForbiddenException), "접근 권한이 없습니다.")]
    [ApiError(typeof(BadRequestException), "골드 던전 제한 시간이 초과되었습니다.")]
    [ApiError(typeof(BadRequestException), "비정상적인 클릭 횟수입니다.")]
    [ApiError(typeof(BadRequestException), "경과 시간 대비 비정상적인 클릭 횟수입니다.")]
    [HttpPost("gold-runs/{runId}/claim")]
    public async Task<CommonApiResponse<GoldDungeonClaimResponse>> ClaimGoldRun(
        Guid runId,
        [FromBody] GoldDungeonClaimRequest request)
    {
        var result = await _goldDungeonClaimService.ExecuteAsync(runId, request);
        return CommonApiResponse.Success("골드 던전 보상을 수령했습니다.", result);
    }

    [ApiDoc("던전 티켓 조회", "보유한 던전 티켓 정보를 조회합니다.")]
    [HttpGet("tickets")]
    public async Task<CommonApiResponse<DungeonTicketResponse>> GetTickets()
    {
        var result = await _dungeonTicketService.ExecuteAsync();
        return CommonApiResponse.Success("던전 티켓 정보를 조회했습니다.", result);
    }

    [ApiDoc("광고 보상 티켓 수령", "광고 시청 보상으로 골드 던전 티켓을 지급합니다. (일일 1회)")]
    [ApiError(typeof(ConflictException), "오늘 이미 광고 보상을 받았습니다.")]
    [HttpPost("gold-tickets/ad-reward")]
    public async Task<CommonApiResponse<DungeonTicketResponse>> ClaimAdReward()
    {
        var result = await _adRewardService.ExecuteAsync();
        return CommonApiResponse.Success("광고 보상 티켓을 수령했습니다.", result);
    }
}
