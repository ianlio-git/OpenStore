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
    private readonly TenantService _service;


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

        _tenantRepository.AnyAsync(Arg.Any<System.Linq.Expressions.Expression<Func<Tenant, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(false);

        Tenant? capturedTenant = null;

        _tenantRepository.When(x => x.Add(Arg.Any<Tenant>()))
            .Do(callInfo => capturedTenant = callInfo.Arg<Tenant>());

        _unitOfWork.When(x => x.SaveChangesAsync(Arg.Any<CancellationToken>()))
            .Do(_ =>
            {
                if (capturedTenant is not null && capturedTenant.Id == 0)
                {
                    capturedTenant.Id = 42;
                    capturedTenant.PublicId = Guid.NewGuid();
                }
            });

        CreateTenantResponse result = await _service.CreateAsync(request, TestContext.Current.CancellationToken);

        Assert.NotEqual(Guid.Empty, result.PublicId);
        Assert.Equal("My Tenant", result.Name);
        Assert.Equal("my-tenant", result.Slug);

        _tenantRepository.Received(1).Add(Arg.Is<Tenant>(t => t.Name == "My Tenant" && t.Slug == "my-tenant"));

        _membershipRepository.Received(1).Add(Arg.Is<TenantMembership>(tm =>
            tm.TenantId == 42 &&
            tm.UserId == userId &&
            tm.Role == "Owner"));

        await _unitOfWork.Received(2).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_DuplicateSlug_ThrowsDuplicateTenantSlugException()
    {
        CreateTenantRequest request = new()
        {
            Name = "My Tenant",
            Slug = "duplicate-slug"
        };

        _tenantRepository.AnyAsync(Arg.Any<System.Linq.Expressions.Expression<Func<Tenant, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(true);

        await Assert.ThrowsAsync<DuplicateTenantSlugException>(() => _service.CreateAsync(request, TestContext.Current.CancellationToken));

        _tenantRepository.DidNotReceive().Add(Arg.Any<Tenant>());
        _membershipRepository.DidNotReceive().Add(Arg.Any<TenantMembership>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
