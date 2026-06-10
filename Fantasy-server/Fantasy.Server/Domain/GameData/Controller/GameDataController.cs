using Fantasy.Server.Domain.GameData.Dto.Response;
using Fantasy.Server.Domain.GameData.Service.Interface;
using Fantasy.Server.Domain.Player.Enum;
using Gamism.SDK.Core.Network;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Fantasy.Server.Domain.GameData.Controller;

[ApiController]
[Route("v1")]
[Authorize]
[EnableRateLimiting("game")]
public class GameDataController : ControllerBase
{
    private readonly IGameDataQueryService _gameDataQueryService;

    public GameDataController(IGameDataQueryService gameDataQueryService)
    {
        _gameDataQueryService = gameDataQueryService;
    }

    [HttpGet("jobs/{jobType}/skills")]
    public async Task<CommonApiResponse<List<SkillDataResponse>>> GetSkills([FromRoute] JobType jobType)
    {
        var result = await _gameDataQueryService.GetSkillsByJobAsync(jobType);
        return CommonApiResponse.Success("스킬 데이터를 불러왔습니다.", result);
    }

    [HttpGet("jobs/{jobType}/weapons")]
    public async Task<CommonApiResponse<List<WeaponDataResponse>>> GetWeapons([FromRoute] JobType jobType)
    {
        var result = await _gameDataQueryService.GetWeaponsByJobAsync(jobType);
        return CommonApiResponse.Success("무기 데이터를 불러왔습니다.", result);
    }

    [HttpGet("levels")]
    public async Task<CommonApiResponse<List<LevelDataResponse>>> GetLevels()
    {
        var result = await _gameDataQueryService.GetLevelsAsync();
        return CommonApiResponse.Success("레벨 테이블을 불러왔습니다.", result);
    }

    [HttpGet("stages")]
    public async Task<CommonApiResponse<List<StageDataResponse>>> GetStages()
    {
        var result = await _gameDataQueryService.GetStagesAsync();
        return CommonApiResponse.Success("스테이지 데이터를 불러왔습니다.", result);
    }
}
