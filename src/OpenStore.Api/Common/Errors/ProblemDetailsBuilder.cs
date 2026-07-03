using Microsoft.AspNetCore.Mvc;

namespace OpenStore.Api.Common.Errors;

public static class ProblemDetailsBuilder
{
    public static ProblemDetails Build(HttpContext httpContext, OpenStoreException exception)
    {
        string title = exception.StatusCode switch
        {
            400 => "Bad Request",
            401 => "Unauthorized",
            404 => "Not Found",
            409 => "Conflict",
            _ => "Error"
        };

        ProblemDetails problem = new()
        {
            Type = $"https://httpstatuses.io/{exception.StatusCode}",
            Title = title,
            Status = exception.StatusCode,
            Detail = exception.Message,
            Instance = httpContext.Request.Path,
            Extensions = new Dictionary<string, object?>
            {
                ["errorCode"] = exception.ErrorCode
            }
        };

        if (exception is ModelValidationException mve && mve.Errors.Count > 0)
        {
            problem.Extensions["errors"] = mve.Errors;
        }

        return problem;
    }
}
