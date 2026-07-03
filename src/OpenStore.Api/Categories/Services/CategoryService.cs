using OpenStore.Api.Categories.Contracts;
using OpenStore.Api.Categories.Dtos;
using OpenStore.Api.Categories.Exceptions;
using OpenStore.Api.Categories.Models;
using OpenStore.Api.Common.Contracts;
using OpenStore.Api.Common.Services;
using OpenStore.Api.Stores.Exceptions;
using OpenStore.Api.Stores.Models;
using OpenStore.Api.Tenancy.Models;

namespace OpenStore.Api.Categories.Services;

public sealed class CategoryService : EntityServiceBase<Category>, ICategoryService
{
    private readonly IRepository<Store> _storeRepository;
    private readonly IRepository<TenantMembership> _membershipRepository;
    private readonly ICurrentUserContext _currentUser;

    public CategoryService(
        IRepository<Category> categoryRepository,
        IRepository<Store> storeRepository,
        IRepository<TenantMembership> membershipRepository,
        IUnitOfWork unitOfWork,
        ICurrentUserContext currentUser,
        IDateTimeProvider dateTimeProvider)
        : base(categoryRepository, unitOfWork, dateTimeProvider)
    {
        _storeRepository = storeRepository;
        _membershipRepository = membershipRepository;
        _currentUser = currentUser;
    }

    private static CategoryResponse MapToResponse(Category category)
    {
        CategoryResponse result = new()
        {
            PublicId = category.PublicId,
            Name = category.Name,
            Slug = category.Slug,
            CreatedAtUtc = category.CreatedAtUtc,
            UpdatedAtUtc = category.UpdatedAtUtc
        };

        return result;
    }

    private async Task<Store> GetStoreOrThrowAsync(Guid parentPublicId, CancellationToken cancellationToken)
    {
        IReadOnlyCollection<Store> matches = await _storeRepository.FindAsync(s => s.PublicId == parentPublicId && s.IsActive, cancellationToken);

        Store result = matches.FirstOrDefault() ?? throw new StoreNotFoundException();

        return result;
    }

    private async Task EnsureTenantOwnerAsync(long tenantId, CancellationToken cancellationToken)
    {
        Guid userId = _currentUser.GetRequiredUserId();

        bool isOwner = await _membershipRepository.AnyAsync(m =>
            m.TenantId == tenantId &&
            m.UserId == userId &&
            m.Role == "Owner", cancellationToken);

        if (!isOwner)
        {
            throw new TenantOwnerRequiredException();
        }
    }

    private async Task EnsureCategorySlugIsAvailableAsync(long storeId, string slug, CancellationToken cancellationToken)
    {
        bool slugExists = await Repository.AnyAsync(c => c.StoreId == storeId && c.Slug == slug, cancellationToken);

        if (slugExists)
        {
            throw new DuplicateCategorySlugException();
        }
    }

    public async Task<CategoryResponse> CreateAsync(Guid parentPublicId, CreateCategoryRequest request, CancellationToken cancellationToken = default)
    {
        string normalizedSlug = request.Slug.Trim();

        Store store = await GetStoreOrThrowAsync(parentPublicId, cancellationToken);
        await EnsureTenantOwnerAsync(store.TenantId, cancellationToken);
        await EnsureCategorySlugIsAvailableAsync(store.Id, normalizedSlug, cancellationToken);

        Category category = new()
        {
            TenantId = store.TenantId,
            StoreId = store.Id,
            Name = request.Name.Trim(),
            Slug = normalizedSlug
        };

        Add(category);

        await SaveChangesAsync(cancellationToken);

        CategoryResponse result = MapToResponse(category);

        return result;
    }

    public async Task<CategoryResponse> GetByPublicIdAsync(Guid parentPublicId, Guid publicId, CancellationToken cancellationToken = default)
    {
        Store store = await GetStoreOrThrowAsync(parentPublicId, cancellationToken);

        Category category = await GetByPublicIdOrThrowAsync(publicId, () => new CategoryNotFoundException(), cancellationToken);

        if (category.StoreId != store.Id)
        {
            throw new CategoryNotFoundException();
        }

        CategoryResponse result = MapToResponse(category);

        return result;
    }

    public async Task<IReadOnlyCollection<CategoryResponse>> GetByStorePublicIdAsync(Guid parentPublicId, CancellationToken cancellationToken = default)
    {
        Store store = await GetStoreOrThrowAsync(parentPublicId, cancellationToken);

        IReadOnlyCollection<Category> categories = await Repository.FindAsync(c => c.StoreId == store.Id && c.IsActive, cancellationToken);

        IReadOnlyCollection<CategoryResponse> result = [.. categories.Select(MapToResponse)];

        return result;
    }

    public async Task<CategoryResponse> UpdateAsync(Guid parentPublicId, Guid publicId, UpdateCategoryRequest request, CancellationToken cancellationToken = default)
    {
        Store store = await GetStoreOrThrowAsync(parentPublicId, cancellationToken);

        Category category = await GetByPublicIdOrThrowAsync(publicId, () => new CategoryNotFoundException(), cancellationToken);

        if (category.StoreId != store.Id)
        {
            throw new CategoryNotFoundException();
        }

        await EnsureTenantOwnerAsync(store.TenantId, cancellationToken);

        string normalizedSlug = request.Slug.Trim();

        bool slugChanged = !string.Equals(category.Slug, normalizedSlug, StringComparison.Ordinal);

        if (slugChanged)
        {
            await EnsureCategorySlugIsAvailableAsync(store.Id, normalizedSlug, cancellationToken);
        }

        category.Name = request.Name.Trim();
        category.Slug = normalizedSlug;

        Update(category);

        await SaveChangesAsync(cancellationToken);

        CategoryResponse result = MapToResponse(category);

        return result;
    }

    public async Task DeleteAsync(Guid parentPublicId, Guid publicId, CancellationToken cancellationToken = default)
    {
        Store store = await GetStoreOrThrowAsync(parentPublicId, cancellationToken);

        Category category = await GetByPublicIdOrThrowAsync(publicId, () => new CategoryNotFoundException(), cancellationToken);

        if (category.StoreId != store.Id)
        {
            throw new CategoryNotFoundException();
        }

        await EnsureTenantOwnerAsync(store.TenantId, cancellationToken);

        category.MarkAsDeleted(DateTimeProvider.UtcNow);

        Update(category);

        await SaveChangesAsync(cancellationToken);
    }
}
