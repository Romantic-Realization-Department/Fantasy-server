using Gamism.SDK.Extensions.AspNetCore.Swagger;
using Microsoft.AspNetCore.Mvc;

namespace Fantasy.Server.Global.Controller;

[ApiController]
[Route("v1/health")]
[Tags("Health")]
public class HealthController : ControllerBase
{
    [ApiDoc("서버 상태 확인", "서버가 정상적으로 작동하는지 확인합니다.")]
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
