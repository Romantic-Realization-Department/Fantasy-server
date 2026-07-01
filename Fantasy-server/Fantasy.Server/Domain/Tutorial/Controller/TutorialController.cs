using Fantasy.Server.Domain.Tutorial.Dto.Response;
using Fantasy.Server.Domain.Tutorial.Service.Interface;
using Gamism.SDK.Core.Network;
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

    [HttpGet]
    public async Task<CommonApiResponse<CompletedTutorialsResponse>> Get()
    {
        var result = await _getCompletedTutorialsService.ExecuteAsync();
        return CommonApiResponse.Success("완료한 튜토리얼 목록을 조회했습니다.", result);
    }

    [HttpPost("{tutorialId}/complete")]
    public async Task<CommonApiResponse<TutorialCompleteResponse>> Complete([FromRoute] string tutorialId)
    {
        var result = await _completeTutorialService.ExecuteAsync(tutorialId);
        return CommonApiResponse.Success("튜토리얼 완료가 처리되었습니다.", result);
    }
}
