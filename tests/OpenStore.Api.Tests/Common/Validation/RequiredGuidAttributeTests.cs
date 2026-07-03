using System.ComponentModel.DataAnnotations;
using OpenStore.Api.Common.Validation;

namespace OpenStore.Api.Tests.Common.Validation;

public sealed class RequiredGuidAttributeTests
{
    private static ValidationContext CreateContext(object instance, string memberName)
    {
        ValidationContext context = new(instance)
        {
            MemberName = memberName
        };

        return context;
    }

    [Fact]
    public void IsValid_ValidGuid_ReturnsSuccess()
    {
        RequiredGuidAttribute attribute = new();

        ValidationResult? result = attribute.GetValidationResult(Guid.NewGuid(), CreateContext(new object(), "TenantPublicId"));

        Assert.Equal(ValidationResult.Success, result);
    }

    [Fact]
    public void IsValid_GuidEmpty_ReturnsError()
    {
        RequiredGuidAttribute attribute = new();

        ValidationResult? result = attribute.GetValidationResult(Guid.Empty, CreateContext(new object(), "TenantPublicId"));

        Assert.NotNull(result);
        Assert.NotEqual(ValidationResult.Success, result);
    }

    [Fact]
    public void IsValid_NullValue_ReturnsSuccess()
    {
        RequiredGuidAttribute attribute = new();

        ValidationResult? result = attribute.GetValidationResult(null, CreateContext(new object(), "TenantPublicId"));

        Assert.Equal(ValidationResult.Success, result);
    }

    [Fact]
    public void IsValid_NonGuidValue_ReturnsSuccess()
    {
        RequiredGuidAttribute attribute = new();

        ValidationResult? result = attribute.GetValidationResult("not-a-guid", CreateContext(new object(), "TenantPublicId"));

        Assert.Equal(ValidationResult.Success, result);
    }

    [Fact]
    public void IsValid_NullableGuidWithValue_ReturnsSuccess()
    {
        RequiredGuidAttribute attribute = new();
        Guid? value = Guid.NewGuid();

        ValidationResult? result = attribute.GetValidationResult(value, CreateContext(new object(), "TenantPublicId"));

        Assert.Equal(ValidationResult.Success, result);
    }

    [Fact]
    public void IsValid_NullableGuidWithEmpty_ReturnsError()
    {
        RequiredGuidAttribute attribute = new();
        Guid? value = Guid.Empty;

        ValidationResult? result = attribute.GetValidationResult(value, CreateContext(new object(), "TenantPublicId"));

        Assert.NotNull(result);
        Assert.NotEqual(ValidationResult.Success, result);
    }
}
