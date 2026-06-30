using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace OpenStore.Api.Common.Errors;

public sealed class GlobalExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        string errorCode;
        int status;
        string title;

        if (exception is OpenStoreException ose)
        {
            errorCode = ose.ErrorCode;
            status = ose.StatusCode;
            title = status switch
            {
                400 => "Bad Request",
                401 => "Unauthorized",
                404 => "Not Found",
                409 => "Conflict",
                _ => "Error"
            };
        }
        else
        {
            errorCode = "unknown.error";
            status = StatusCodes.Status500InternalServerError;
            title = "Internal Server Error";
        }

        httpContext.Response.StatusCode = status;
        httpContext.Response.ContentType = "application/problem+json";

        ProblemDetails problem = new()
        {
            Type = $"https://httpstatuses.io/{status}",
            Title = title,
            Status = status,
            Detail = exception.Message,
            Instance = httpContext.Request.Path,
            Extensions = new Dictionary<string, object?>
            {
                ["errorCode"] = errorCode
            }
        };

        await Results.Json(problem, statusCode: status).ExecuteAsync(httpContext);

        return true;
    }
}
