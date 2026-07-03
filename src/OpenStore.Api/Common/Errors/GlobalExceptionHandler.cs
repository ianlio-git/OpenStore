using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace OpenStore.Api.Common.Errors;

public sealed class GlobalExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        int status;
        ProblemDetails problem;

        if (exception is OpenStoreException ose)
        {
            status = ose.StatusCode;
            problem = ProblemDetailsBuilder.Build(httpContext, ose);
        }
        else
        {
            status = StatusCodes.Status500InternalServerError;
            problem = new ProblemDetails
            {
                Type = $"https://httpstatuses.io/{status}",
                Title = "Internal Server Error",
                Status = status,
                Detail = exception.Message,
                Instance = httpContext.Request.Path,
                Extensions = new Dictionary<string, object?>
                {
                    ["errorCode"] = "unknown.error"
                }
            };
        }

        httpContext.Response.StatusCode = status;
        httpContext.Response.ContentType = "application/problem+json";

        await Results.Json(problem, statusCode: status).ExecuteAsync(httpContext);

        return true;
    }
}
