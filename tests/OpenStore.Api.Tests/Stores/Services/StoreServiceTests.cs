using NSubstitute;
using OpenStore.Api.Common.Contracts;
using OpenStore.Api.Stores.Contracts;
using OpenStore.Api.Stores.Dtos;
using OpenStore.Api.Stores.Exceptions;
using OpenStore.Api.Stores.Models;
using OpenStore.Api.Stores.Services;
using OpenStore.Api.Tenancy.Models;

namespace OpenStore.Api.Tests.Stores.Services;

public sealed class StoreServiceTests
{
    private readonly IRepository<Store> _storeRepository;
    private readonly IRepository<Tenant> _tenantRepository;
    private readonly IRepository<TenantMembership> _membershipRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserContext _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IStoreService _service;

    private static readonly DateTimeOffset FixedUtcNow = new(2026, 6, 30, 12, 0, 0, TimeSpan.Zero);

    public StoreServiceTests()
    {
        _storeRepository = Substitute.For<IRepository<Store>>();
        _tenantRepository = Substitute.For<IRepository<Tenant>>();
        _membershipRepository = Substitute.For<IRepository<TenantMembership>>();
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _currentUser = Substitute.For<ICurrentUserContext>();
        _dateTimeProvider = Substitute.For<IDateTimeProvider>();

        _currentUser.IsAuthenticated.Returns(true);
        _currentUser.GetRequiredUserId().Returns(Guid.NewGuid());
        _dateTimeProvider.UtcNow.Returns(FixedUtcNow);

        _service = new StoreService(_storeRepository, _tenantRepository, _membershipRepository, _unitOfWork, _currentUser, _dateTimeProvider);
    }

    [Fact]
    public async Task CreateAsync_ValidRequest_CreatesStoreForOwner()
    {
        Guid tenantPublicId = Guid.NewGuid();
        Guid userId = Guid.NewGuid();
        long tenantId = 42;

        CreateStoreRequest request = new()
        {
            TenantPublicId = tenantPublicId,
            Name = "My Store",
            Slug = "my-store"
        };

        _currentUser.GetRequiredUserId().Returns(userId);

        _tenantRepository.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<Tenant, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new List<Tenant>
            {
                new() { Id = tenantId, PublicId = tenantPublicId, Name = "Test Tenant", Slug = "test-tenant" }
            });

