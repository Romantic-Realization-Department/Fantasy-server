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
public class PlayerSkillController : ControllerBase
{
    private readonly IUpdatePlayerSkillService _updatePlayerSkillService;

    public PlayerSkillController(IUpdatePlayerSkillService updatePlayerSkillService)
    {
        _updatePlayerSkillService = updatePlayerSkillService;
    }

    [HttpPatch("skill")]
    public async Task<CommonApiResponse> UpdateSkill([FromBody] UpdatePlayerSkillRequest request)
    {
        await _updatePlayerSkillService.ExecuteAsync(request);
        return CommonApiResponse.Success("스킬 정보가 업데이트되었습니다.");
    }
}
