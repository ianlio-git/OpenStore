using System.ComponentModel.DataAnnotations;
using OpenStore.Api.Common.Validation;

namespace OpenStore.Api.Tests.Common.Validation;

public sealed class SlugAttributeTests
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
    public void IsValid_ValidSlug_ReturnsSuccess()
    {
        SlugAttribute attribute = new();

        ValidationResult? result = attribute.GetValidationResult("my-slug", CreateContext(new object(), "Slug"));

        Assert.Equal(ValidationResult.Success, result);
    }

    [Fact]
    public void IsValid_SlugWithDigits_ReturnsSuccess()
    {
        SlugAttribute attribute = new();

        ValidationResult? result = attribute.GetValidationResult("my-slug-123", CreateContext(new object(), "Slug"));

        Assert.Equal(ValidationResult.Success, result);
    }

    [Fact]
    public void IsValid_SlugIsNull_ReturnsError()
    {
        SlugAttribute attribute = new();

        ValidationResult? result = attribute.GetValidationResult(null, CreateContext(new object(), "Slug"));

        Assert.NotNull(result);
        Assert.NotEqual(ValidationResult.Success, result);
    }

    [Fact]
    public void IsValid_SlugIsEmpty_ReturnsError()
    {
        SlugAttribute attribute = new();

        ValidationResult? result = attribute.GetValidationResult("", CreateContext(new object(), "Slug"));

        Assert.NotNull(result);
        Assert.NotEqual(ValidationResult.Success, result);
    }

    [Fact]
    public void IsValid_SlugIsWhitespace_ReturnsError()
    {
        SlugAttribute attribute = new();

        ValidationResult? result = attribute.GetValidationResult("   ", CreateContext(new object(), "Slug"));

        Assert.NotNull(result);
        Assert.NotEqual(ValidationResult.Success, result);
    }

    [Fact]
    public void IsValid_SlugExceedsMaxLength_ReturnsError()
    {
        SlugAttribute attribute = new();

        ValidationResult? result = attribute.GetValidationResult(new string('a', 101), CreateContext(new object(), "Slug"));

        Assert.NotNull(result);
        Assert.NotEqual(ValidationResult.Success, result);
    }

    [Fact]
    public void IsValid_SlugContainsUppercase_ReturnsError()
    {
        SlugAttribute attribute = new();

        ValidationResult? result = attribute.GetValidationResult("My-Slug", CreateContext(new object(), "Slug"));

        Assert.NotNull(result);
        Assert.NotEqual(ValidationResult.Success, result);
    }

    [Fact]
    public void IsValid_SlugContainsSpaces_ReturnsError()
    {
        SlugAttribute attribute = new();

        ValidationResult? result = attribute.GetValidationResult("my slug", CreateContext(new object(), "Slug"));

        Assert.NotNull(result);
        Assert.NotEqual(ValidationResult.Success, result);
    }

    [Fact]
    public void IsValid_SlugContainsSpecialCharacters_ReturnsError()
    {
        SlugAttribute attribute = new();

        ValidationResult? result = attribute.GetValidationResult("my_slug!", CreateContext(new object(), "Slug"));

        Assert.NotNull(result);
        Assert.NotEqual(ValidationResult.Success, result);
    }

    [Fact]
    public void IsValid_NotAString_ReturnsError()
    {
        SlugAttribute attribute = new();

        ValidationResult? result = attribute.GetValidationResult(123, CreateContext(new object(), "Slug"));

        Assert.NotNull(result);
        Assert.NotEqual(ValidationResult.Success, result);
    }
}
