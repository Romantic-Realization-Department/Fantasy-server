using Fantasy.Server.Domain.Tutorial.Dto.Response;
using Fantasy.Server.Domain.Tutorial.Service.Interface;
using Gamism.SDK.Core.Network;
using Gamism.SDK.Extensions.AspNetCore.Exceptions;
using Gamism.SDK.Extensions.AspNetCore.Swagger;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Fantasy.Server.Domain.Tutorial.Controller;

[ApiController]
[Route("v1/tutorials")]
[Authorize]
[EnableRateLimiting("game")]
public class TutorialController : ControllerBase
{
    private readonly ICompleteTutorialService _completeTutorialService;
    private readonly IGetCompletedTutorialsService _getCompletedTutorialsService;

    public TutorialController(
        ICompleteTutorialService completeTutorialService,
        IGetCompletedTutorialsService getCompletedTutorialsService)
    {
        _completeTutorialService = completeTutorialService;
        _getCompletedTutorialsService = getCompletedTutorialsService;
    }

    [ApiDoc("완료 튜토리얼 조회", "현재 플레이어가 완료한 튜토리얼 목록을 조회합니다.")]
    [ApiError(typeof(NotFoundException), "플레이어를 찾을 수 없습니다.")]
    [HttpGet]
    public async Task<CommonApiResponse<CompletedTutorialsResponse>> Get()
    {
        var result = await _getCompletedTutorialsService.ExecuteAsync();
        return CommonApiResponse.Success("완료한 튜토리얼 목록을 조회했습니다.", result);
    }

    [ApiDoc("튜토리얼 완료", "지정한 튜토리얼을 완료 처리하고 보상을 지급합니다.")]
    [ApiError(typeof(NotFoundException), "플레이어를 찾을 수 없습니다.")]
    [ApiError(typeof(BadRequestException), "존재하지 않는 튜토리얼입니다.")]
    [HttpPost("{tutorialId}/complete")]
    public async Task<CommonApiResponse<TutorialCompleteResponse>> Complete([FromRoute] string tutorialId)
    {
        var result = await _completeTutorialService.ExecuteAsync(tutorialId);
        return CommonApiResponse.Success("튜토리얼 완료가 처리되었습니다.", result);
    }
}
