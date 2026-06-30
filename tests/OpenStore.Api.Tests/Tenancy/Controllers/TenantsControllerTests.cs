using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using OpenStore.Api.Tenancy.Contracts;
using OpenStore.Api.Tenancy.Controllers;
using OpenStore.Api.Tenancy.Dtos;

namespace OpenStore.Api.Tests.Tenancy.Controllers;

public sealed class TenantsControllerTests
{
    private readonly ITenantService _service;
    private readonly TenantsController _controller;

    public TenantsControllerTests()
    {
        _service = Substitute.For<ITenantService>();
        _controller = new TenantsController(_service);
    }

    [Fact]
    public async Task Create_ValidRequest_ReturnsCreatedWithResponse()
    {
        CreateTenantRequest request = new()
        {
            Name = "Test Tenant",
            Slug = "test-tenant"
        };

        CreateTenantResponse expected = new()
        {
            Id = Guid.NewGuid(),
            Name = "Test Tenant",
            Slug = "test-tenant",
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        _service.CreateAsync(request, Arg.Any<CancellationToken>()).Returns(expected);

        IActionResult result = await _controller.Create(request, CancellationToken.None);

        CreatedAtActionResult createdResult = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(201, createdResult.StatusCode);
        Assert.Equal(nameof(TenantsController.Create), createdResult.ActionName);

        CreateTenantResponse? body = Assert.IsType<CreateTenantResponse>(createdResult.Value);
        Assert.Equal(expected.Id, body.Id);
        Assert.Equal(expected.Name, body.Name);
        Assert.Equal(expected.Slug, body.Slug);
    }
}