        _membershipRepository.AnyAsync(Arg.Any<System.Linq.Expressions.Expression<Func<TenantMembership, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(true);

        _storeRepository.AnyAsync(Arg.Any<System.Linq.Expressions.Expression<Func<Store, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(false);

        Store? capturedStore = null;

        _storeRepository.When(x => x.Add(Arg.Any<Store>()))
            .Do(callInfo => capturedStore = callInfo.Arg<Store>());

        _unitOfWork.When(x => x.SaveChangesAsync(Arg.Any<CancellationToken>()))
            .Do(_ =>
            {
                if (capturedStore is not null && capturedStore.Id == 0)
                {
                    capturedStore.Id = 100;
                    capturedStore.PublicId = Guid.NewGuid();
                }
            });

        StoreResponse result = await _service.CreateAsync(request, TestContext.Current.CancellationToken);

        Assert.NotEqual(Guid.Empty, result.PublicId);
        Assert.Equal("My Store", result.Name);
        Assert.Equal("my-store", result.Slug);

        _storeRepository.Received(1).Add(Arg.Is<Store>(s =>
            s.TenantId == tenantId &&
            s.Name == "My Store" &&
            s.Slug == "my-store"));

        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_TenantNotFound_ThrowsTenantNotFoundException()
    {
        CreateStoreRequest request = new()
        {
            TenantPublicId = Guid.NewGuid(),
            Name = "My Store",
            Slug = "my-store"
        };

        _tenantRepository.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<Tenant, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new List<Tenant>());

        await Assert.ThrowsAsync<TenantNotFoundException>(() => _service.CreateAsync(request, TestContext.Current.CancellationToken));

        _storeRepository.DidNotReceive().Add(Arg.Any<Store>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_NotOwner_ThrowsTenantOwnerRequiredException()
    {
        Guid tenantPublicId = Guid.NewGuid();

        CreateStoreRequest request = new()
        {
            TenantPublicId = tenantPublicId,
            Name = "My Store",
            Slug = "my-store"
        };

        _tenantRepository.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<Tenant, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new List<Tenant>
            {
                new() { Id = 42, PublicId = tenantPublicId, Name = "Test Tenant", Slug = "test-tenant" }
            });

        _membershipRepository.AnyAsync(Arg.Any<System.Linq.Expressions.Expression<Func<TenantMembership, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(false);

        await Assert.ThrowsAsync<TenantOwnerRequiredException>(() => _service.CreateAsync(request, TestContext.Current.CancellationToken));

        _storeRepository.DidNotReceive().Add(Arg.Any<Store>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_DuplicateSlug_ThrowsDuplicateStoreSlugException()
    {
        Guid tenantPublicId = Guid.NewGuid();

        CreateStoreRequest request = new()
        {
            TenantPublicId = tenantPublicId,
            Name = "My Store",
            Slug = "duplicate-slug"
        };

        _tenantRepository.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<Tenant, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new List<Tenant>
            {
                new() { Id = 42, PublicId = tenantPublicId, Name = "Test Tenant", Slug = "test-tenant" }
            });

        _membershipRepository.AnyAsync(Arg.Any<System.Linq.Expressions.Expression<Func<TenantMembership, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(true);

        _storeRepository.AnyAsync(Arg.Any<System.Linq.Expressions.Expression<Func<Store, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(true);

        await Assert.ThrowsAsync<DuplicateStoreSlugException>(() => _service.CreateAsync(request, TestContext.Current.CancellationToken));

        _storeRepository.DidNotReceive().Add(Arg.Any<Store>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetByPublicIdAsync_StoreFound_ReturnsResponse()
    {
        Guid publicId = Guid.NewGuid();

        Store store = new()
        {
            Id = 1,
            PublicId = publicId,
            TenantId = 42,
            Name = "My Store",
            Slug = "my-store",
            CreatedAtUtc = FixedUtcNow
        };

        _storeRepository.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<Store, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new List<Store> { store });

        StoreResponse result = await _service.GetByPublicIdAsync(publicId, TestContext.Current.CancellationToken);

        Assert.Equal(publicId, result.PublicId);
        Assert.Equal("My Store", result.Name);
        Assert.Equal("my-store", result.Slug);
    }

    [Fact]
    public async Task GetByPublicIdAsync_StoreNotFound_ThrowsStoreNotFoundException()
    {
        _storeRepository.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<Store, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new List<Store>());

        await Assert.ThrowsAsync<StoreNotFoundException>(() =>
            _service.GetByPublicIdAsync(Guid.NewGuid(), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task GetByTenantPublicIdAsync_ReturnsActiveStores()
    {
        Guid tenantPublicId = Guid.NewGuid();

        _tenantRepository.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<Tenant, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new List<Tenant>
            {
                new() { Id = 42, PublicId = tenantPublicId, Name = "Test Tenant", Slug = "test-tenant" }
            });

        List<Store> stores =
        [
            new() { Id = 1, PublicId = Guid.NewGuid(), TenantId = 42, Name = "Store 1", Slug = "store-1", IsActive = true },
            new() { Id = 2, PublicId = Guid.NewGuid(), TenantId = 42, Name = "Store 2", Slug = "store-2", IsActive = true }
        ];

        _storeRepository.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<Store, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(stores);

        IReadOnlyCollection<StoreResponse> result = await _service.GetByTenantPublicIdAsync(tenantPublicId, TestContext.Current.CancellationToken);

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetByTenantPublicIdAsync_TenantNotFound_ThrowsTenantNotFoundException()
    {
        _tenantRepository.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<Tenant, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new List<Tenant>());

        await Assert.ThrowsAsync<TenantNotFoundException>(() =>
            _service.GetByTenantPublicIdAsync(Guid.NewGuid(), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task UpdateAsync_ValidRequest_UpdatesStore()
    {
        Guid publicId = Guid.NewGuid();
        Guid userId = Guid.NewGuid();

        Store store = new()
        {
            Id = 1,
            PublicId = publicId,
            TenantId = 42,
            Name = "Old Name",
            Slug = "old-slug",
            CreatedAtUtc = FixedUtcNow
        };

        UpdateStoreRequest request = new()
        {
            Name = "New Name",
            Slug = "new-slug"
        };

        _currentUser.GetRequiredUserId().Returns(userId);

        _storeRepository.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<Store, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new List<Store> { store });

        _membershipRepository.AnyAsync(Arg.Any<System.Linq.Expressions.Expression<Func<TenantMembership, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(true);

        _storeRepository.AnyAsync(Arg.Any<System.Linq.Expressions.Expression<Func<Store, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(false);

        StoreResponse result = await _service.UpdateAsync(publicId, request, TestContext.Current.CancellationToken);

        Assert.Equal("New Name", result.Name);
        Assert.Equal("new-slug", result.Slug);

        _storeRepository.Received(1).Update(Arg.Is<Store>(s =>
            s.Name == "New Name" &&
            s.Slug == "new-slug"));

        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_StoreNotFound_ThrowsStoreNotFoundException()
    {
        _storeRepository.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<Store, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new List<Store>());

        UpdateStoreRequest request = new()
        {
            Name = "New Name",
            Slug = "new-slug"
        };

        await Assert.ThrowsAsync<StoreNotFoundException>(() =>
            _service.UpdateAsync(Guid.NewGuid(), request, TestContext.Current.CancellationToken));

        _storeRepository.DidNotReceive().Update(Arg.Any<Store>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_NotOwner_ThrowsTenantOwnerRequiredException()
    {
        Guid publicId = Guid.NewGuid();

        Store store = new()
        {
            Id = 1,
            PublicId = publicId,
            TenantId = 42,
            Name = "Old Name",
            Slug = "old-slug"
        };

        _storeRepository.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<Store, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new List<Store> { store });

        _membershipRepository.AnyAsync(Arg.Any<System.Linq.Expressions.Expression<Func<TenantMembership, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(false);

        UpdateStoreRequest request = new()
        {
            Name = "New Name",
            Slug = "new-slug"
        };

        await Assert.ThrowsAsync<TenantOwnerRequiredException>(() =>
            _service.UpdateAsync(publicId, request, TestContext.Current.CancellationToken));

        _storeRepository.DidNotReceive().Update(Arg.Any<Store>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_DuplicateSlug_ThrowsDuplicateStoreSlugException()
    {
        Guid publicId = Guid.NewGuid();

        Store store = new()
        {
            Id = 1,
            PublicId = publicId,
            TenantId = 42,
            Name = "Old Name",
            Slug = "old-slug"
        };

        _storeRepository.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<Store, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new List<Store> { store });

        _membershipRepository.AnyAsync(Arg.Any<System.Linq.Expressions.Expression<Func<TenantMembership, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(true);

        _storeRepository.AnyAsync(Arg.Any<System.Linq.Expressions.Expression<Func<Store, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(true);

        UpdateStoreRequest request = new()
        {
            Name = "New Name",
            Slug = "duplicate-slug"
        };

        await Assert.ThrowsAsync<DuplicateStoreSlugException>(() =>
            _service.UpdateAsync(publicId, request, TestContext.Current.CancellationToken));

        _storeRepository.DidNotReceive().Update(Arg.Any<Store>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_SameSlug_DoesNotCheckDuplicate()
    {
        Guid publicId = Guid.NewGuid();
        Guid userId = Guid.NewGuid();

        Store store = new()
        {
            Id = 1,
            PublicId = publicId,
            TenantId = 42,
            Name = "Old Name",
            Slug = "same-slug",
            CreatedAtUtc = FixedUtcNow
        };

        _currentUser.GetRequiredUserId().Returns(userId);

        _storeRepository.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<Store, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new List<Store> { store });

        _membershipRepository.AnyAsync(Arg.Any<System.Linq.Expressions.Expression<Func<TenantMembership, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(true);

        UpdateStoreRequest request = new()
        {
            Name = "New Name",
            Slug = "same-slug"
        };

        StoreResponse result = await _service.UpdateAsync(publicId, request, TestContext.Current.CancellationToken);

        Assert.Equal("New Name", result.Name);
        Assert.Equal("same-slug", result.Slug);

        _storeRepository.Received(1).Update(Arg.Any<Store>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteAsync_SoftDeletesStore()
    {
        Guid publicId = Guid.NewGuid();
        Guid userId = Guid.NewGuid();

        Store store = new()
        {
            Id = 1,
            PublicId = publicId,
            TenantId = 42,
            Name = "My Store",
            Slug = "my-store"
        };

        _currentUser.GetRequiredUserId().Returns(userId);

        _storeRepository.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<Store, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new List<Store> { store });

        _membershipRepository.AnyAsync(Arg.Any<System.Linq.Expressions.Expression<Func<TenantMembership, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(true);

        await _service.DeleteAsync(publicId, TestContext.Current.CancellationToken);

        _storeRepository.Received(1).Update(Arg.Is<Store>(s =>
            s.IsActive == false &&
            s.DeletedAtUtc == FixedUtcNow));

        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteAsync_StoreNotFound_ThrowsStoreNotFoundException()
    {
        _storeRepository.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<Store, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new List<Store>());

        await Assert.ThrowsAsync<StoreNotFoundException>(() =>
            _service.DeleteAsync(Guid.NewGuid(), TestContext.Current.CancellationToken));

        _storeRepository.DidNotReceive().Update(Arg.Any<Store>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteAsync_NotOwner_ThrowsTenantOwnerRequiredException()
    {
        Guid publicId = Guid.NewGuid();

        Store store = new()
        {
            Id = 1,
            PublicId = publicId,
            TenantId = 42,
            Name = "My Store",
            Slug = "my-store"
        };

        _storeRepository.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<Store, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new List<Store> { store });

        _membershipRepository.AnyAsync(Arg.Any<System.Linq.Expressions.Expression<Func<TenantMembership, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(false);

        await Assert.ThrowsAsync<TenantOwnerRequiredException>(() =>
            _service.DeleteAsync(publicId, TestContext.Current.CancellationToken));

        _storeRepository.DidNotReceive().Update(Arg.Any<Store>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
