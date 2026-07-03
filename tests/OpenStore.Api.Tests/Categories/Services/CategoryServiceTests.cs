using NSubstitute;
using OpenStore.Api.Categories.Contracts;
using OpenStore.Api.Categories.Dtos;
using OpenStore.Api.Categories.Exceptions;
using OpenStore.Api.Categories.Models;
using OpenStore.Api.Categories.Services;
using OpenStore.Api.Common.Contracts;
using OpenStore.Api.Stores.Exceptions;
using OpenStore.Api.Stores.Models;
using OpenStore.Api.Tenancy.Models;

namespace OpenStore.Api.Tests.Categories.Services;

public sealed class CategoryServiceTests
{
    private readonly IRepository<Category> _categoryRepository;
    private readonly IRepository<Store> _storeRepository;
    private readonly IRepository<TenantMembership> _membershipRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserContext _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ICategoryService _service;

    private static readonly DateTimeOffset FixedUtcNow = new(2026, 6, 30, 12, 0, 0, TimeSpan.Zero);

    public CategoryServiceTests()
    {
        _categoryRepository = Substitute.For<IRepository<Category>>();
        _storeRepository = Substitute.For<IRepository<Store>>();
        _membershipRepository = Substitute.For<IRepository<TenantMembership>>();
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _currentUser = Substitute.For<ICurrentUserContext>();
        _dateTimeProvider = Substitute.For<IDateTimeProvider>();

        _currentUser.IsAuthenticated.Returns(true);
        _currentUser.GetRequiredUserId().Returns(Guid.NewGuid());
        _dateTimeProvider.UtcNow.Returns(FixedUtcNow);

        _service = new CategoryService(_categoryRepository, _storeRepository, _membershipRepository, _unitOfWork, _currentUser, _dateTimeProvider);
    }

    [Fact]
    public async Task CreateAsync_ValidRequest_CreatesCategory()
    {
        Guid storePublicId = Guid.NewGuid();
        long storeId = 42;
        long tenantId = 10;
        Guid userId = Guid.NewGuid();

        CreateCategoryRequest request = new()
        {
            Name = "Electronics",
            Slug = "electronics"
        };

        _currentUser.GetRequiredUserId().Returns(userId);

        _storeRepository.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<Store, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new List<Store>
            {
                new() { Id = storeId, PublicId = storePublicId, TenantId = tenantId, Name = "My Store", Slug = "my-store", IsActive = true }
            });

