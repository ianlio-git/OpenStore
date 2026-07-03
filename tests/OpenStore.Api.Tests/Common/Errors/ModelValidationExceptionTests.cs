using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using OpenStore.Api.Common.Errors;

namespace OpenStore.Api.Tests.Common.Errors;

public sealed class ModelValidationExceptionTests
{
    [Fact]
    public void Constructor_SetsProperties()
    {
        Dictionary<string, string[]> errors = new()
        {
            ["name"] = ["Name is required."]
        };

        ModelValidationException ex = new(errors);

        Assert.Equal("validation.failed", ex.ErrorCode);
        Assert.Equal(StatusCodes.Status400BadRequest, ex.StatusCode);
        Assert.Equal("One or more validation errors occurred.", ex.Message);
        Assert.Equal("Name is required.", Assert.Single(ex.Errors["name"]));
    }

    [Fact]
    public void FromModelState_NoErrors_ReturnsEmptyErrors()
    {
        ModelStateDictionary modelState = new();

        ModelValidationException ex = ModelValidationException.FromModelState(modelState);

        Assert.Empty(ex.Errors);
    }

    [Fact]
    public void FromModelState_SingleError_MapsFieldInCamelCase()
    {
        ModelStateDictionary modelState = new();
        modelState.AddModelError("Name", "The Name field is required.");

        ModelValidationException ex = ModelValidationException.FromModelState(modelState);

        string[]? nameErrors = ex.Errors.GetValueOrDefault("name");
        Assert.NotNull(nameErrors);
        Assert.Equal("The Name field is required.", Assert.Single(nameErrors));
    }

    [Fact]
    public void FromModelState_MultipleErrorsPerField_GroupsByField()
    {
        ModelStateDictionary modelState = new();
        modelState.AddModelError("Name", "The Name field is required.");
        modelState.AddModelError("Name", "The Name field must be a string with a maximum length of 200.");

        ModelValidationException ex = ModelValidationException.FromModelState(modelState);

        string[]? nameErrors = ex.Errors.GetValueOrDefault("name");
        Assert.NotNull(nameErrors);
        Assert.Equal(2, nameErrors.Length);
    }

    [Fact]
    public void FromModelState_MultipleFields_IncludesAllInCamelCase()
    {
        ModelStateDictionary modelState = new();
        modelState.AddModelError("Name", "The Name field is required.");
        modelState.AddModelError("Slug", "The Slug field is required.");

        ModelValidationException ex = ModelValidationException.FromModelState(modelState);

        Assert.True(ex.Errors.ContainsKey("name"));
        Assert.True(ex.Errors.ContainsKey("slug"));
    }

    [Fact]
    public void FromModelState_PascalCaseKeys_ConvertedToCamelCase()
    {
        ModelStateDictionary modelState = new();
        modelState.AddModelError("TenantPublicId", "The field is required.");

        ModelValidationException ex = ModelValidationException.FromModelState(modelState);

        Assert.True(ex.Errors.ContainsKey("tenantPublicId"));
    }

    [Fact]
    public void FromValidationResults_NoResults_ReturnsEmptyErrors()
    {
        List<ValidationResult> results = [];

        ModelValidationException ex = ModelValidationException.FromValidationResults(results);

        Assert.Empty(ex.Errors);
    }

    [Fact]
    public void FromValidationResults_SingleResult_MapsMemberNameInCamelCase()
    {
        List<ValidationResult> results =
        [
            new ValidationResult("The Name field is required.", ["Name"])
        ];

        ModelValidationException ex = ModelValidationException.FromValidationResults(results);

        string[]? nameErrors = ex.Errors.GetValueOrDefault("name");
        Assert.NotNull(nameErrors);
        Assert.Equal("The Name field is required.", Assert.Single(nameErrors));
    }

    [Fact]
    public void FromValidationResults_MultipleResultsForSameMember_GroupsByMemberName()
    {
        List<ValidationResult> results =
        [
            new ValidationResult("The Name field is required.", ["Name"]),
            new ValidationResult("The Name field must be a string with a maximum length of 200.", ["Name"])
        ];

        ModelValidationException ex = ModelValidationException.FromValidationResults(results);

        string[]? nameErrors = ex.Errors.GetValueOrDefault("name");
        Assert.NotNull(nameErrors);
        Assert.Equal(2, nameErrors.Length);
    }

    [Fact]
    public void FromValidationResults_EmptyMemberNames_UsesEmptyStringKey()
    {
        List<ValidationResult> results =
        [
            new ValidationResult("A model-level error occurred.")
        ];

        ModelValidationException ex = ModelValidationException.FromValidationResults(results);

        string[]? rootErrors = ex.Errors.GetValueOrDefault("");
        Assert.NotNull(rootErrors);
        Assert.Equal("A model-level error occurred.", Assert.Single(rootErrors));
    }

    [Fact]
    public void FromValidationResults_NullErrorMessage_SkipsResult()
    {
        List<ValidationResult> results =
        [
            new ValidationResult(null)
        ];

        ModelValidationException ex = ModelValidationException.FromValidationResults(results);

        Assert.Empty(ex.Errors);
    }

    [Fact]
    public void FromValidationResults_PascalCaseMemberNames_ConvertedToCamelCase()
    {
        List<ValidationResult> results =
        [
            new ValidationResult("The field is required.", ["TenantPublicId"])
        ];

        ModelValidationException ex = ModelValidationException.FromValidationResults(results);

        Assert.True(ex.Errors.ContainsKey("tenantPublicId"));
    }
}
