using Fantasy.Server.Domain.Weapon.Dto.Response;
using Fantasy.Server.Domain.Weapon.Service.Interface;
using Gamism.SDK.Core.Network;
using Gamism.SDK.Extensions.AspNetCore.Exceptions;
using Gamism.SDK.Extensions.AspNetCore.Swagger;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Fantasy.Server.Domain.Weapon.Controller;

[ApiController]
[Route("v1/weapons")]
[Authorize]
[EnableRateLimiting("game")]
public class WeaponController : ControllerBase
{
    private readonly IUpgradeWeaponService _upgradeWeaponService;
    private readonly ISynthesizeWeaponService _synthesizeWeaponService;
    private readonly IAwakenWeaponService _awakenWeaponService;

    public WeaponController(
        IUpgradeWeaponService upgradeWeaponService,
        ISynthesizeWeaponService synthesizeWeaponService,
        IAwakenWeaponService awakenWeaponService)
    {
        _upgradeWeaponService = upgradeWeaponService;
        _synthesizeWeaponService = synthesizeWeaponService;
        _awakenWeaponService = awakenWeaponService;
    }

    [ApiDoc("무기 강화", "재화를 소모해 지정한 무기의 강화 레벨을 올립니다.")]
    [ApiError(typeof(NotFoundException), "플레이어 데이터를 찾을 수 없습니다.")]
    [ApiError(typeof(NotFoundException), "무기 데이터를 찾을 수 없습니다.")]
    [ApiError(typeof(NotFoundException), "보유하지 않은 무기입니다.")]
    [ApiError(typeof(NotFoundException), "강화 비용 데이터를 찾을 수 없습니다.")]
    [ApiError(typeof(NotFoundException), "플레이어 재화 데이터를 찾을 수 없습니다.")]
    [ApiError(typeof(NotFoundException), "플레이어 스테이지 데이터를 찾을 수 없습니다.")]
    [ApiError(typeof(NotFoundException), "플레이어 세션 데이터를 찾을 수 없습니다.")]
    [ApiError(typeof(BadRequestException), "이미 최대 강화 레벨입니다.")]
    [ApiError(typeof(BadRequestException), "재화가 부족합니다.")]
    [HttpPost("{weaponId:int}/upgrade")]
    public async Task<CommonApiResponse<WeaponUpgradeResponse>> Upgrade([FromRoute] int weaponId)
    {
        var result = await _upgradeWeaponService.ExecuteAsync(weaponId);
        return CommonApiResponse.Success("무기 강화가 완료되었습니다.", result);
    }

    [ApiDoc("무기 합성", "재료 무기를 소모해 지정한 무기를 상위 등급으로 합성합니다.")]
    [ApiError(typeof(NotFoundException), "플레이어 데이터를 찾을 수 없습니다.")]
    [ApiError(typeof(NotFoundException), "무기 데이터를 찾을 수 없습니다.")]
    [ApiError(typeof(NotFoundException), "보유하지 않은 무기입니다.")]
    [ApiError(typeof(NotFoundException), "플레이어 재화 데이터를 찾을 수 없습니다.")]
    [ApiError(typeof(NotFoundException), "플레이어 스테이지 데이터를 찾을 수 없습니다.")]
    [ApiError(typeof(NotFoundException), "플레이어 세션 데이터를 찾을 수 없습니다.")]
    [ApiError(typeof(BadRequestException), "합성할 수 없는 무기입니다.")]
    [ApiError(typeof(BadRequestException), "합성 재료가 부족합니다.")]
    [HttpPost("{weaponId:int}/synthesize")]
    public async Task<CommonApiResponse<WeaponSynthesizeResponse>> Synthesize([FromRoute] int weaponId)
    {
        var result = await _synthesizeWeaponService.ExecuteAsync(weaponId);
        return CommonApiResponse.Success("무기 합성이 완료되었습니다.", result);
    }

    [ApiDoc("무기 각성", "재화를 소모해 지정한 무기의 각성 레벨을 올립니다.")]
    [ApiError(typeof(NotFoundException), "플레이어 데이터를 찾을 수 없습니다.")]
    [ApiError(typeof(NotFoundException), "무기 데이터를 찾을 수 없습니다.")]
    [ApiError(typeof(NotFoundException), "보유하지 않은 무기입니다.")]
    [ApiError(typeof(NotFoundException), "각성 비용 데이터를 찾을 수 없습니다.")]
    [ApiError(typeof(NotFoundException), "플레이어 재화 데이터를 찾을 수 없습니다.")]
    [ApiError(typeof(NotFoundException), "플레이어 스테이지 데이터를 찾을 수 없습니다.")]
    [ApiError(typeof(NotFoundException), "플레이어 세션 데이터를 찾을 수 없습니다.")]
    [ApiError(typeof(BadRequestException), "이미 최대 각성 레벨입니다.")]
    [ApiError(typeof(BadRequestException), "각성 재료가 부족합니다.")]
    [ApiError(typeof(BadRequestException), "재화가 부족합니다.")]
    [HttpPost("{weaponId:int}/awaken")]
    public async Task<CommonApiResponse<WeaponAwakenResponse>> Awaken([FromRoute] int weaponId)
    {
        var result = await _awakenWeaponService.ExecuteAsync(weaponId);
        return CommonApiResponse.Success("무기 각성이 완료되었습니다.", result);
    }
}
