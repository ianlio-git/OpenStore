using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OpenStore.Api.Common.Errors;

namespace OpenStore.Api.Tests.Common.Errors;

public sealed class ProblemDetailsBuilderTests
{
    private static HttpContext CreateHttpContext(string path = "/api/test")
    {
        DefaultHttpContext context = new();
        context.Request.Path = new PathString(path);

        return context;
    }

    [Fact]
    public void Build_OpenStoreException_SetsStandardFields()
    {
        HttpContext httpContext = CreateHttpContext();
        OpenStoreException exception = new("test.error", 400, "Test error occurred.");

        ProblemDetails problem = ProblemDetailsBuilder.Build(httpContext, exception);

        Assert.Equal(400, problem.Status);
        Assert.Equal("Bad Request", problem.Title);
        Assert.Equal("Test error occurred.", problem.Detail);
        Assert.Equal("/api/test", problem.Instance);

        Assert.True(problem.Extensions.TryGetValue("errorCode", out object? errorCode));
        Assert.Equal("test.error", errorCode);
    }

    [Fact]
    public void Build_NotFound_MapTitle()
    {
        HttpContext httpContext = CreateHttpContext();
        OpenStoreException exception = new("resource.not_found", 404, "Resource not found.");

        ProblemDetails problem = ProblemDetailsBuilder.Build(httpContext, exception);

        Assert.Equal(404, problem.Status);
        Assert.Equal("Not Found", problem.Title);
    }

    [Fact]
    public void Build_Conflict_MapTitle()
    {
        HttpContext httpContext = CreateHttpContext();
        OpenStoreException exception = new("resource.conflict", 409, "Resource conflict.");

        ProblemDetails problem = ProblemDetailsBuilder.Build(httpContext, exception);

        Assert.Equal(409, problem.Status);
        Assert.Equal("Conflict", problem.Title);
    }

    [Fact]
    public void Build_ModelValidationException_IncludesErrors()
    {
        HttpContext httpContext = CreateHttpContext();
        Dictionary<string, string[]> errors = new()
        {
            ["name"] = ["The Name field is required."],
            ["slug"] = ["Slug must contain only lowercase letters, digits, and hyphens."]
        };

        ModelValidationException exception = new(errors);

        ProblemDetails problem = ProblemDetailsBuilder.Build(httpContext, exception);

        Assert.Equal(400, problem.Status);
        Assert.Equal("validation.failed", problem.Extensions["errorCode"]);

        Assert.True(problem.Extensions.TryGetValue("errors", out object? errorsValue));
        IReadOnlyDictionary<string, string[]>? responseErrors = errorsValue as IReadOnlyDictionary<string, string[]>;
        Assert.NotNull(responseErrors);
        Assert.Equal(2, responseErrors.Count);
        Assert.Equal("The Name field is required.", responseErrors["name"].Single());
    }

    [Fact]
    public void Build_ModelValidationExceptionWithoutErrors_DoesNotIncludeErrorsField()
    {
        HttpContext httpContext = CreateHttpContext();
        Dictionary<string, string[]> errors = new();
        ModelValidationException exception = new(errors);

        ProblemDetails problem = ProblemDetailsBuilder.Build(httpContext, exception);

        Assert.False(problem.Extensions.ContainsKey("errors"));
    }

    [Fact]
    public void Build_UnknownStatusCode_MapsToError()
    {
        HttpContext httpContext = CreateHttpContext();
        OpenStoreException exception = new("custom.error", 499, "Custom error.");

        ProblemDetails problem = ProblemDetailsBuilder.Build(httpContext, exception);

        Assert.Equal(499, problem.Status);
        Assert.Equal("Error", problem.Title);
    }
}
