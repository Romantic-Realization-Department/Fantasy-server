using Fantasy.Common.Domain.Account.Exception;
using Fantasy.Common.Domain.Auth.Exception;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Fantasy.Common.Global.Exception;

public class GlobalExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        System.Exception exception,
        CancellationToken cancellationToken)
    {
        var (statusCode, title) = exception switch
        {
            InvalidCredentialsException => (StatusCodes.Status401Unauthorized, "Unauthorized"),
            UnauthorizedAccessException => (StatusCodes.Status401Unauthorized, "Unauthorized"),
            DuplicateEmailException => (StatusCodes.Status409Conflict, "Conflict"),
            _ => (0, null)
        };

        if (statusCode == 0) return false;

        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = exception.Message
        }, cancellationToken);

        return true;
    }
}