        _membershipRepository.AnyAsync(Arg.Any<System.Linq.Expressions.Expression<Func<TenantMembership, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(true);

        _categoryRepository.AnyAsync(Arg.Any<System.Linq.Expressions.Expression<Func<Category, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(false);

        Category? capturedCategory = null;

        _categoryRepository.When(x => x.Add(Arg.Any<Category>()))
            .Do(callInfo => capturedCategory = callInfo.Arg<Category>());

        _unitOfWork.When(x => x.SaveChangesAsync(Arg.Any<CancellationToken>()))
            .Do(_ =>
            {
                if (capturedCategory is not null && capturedCategory.Id == 0)
                {
                    capturedCategory.Id = 100;
                    capturedCategory.PublicId = Guid.NewGuid();
                }
            });

        CategoryResponse result = await _service.CreateAsync(storePublicId, request, TestContext.Current.CancellationToken);

        Assert.NotEqual(Guid.Empty, result.PublicId);
        Assert.Equal("Electronics", result.Name);
        Assert.Equal("electronics", result.Slug);

        _categoryRepository.Received(1).Add(Arg.Is<Category>(c =>
            c.StoreId == storeId &&
            c.TenantId == tenantId &&
            c.Name == "Electronics" &&
            c.Slug == "electronics"));

        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_StoreNotFound_ThrowsStoreNotFoundException()
    {
        _storeRepository.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<Store, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new List<Store>());

        CreateCategoryRequest request = new()
        {
            Name = "Electronics",
            Slug = "electronics"
        };

        await Assert.ThrowsAsync<StoreNotFoundException>(() =>
            _service.CreateAsync(Guid.NewGuid(), request, TestContext.Current.CancellationToken));

        _categoryRepository.DidNotReceive().Add(Arg.Any<Category>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_NotOwner_ThrowsTenantOwnerRequiredException()
    {
        Guid storePublicId = Guid.NewGuid();

        _storeRepository.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<Store, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new List<Store>
            {
                new() { Id = 42, PublicId = storePublicId, TenantId = 10, Name = "My Store", Slug = "my-store", IsActive = true }
            });

        _membershipRepository.AnyAsync(Arg.Any<System.Linq.Expressions.Expression<Func<TenantMembership, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(false);

        CreateCategoryRequest request = new()
        {
            Name = "Electronics",
            Slug = "electronics"
        };

        await Assert.ThrowsAsync<TenantOwnerRequiredException>(() =>
            _service.CreateAsync(storePublicId, request, TestContext.Current.CancellationToken));

        _categoryRepository.DidNotReceive().Add(Arg.Any<Category>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_DuplicateSlug_ThrowsDuplicateCategorySlugException()
    {
        Guid storePublicId = Guid.NewGuid();

        _storeRepository.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<Store, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new List<Store>
            {
                new() { Id = 42, PublicId = storePublicId, TenantId = 10, Name = "My Store", Slug = "my-store", IsActive = true }
            });

        _membershipRepository.AnyAsync(Arg.Any<System.Linq.Expressions.Expression<Func<TenantMembership, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(true);

        _categoryRepository.AnyAsync(Arg.Any<System.Linq.Expressions.Expression<Func<Category, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(true);

        CreateCategoryRequest request = new()
        {
            Name = "Electronics",
            Slug = "duplicate-slug"
        };

        await Assert.ThrowsAsync<DuplicateCategorySlugException>(() =>
            _service.CreateAsync(storePublicId, request, TestContext.Current.CancellationToken));

        _categoryRepository.DidNotReceive().Add(Arg.Any<Category>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetByPublicIdAsync_CategoryFound_ReturnsResponse()
    {
        Guid storePublicId = Guid.NewGuid();
        Guid categoryPublicId = Guid.NewGuid();

        _storeRepository.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<Store, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new List<Store>
            {
                new() { Id = 42, PublicId = storePublicId, TenantId = 10, Name = "My Store", Slug = "my-store", IsActive = true }
            });

        Category category = new()
        {
            Id = 1,
            PublicId = categoryPublicId,
            StoreId = 42,
            TenantId = 10,
            Name = "Electronics",
            Slug = "electronics",
            CreatedAtUtc = FixedUtcNow
        };

        _categoryRepository.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<Category, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new List<Category> { category });

        CategoryResponse result = await _service.GetByPublicIdAsync(storePublicId, categoryPublicId, TestContext.Current.CancellationToken);

        Assert.Equal(categoryPublicId, result.PublicId);
        Assert.Equal("Electronics", result.Name);
        Assert.Equal("electronics", result.Slug);
    }

    [Fact]
    public async Task GetByPublicIdAsync_CategoryNotFound_ThrowsCategoryNotFoundException()
    {
        Guid storePublicId = Guid.NewGuid();

        _storeRepository.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<Store, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new List<Store>
            {
                new() { Id = 42, PublicId = storePublicId, TenantId = 10, Name = "My Store", Slug = "my-store", IsActive = true }
            });

        _categoryRepository.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<Category, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new List<Category>());

        await Assert.ThrowsAsync<CategoryNotFoundException>(() =>
            _service.GetByPublicIdAsync(storePublicId, Guid.NewGuid(), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task GetByPublicIdAsync_CategoryFromDifferentStore_ThrowsCategoryNotFoundException()
    {
        Guid storePublicId = Guid.NewGuid();
        Guid categoryPublicId = Guid.NewGuid();

        _storeRepository.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<Store, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new List<Store>
            {
                new() { Id = 42, PublicId = storePublicId, TenantId = 10, Name = "My Store", Slug = "my-store", IsActive = true }
            });

        Category category = new()
        {
            Id = 1,
            PublicId = categoryPublicId,
            StoreId = 99,
            TenantId = 10,
            Name = "Electronics",
            Slug = "electronics"
        };

        _categoryRepository.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<Category, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new List<Category> { category });

        await Assert.ThrowsAsync<CategoryNotFoundException>(() =>
            _service.GetByPublicIdAsync(storePublicId, categoryPublicId, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task GetByStorePublicIdAsync_ReturnsActiveCategories()
    {
        Guid storePublicId = Guid.NewGuid();

        _storeRepository.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<Store, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new List<Store>
            {
                new() { Id = 42, PublicId = storePublicId, TenantId = 10, Name = "My Store", Slug = "my-store", IsActive = true }
            });

        List<Category> categories =
        [
            new() { Id = 1, PublicId = Guid.NewGuid(), StoreId = 42, TenantId = 10, Name = "Cat 1", Slug = "cat-1", IsActive = true },
            new() { Id = 2, PublicId = Guid.NewGuid(), StoreId = 42, TenantId = 10, Name = "Cat 2", Slug = "cat-2", IsActive = true }
        ];

        _categoryRepository.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<Category, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(categories);

        IReadOnlyCollection<CategoryResponse> result = await _service.GetByStorePublicIdAsync(storePublicId, TestContext.Current.CancellationToken);

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task UpdateAsync_ValidRequest_UpdatesCategory()
    {
        Guid storePublicId = Guid.NewGuid();
        Guid categoryPublicId = Guid.NewGuid();
        Guid userId = Guid.NewGuid();

        _storeRepository.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<Store, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new List<Store>
            {
                new() { Id = 42, PublicId = storePublicId, TenantId = 10, Name = "My Store", Slug = "my-store", IsActive = true }
            });

        Category category = new()
        {
            Id = 1,
            PublicId = categoryPublicId,
            StoreId = 42,
            TenantId = 10,
            Name = "Old Name",
            Slug = "old-slug",
            CreatedAtUtc = FixedUtcNow
        };

        _currentUser.GetRequiredUserId().Returns(userId);

        _categoryRepository.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<Category, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new List<Category> { category });

        _membershipRepository.AnyAsync(Arg.Any<System.Linq.Expressions.Expression<Func<TenantMembership, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(true);

        _categoryRepository.AnyAsync(Arg.Any<System.Linq.Expressions.Expression<Func<Category, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(false);

        UpdateCategoryRequest request = new()
        {
            Name = "New Name",
            Slug = "new-slug"
        };

        CategoryResponse result = await _service.UpdateAsync(storePublicId, categoryPublicId, request, TestContext.Current.CancellationToken);

        Assert.Equal("New Name", result.Name);
        Assert.Equal("new-slug", result.Slug);

        _categoryRepository.Received(1).Update(Arg.Is<Category>(c =>
            c.Name == "New Name" &&
            c.Slug == "new-slug"));

        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_CategoryNotFound_ThrowsCategoryNotFoundException()
    {
        Guid storePublicId = Guid.NewGuid();

        _storeRepository.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<Store, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new List<Store>
            {
                new() { Id = 42, PublicId = storePublicId, TenantId = 10, Name = "My Store", Slug = "my-store", IsActive = true }
            });

        _categoryRepository.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<Category, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new List<Category>());

        UpdateCategoryRequest request = new()
        {
            Name = "New Name",
            Slug = "new-slug"
        };

        await Assert.ThrowsAsync<CategoryNotFoundException>(() =>
            _service.UpdateAsync(storePublicId, Guid.NewGuid(), request, TestContext.Current.CancellationToken));

        _categoryRepository.DidNotReceive().Update(Arg.Any<Category>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_NotOwner_ThrowsTenantOwnerRequiredException()
    {
        Guid storePublicId = Guid.NewGuid();
        Guid categoryPublicId = Guid.NewGuid();

        _storeRepository.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<Store, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new List<Store>
            {
                new() { Id = 42, PublicId = storePublicId, TenantId = 10, Name = "My Store", Slug = "my-store", IsActive = true }
            });

        Category category = new()
        {
            Id = 1,
            PublicId = categoryPublicId,
            StoreId = 42,
            TenantId = 10,
            Name = "Old Name",
            Slug = "old-slug"
        };

        _categoryRepository.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<Category, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new List<Category> { category });

        _membershipRepository.AnyAsync(Arg.Any<System.Linq.Expressions.Expression<Func<TenantMembership, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(false);

        UpdateCategoryRequest request = new()
        {
            Name = "New Name",
            Slug = "new-slug"
        };

        await Assert.ThrowsAsync<TenantOwnerRequiredException>(() =>
            _service.UpdateAsync(storePublicId, categoryPublicId, request, TestContext.Current.CancellationToken));

        _categoryRepository.DidNotReceive().Update(Arg.Any<Category>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_DuplicateSlug_ThrowsDuplicateCategorySlugException()
    {
        Guid storePublicId = Guid.NewGuid();
        Guid categoryPublicId = Guid.NewGuid();

        _storeRepository.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<Store, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new List<Store>
            {
                new() { Id = 42, PublicId = storePublicId, TenantId = 10, Name = "My Store", Slug = "my-store", IsActive = true }
            });

        Category category = new()
        {
            Id = 1,
            PublicId = categoryPublicId,
            StoreId = 42,
            TenantId = 10,
            Name = "Old Name",
            Slug = "old-slug"
        };

        _categoryRepository.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<Category, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new List<Category> { category });

        _membershipRepository.AnyAsync(Arg.Any<System.Linq.Expressions.Expression<Func<TenantMembership, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(true);

        _categoryRepository.AnyAsync(Arg.Any<System.Linq.Expressions.Expression<Func<Category, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(true);

        UpdateCategoryRequest request = new()
        {
            Name = "New Name",
            Slug = "duplicate-slug"
        };

        await Assert.ThrowsAsync<DuplicateCategorySlugException>(() =>
            _service.UpdateAsync(storePublicId, categoryPublicId, request, TestContext.Current.CancellationToken));

        _categoryRepository.DidNotReceive().Update(Arg.Any<Category>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_SameSlug_DoesNotCheckDuplicate()
    {
        Guid storePublicId = Guid.NewGuid();
        Guid categoryPublicId = Guid.NewGuid();
        Guid userId = Guid.NewGuid();

        _storeRepository.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<Store, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new List<Store>
            {
                new() { Id = 42, PublicId = storePublicId, TenantId = 10, Name = "My Store", Slug = "my-store", IsActive = true }
            });

        Category category = new()
        {
            Id = 1,
            PublicId = categoryPublicId,
            StoreId = 42,
            TenantId = 10,
            Name = "Old Name",
            Slug = "same-slug",
            CreatedAtUtc = FixedUtcNow
        };

        _currentUser.GetRequiredUserId().Returns(userId);

        _categoryRepository.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<Category, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new List<Category> { category });

        _membershipRepository.AnyAsync(Arg.Any<System.Linq.Expressions.Expression<Func<TenantMembership, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(true);

        UpdateCategoryRequest request = new()
        {
            Name = "New Name",
            Slug = "same-slug"
        };

        CategoryResponse result = await _service.UpdateAsync(storePublicId, categoryPublicId, request, TestContext.Current.CancellationToken);

        Assert.Equal("New Name", result.Name);
        Assert.Equal("same-slug", result.Slug);

        _categoryRepository.Received(1).Update(Arg.Any<Category>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteAsync_SoftDeletesCategory()
    {
        Guid storePublicId = Guid.NewGuid();
        Guid categoryPublicId = Guid.NewGuid();
        Guid userId = Guid.NewGuid();

        _storeRepository.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<Store, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new List<Store>
            {
                new() { Id = 42, PublicId = storePublicId, TenantId = 10, Name = "My Store", Slug = "my-store", IsActive = true }
            });

        Category category = new()
        {
            Id = 1,
            PublicId = categoryPublicId,
            StoreId = 42,
            TenantId = 10,
            Name = "Electronics",
            Slug = "electronics"
        };

        _currentUser.GetRequiredUserId().Returns(userId);

        _categoryRepository.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<Category, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new List<Category> { category });

        _membershipRepository.AnyAsync(Arg.Any<System.Linq.Expressions.Expression<Func<TenantMembership, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(true);

        await _service.DeleteAsync(storePublicId, categoryPublicId, TestContext.Current.CancellationToken);

        _categoryRepository.Received(1).Update(Arg.Is<Category>(c =>
            c.IsActive == false &&
            c.DeletedAtUtc == FixedUtcNow));

        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteAsync_CategoryNotFound_ThrowsCategoryNotFoundException()
    {
        Guid storePublicId = Guid.NewGuid();

        _storeRepository.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<Store, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new List<Store>
            {
                new() { Id = 42, PublicId = storePublicId, TenantId = 10, Name = "My Store", Slug = "my-store", IsActive = true }
            });

        _categoryRepository.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<Category, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new List<Category>());

        await Assert.ThrowsAsync<CategoryNotFoundException>(() =>
            _service.DeleteAsync(storePublicId, Guid.NewGuid(), TestContext.Current.CancellationToken));

        _categoryRepository.DidNotReceive().Update(Arg.Any<Category>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteAsync_NotOwner_ThrowsTenantOwnerRequiredException()
    {
        Guid storePublicId = Guid.NewGuid();
        Guid categoryPublicId = Guid.NewGuid();

        _storeRepository.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<Store, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new List<Store>
            {
                new() { Id = 42, PublicId = storePublicId, TenantId = 10, Name = "My Store", Slug = "my-store", IsActive = true }
            });

        Category category = new()
        {
            Id = 1,
            PublicId = categoryPublicId,
            StoreId = 42,
            TenantId = 10,
            Name = "Electronics",
            Slug = "electronics"
        };

        _categoryRepository.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<Category, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new List<Category> { category });

        _membershipRepository.AnyAsync(Arg.Any<System.Linq.Expressions.Expression<Func<TenantMembership, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(false);

        await Assert.ThrowsAsync<TenantOwnerRequiredException>(() =>
            _service.DeleteAsync(storePublicId, categoryPublicId, TestContext.Current.CancellationToken));

        _categoryRepository.DidNotReceive().Update(Arg.Any<Category>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
