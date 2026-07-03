using Fantasy.Server.Domain.Player.Dto.Request;
using Fantasy.Server.Domain.Player.Dto.Response;
using Fantasy.Server.Domain.Player.Service.Interface;
using Gamism.SDK.Core.Network;
using Gamism.SDK.Extensions.AspNetCore.Exceptions;
using Gamism.SDK.Extensions.AspNetCore.Swagger;
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

    [ApiDoc("플레이어 조회", "현재 계정의 플레이어 데이터를 조회합니다.")]
    [ApiError(typeof(NotFoundException), "플레이어를 찾을 수 없습니다.")]
    [ApiError(typeof(NotFoundException), "플레이어 재화 데이터를 찾을 수 없습니다.")]
    [ApiError(typeof(NotFoundException), "플레이어 스테이지 데이터를 찾을 수 없습니다.")]
    [ApiError(typeof(NotFoundException), "플레이어 세션 데이터를 찾을 수 없습니다.")]
    [HttpGet]
    public async Task<PlayerDataResponse> Get() => await _getPlayerService.ExecuteAsync();

    [ApiDoc("플레이어 생성", "직업을 선택해 플레이어를 생성합니다.")]
    [ApiError(typeof(ConflictException), "이미 플레이어가 존재합니다.")]
    [HttpPost]
    public async Task<CommonApiResponse<PlayerDataResponse>> Create([FromBody] CreatePlayerRequest request)
    {
        var data = await _createPlayerService.ExecuteAsync(request);
        return CommonApiResponse.Created("플레이어가 생성되었습니다.", data);
    }

    [ApiDoc("로드아웃 저장", "장착 무기·액티브 스킬을 저장하고, 저장 시점의 방치 보상을 함께 정산합니다.")]
    [ApiError(typeof(NotFoundException), "플레이어 데이터를 찾을 수 없습니다.")]
    [ApiError(typeof(NotFoundException), "플레이어 재화 데이터를 찾을 수 없습니다.")]
    [ApiError(typeof(NotFoundException), "플레이어 스테이지 데이터를 찾을 수 없습니다.")]
    [ApiError(typeof(NotFoundException), "플레이어 세션 데이터를 찾을 수 없습니다.")]
    [ApiError(typeof(NotFoundException), "스테이지 데이터를 찾을 수 없습니다.")]
    [ApiError(typeof(NotFoundException), "직업 기본 스탯 데이터를 찾을 수 없습니다.")]
    [ApiError(typeof(BadRequestException), "보유하지 않은 무기입니다.")]
    [ApiError(typeof(BadRequestException), "중복된 스킬 ID가 있습니다.")]
    [ApiError(typeof(BadRequestException), "유효하지 않은 스킬입니다.")]
    [ApiError(typeof(BadRequestException), "패시브 스킬은 장착할 수 없습니다.")]
    [ApiError(typeof(BadRequestException), "해금되지 않은 스킬입니다.")]
    [HttpPost("loadout")]
    public async Task<CommonApiResponse<LoadoutResponse>> Loadout([FromBody] LoadoutRequest request)
    {
        var result = await _loadoutService.ExecuteAsync(request);
        return CommonApiResponse.Success("로드아웃이 저장되었습니다.", result);
    }

    [ApiDoc("스킬 해금", "SP를 소모해 직업 스킬을 해금합니다.")]
    [ApiError(typeof(NotFoundException), "플레이어 데이터를 찾을 수 없습니다.")]
    [ApiError(typeof(NotFoundException), "존재하지 않는 스킬입니다.")]
    [ApiError(typeof(NotFoundException), "플레이어 재화 데이터를 찾을 수 없습니다.")]
    [ApiError(typeof(NotFoundException), "플레이어 스테이지 데이터를 찾을 수 없습니다.")]
    [ApiError(typeof(NotFoundException), "플레이어 세션 데이터를 찾을 수 없습니다.")]
    [ApiError(typeof(BadRequestException), "해당 직업의 스킬이 아닙니다.")]
    [ApiError(typeof(BadRequestException), "선행 스킬이 해금되지 않았습니다.")]
    [ApiError(typeof(BadRequestException), "SP가 부족합니다.")]
    [HttpPost("skill/unlock")]
    public async Task<CommonApiResponse<SkillUnlockResponse>> UnlockSkill([FromBody] SkillUnlockRequest request)
    {
        var result = await _skillUnlockService.ExecuteAsync(request);
        return CommonApiResponse.Success("스킬 해금이 처리되었습니다.", result);
    }
}
