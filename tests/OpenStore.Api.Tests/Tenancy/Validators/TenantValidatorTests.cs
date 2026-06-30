using System.Linq.Expressions;
using NSubstitute;
using OpenStore.Api.Common.Contracts;
using OpenStore.Api.Tenancy.Dtos;
using OpenStore.Api.Tenancy.Exceptions;
using OpenStore.Api.Tenancy.Models;
using OpenStore.Api.Tenancy.Validators;

namespace OpenStore.Api.Tests.Tenancy.Validators;

public sealed class TenantValidatorTests
{
    [Fact]
    public void Validate_NameIsEmpty_ThrowsTenantNameValidationException()
    {
        CreateTenantRequest request = new()
        {
            Name = string.Empty,
            Slug = "my-slug"
        };

        Assert.Throws<TenantNameValidationException>(() => TenantValidator.Validate(request));
    }

    [Fact]
    public void Validate_NameIsWhitespace_ThrowsTenantNameValidationException()
    {
        CreateTenantRequest request = new()
        {
            Name = "   ",
            Slug = "my-slug"
        };

        Assert.Throws<TenantNameValidationException>(() => TenantValidator.Validate(request));
    }

    [Fact]
    public void Validate_NameExceedsMaxLength_ThrowsTenantNameValidationException()
    {
        CreateTenantRequest request = new()
        {
            Name = new string('a', 201),
            Slug = "my-slug"
        };

        Assert.Throws<TenantNameValidationException>(() => TenantValidator.Validate(request));
    }

    [Fact]
    public void Validate_SlugIsEmpty_ThrowsTenantSlugValidationException()
    {
        CreateTenantRequest request = new()
        {
            Name = "My Tenant",
            Slug = string.Empty
        };

        Assert.Throws<TenantSlugValidationException>(() => TenantValidator.Validate(request));
    }

    [Fact]
    public void Validate_SlugIsWhitespace_ThrowsTenantSlugValidationException()
    {
        CreateTenantRequest request = new()
        {
            Name = "My Tenant",
            Slug = "   "
        };

        Assert.Throws<TenantSlugValidationException>(() => TenantValidator.Validate(request));
    }

    [Fact]
    public void Validate_SlugExceedsMaxLength_ThrowsTenantSlugValidationException()
    {
        CreateTenantRequest request = new()
        {
            Name = "My Tenant",
            Slug = new string('a', 101)
        };

        Assert.Throws<TenantSlugValidationException>(() => TenantValidator.Validate(request));
    }

    [Fact]
    public void Validate_SlugContainsUppercase_ThrowsTenantSlugValidationException()
    {
        CreateTenantRequest request = new()
        {
            Name = "My Tenant",
            Slug = "My-Tenant"
        };

        Assert.Throws<TenantSlugValidationException>(() => TenantValidator.Validate(request));
    }

    [Fact]
    public void Validate_SlugContainsSpaces_ThrowsTenantSlugValidationException()
    {
        CreateTenantRequest request = new()
        {
            Name = "My Tenant",
            Slug = "my tenant"
        };

        Assert.Throws<TenantSlugValidationException>(() => TenantValidator.Validate(request));
    }

    [Fact]
    public void Validate_SlugContainsSpecialCharacters_ThrowsTenantSlugValidationException()
    {
        CreateTenantRequest request = new()
        {
            Name = "My Tenant",
            Slug = "my_tenant!"
        };

        Assert.Throws<TenantSlugValidationException>(() => TenantValidator.Validate(request));
    }

    [Fact]
    public void Validate_ValidNameAndSlug_ReturnsNormalizedSlug()
    {
        CreateTenantRequest request = new()
        {
            Name = "My Tenant",
            Slug = "my-tenant"
        };

        string result = TenantValidator.Validate(request);

        Assert.Equal("my-tenant", result);
    }

    [Fact]
    public void Validate_SlugWithWhitespace_ReturnsTrimmedSlug()
    {
        CreateTenantRequest request = new()
        {
            Name = "My Tenant",
            Slug = "  my-tenant  "
        };

        string result = TenantValidator.Validate(request);

        Assert.Equal("my-tenant", result);
    }

    [Fact]
    public async Task ValidateSlugIsAvailableAsync_SlugAvailable_DoesNotThrow()
    {
        IRepository<Tenant> repository = Substitute.For<IRepository<Tenant>>();
        repository.AnyAsync(Arg.Any<Expression<Func<Tenant, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(false);

        await TenantValidator.ValidateSlugIsAvailableAsync("available-slug", repository, CancellationToken.None);
    }

    [Fact]
    public async Task ValidateSlugIsAvailableAsync_SlugTaken_ThrowsDuplicateTenantSlugException()
    {
        IRepository<Tenant> repository = Substitute.For<IRepository<Tenant>>();
        repository.AnyAsync(Arg.Any<Expression<Func<Tenant, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(true);

        await Assert.ThrowsAsync<DuplicateTenantSlugException>(
            () => TenantValidator.ValidateSlugIsAvailableAsync("taken-slug", repository, CancellationToken.None));
    }
}
