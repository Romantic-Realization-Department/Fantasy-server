using Fantasy.Server.Domain.Player.Dto.Request;
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
public class PlayerWeaponController : ControllerBase
{
    private readonly IUpdatePlayerWeaponService _updatePlayerWeaponService;

    public PlayerWeaponController(IUpdatePlayerWeaponService updatePlayerWeaponService)
    {
        _updatePlayerWeaponService = updatePlayerWeaponService;
    }

    [HttpPatch("weapon")]
    public async Task<CommonApiResponse> UpdateWeapon([FromBody] UpdatePlayerWeaponRequest request)
    {
        await _updatePlayerWeaponService.ExecuteAsync(request);
        return CommonApiResponse.Success("무기 정보가 업데이트되었습니다.");
    }
}
