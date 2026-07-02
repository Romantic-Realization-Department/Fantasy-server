using Fantasy.Server.Domain.Weapon.Dto.Response;
using Fantasy.Server.Domain.Weapon.Service.Interface;
using Gamism.SDK.Core.Network;
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

    [HttpPost("{weaponId:int}/upgrade")]
    public async Task<CommonApiResponse<WeaponUpgradeResponse>> Upgrade([FromRoute] int weaponId)
    {
        var result = await _upgradeWeaponService.ExecuteAsync(weaponId);
        return CommonApiResponse.Success("무기 강화가 완료되었습니다.", result);
    }

    [HttpPost("{weaponId:int}/synthesize")]
    public async Task<CommonApiResponse<WeaponSynthesizeResponse>> Synthesize([FromRoute] int weaponId)
    {
        var result = await _synthesizeWeaponService.ExecuteAsync(weaponId);
        return CommonApiResponse.Success("무기 합성이 완료되었습니다.", result);
    }

    [HttpPost("{weaponId:int}/awaken")]
    public async Task<CommonApiResponse<WeaponAwakenResponse>> Awaken([FromRoute] int weaponId)
    {
        var result = await _awakenWeaponService.ExecuteAsync(weaponId);
        return CommonApiResponse.Success("무기 각성이 완료되었습니다.", result);
    }
}
