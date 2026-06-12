using System.Text;

namespace Fantasy.Server.Global.Security;

public class SwaggerBasicAuthMiddleware
{
    private readonly RequestDelegate _next;
    private readonly string? _password;

    public SwaggerBasicAuthMiddleware(RequestDelegate next, IConfiguration configuration)
    {
        _next = next;
        _password = configuration["Swagger:Password"];
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!context.Request.Path.StartsWithSegments("/swagger"))
        {
            await _next(context);
            return;
        }

        // 비밀번호 미설정 배포에서 Swagger가 무방비 노출되지 않도록 경로 자체를 숨김
        if (string.IsNullOrEmpty(_password))
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }

        if (!IsAuthorized(context.Request.Headers.Authorization.ToString()))
        {
            context.Response.Headers.WWWAuthenticate = "Basic realm=\"swagger\"";
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        await _next(context);
    }

    private bool IsAuthorized(string authorizationHeader)
    {
        if (!authorizationHeader.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase))
            return false;

        string decoded;
        try
        {
            decoded = Encoding.UTF8.GetString(Convert.FromBase64String(authorizationHeader["Basic ".Length..]));
        }
        catch (FormatException)
        {
            return false;
        }

        // "username:password" 형식 — username은 검사하지 않음
        var separatorIndex = decoded.IndexOf(':');
        return separatorIndex >= 0 && decoded[(separatorIndex + 1)..] == _password;
    }
}
