using Microsoft.AspNetCore.Mvc;

namespace Fantasy.Common.Global.Controller;

[ApiController]
[Route("v1/health")]
[Tags("Health")]
public class HealthController : ControllerBase
{
    /// <summary>서버 상태 확인</summary>
    /// <remarks>서버가 정상적으로 작동하는지 확인합니다.</remarks>
    /// <response code="200">서버 정상</response>
    [HttpGet]
    public object CheckHealth()
    {
        return new
        {
            status = "healthy",
            timestamp = DateTime.UtcNow
        };
    }
}
