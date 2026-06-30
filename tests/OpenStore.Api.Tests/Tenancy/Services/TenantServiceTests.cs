using System.Linq.Expressions;
using NSubstitute;
using OpenStore.Api.Common.Contracts;
using OpenStore.Api.Tenancy.Contracts;
using OpenStore.Api.Tenancy.Dtos;
using OpenStore.Api.Tenancy.Exceptions;
using OpenStore.Api.Tenancy.Models;
using OpenStore.Api.Tenancy.Services;

namespace OpenStore.Api.Tests.Tenancy.Services;

public sealed class TenantServiceTests
{
    private readonly IRepository<Tenant> _tenantRepository;
    private readonly IRepository<TenantMembership> _membershipRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserContext _currentUser;
    private readonly ITenantService _service;

    public TenantServiceTests()
    {
        _tenantRepository = Substitute.For<IRepository<Tenant>>();
        _membershipRepository = Substitute.For<IRepository<TenantMembership>>();
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _currentUser = Substitute.For<ICurrentUserContext>();

        _currentUser.IsAuthenticated.Returns(true);
        _currentUser.GetRequiredUserId().Returns(Guid.NewGuid());

        _service = new TenantService(_tenantRepository, _membershipRepository, _unitOfWork, _currentUser);
    }

    [Fact]
    public async Task CreateAsync_ValidRequest_CreatesTenantAndMembership()
    {
        CreateTenantRequest request = new()
        {
            Name = "My Tenant",
            Slug = "my-tenant"
        };

        Guid userId = Guid.NewGuid();

        _currentUser.GetRequiredUserId().Returns(userId);

        _tenantRepository.AnyAsync(Arg.Any<Expression<Func<Tenant, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(false);

        CreateTenantResponse result = await _service.CreateAsync(request, TestContext.Current.CancellationToken);

        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal("My Tenant", result.Name);
        Assert.Equal("my-tenant", result.Slug);

        _tenantRepository.Received(1).Add(Arg.Is<Tenant>(t => t.Name == "My Tenant" && t.Slug == "my-tenant" && t.Id != Guid.Empty));

        _membershipRepository.Received(1).Add(Arg.Is<TenantMembership>(tm => tm.UserId == userId && tm.Role == "Owner" && tm.TenantId != Guid.Empty));

        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_DuplicateSlug_ThrowsDuplicateTenantSlugException()
    {
        CreateTenantRequest request = new()
        {
            Name = "My Tenant",
            Slug = "duplicate-slug"
        };

        Guid userId = Guid.NewGuid();

        _currentUser.GetRequiredUserId().Returns(userId);

        _tenantRepository.AnyAsync(Arg.Any<Expression<Func<Tenant, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(true);

        await Assert.ThrowsAsync<DuplicateTenantSlugException>(() => _service.CreateAsync(request, TestContext.Current.CancellationToken));

        _tenantRepository.DidNotReceive().Add(Arg.Any<Tenant>());
        _membershipRepository.DidNotReceive().Add(Arg.Any<TenantMembership>());
    }
}
