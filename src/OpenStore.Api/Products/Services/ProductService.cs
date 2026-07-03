using OpenStore.Api.Categories.Exceptions;
using OpenStore.Api.Categories.Models;
using OpenStore.Api.Common.Contracts;
using OpenStore.Api.Common.Services;
using OpenStore.Api.Products.Contracts;
using OpenStore.Api.Products.Dtos;
using OpenStore.Api.Products.Exceptions;
using OpenStore.Api.Products.Models;
using OpenStore.Api.Stores.Exceptions;
using OpenStore.Api.Stores.Models;
using OpenStore.Api.Tenancy.Models;

namespace OpenStore.Api.Products.Services;

public sealed class ProductService : EntityServiceBase<Product>, IProductService
{
    private readonly IRepository<Store> _storeRepository;
    private readonly IRepository<Category> _categoryRepository;
    private readonly IRepository<TenantMembership> _membershipRepository;
    private readonly ICurrentUserContext _currentUser;

    public ProductService(
        IRepository<Product> productRepository,
        IRepository<Store> storeRepository,
        IRepository<Category> categoryRepository,
        IRepository<TenantMembership> membershipRepository,
        IUnitOfWork unitOfWork,
        ICurrentUserContext currentUser,
        IDateTimeProvider dateTimeProvider)
        : base(productRepository, unitOfWork, dateTimeProvider)
    {
        _storeRepository = storeRepository;
        _categoryRepository = categoryRepository;
        _membershipRepository = membershipRepository;
        _currentUser = currentUser;
    }

    private static ProductResponse MapToResponse(Product product)
    {
        ProductResponse result = new()
        {
            PublicId = product.PublicId,
            Name = product.Name,
            Slug = product.Slug,
            Description = product.Description,
            Price = product.Price,
            CategoryPublicId = null,
            CreatedAtUtc = product.CreatedAtUtc,
            UpdatedAtUtc = product.UpdatedAtUtc
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

    private async Task EnsureProductSlugIsAvailableAsync(long storeId, string slug, CancellationToken cancellationToken)
    {
        bool slugExists = await Repository.AnyAsync(p => p.StoreId == storeId && p.Slug == slug, cancellationToken);

        if (slugExists)
        {
            throw new DuplicateProductSlugException();
        }
    }

    private async Task<long?> ResolveCategoryIdAsync(long storeId, Guid? categoryPublicId, CancellationToken cancellationToken)
    {
        long? resolved;

        if (categoryPublicId is null)
        {
            resolved = null;
        }
        else
        {
            IReadOnlyCollection<Category> matches = await _categoryRepository.FindAsync(c => c.PublicId == categoryPublicId.Value && c.StoreId == storeId && c.IsActive, cancellationToken);

            Category? category = matches.FirstOrDefault() ?? throw new CategoryNotFoundException();
            resolved = category.Id;
        }

        return resolved;
    }

    public async Task<ProductResponse> CreateAsync(Guid parentPublicId, CreateProductRequest request, CancellationToken cancellationToken = default)
    {
        string normalizedSlug = request.Slug.Trim();

        Store store = await GetStoreOrThrowAsync(parentPublicId, cancellationToken);
        await EnsureTenantOwnerAsync(store.TenantId, cancellationToken);
        await EnsureProductSlugIsAvailableAsync(store.Id, normalizedSlug, cancellationToken);

        long? categoryId = await ResolveCategoryIdAsync(store.Id, request.CategoryPublicId, cancellationToken);

        Product product = new()
        {
            TenantId = store.TenantId,
            StoreId = store.Id,
            CategoryId = categoryId,
            Name = request.Name.Trim(),
            Slug = normalizedSlug,
            Description = request.Description?.Trim(),
            Price = request.Price
        };

        Add(product);

        await SaveChangesAsync(cancellationToken);

        ProductResponse result = MapToResponse(product);

        return result;
    }

    public async Task<ProductResponse> GetByPublicIdAsync(Guid parentPublicId, Guid publicId, CancellationToken cancellationToken = default)
    {
        Store store = await GetStoreOrThrowAsync(parentPublicId, cancellationToken);

        Product product = await GetByPublicIdOrThrowAsync(publicId, () => new ProductNotFoundException(), cancellationToken);

        if (product.StoreId != store.Id)
        {
            throw new ProductNotFoundException();
        }

        ProductResponse result = MapToResponse(product);

        return result;
    }

    public async Task<IReadOnlyCollection<ProductResponse>> GetByStorePublicIdAsync(Guid storePublicId, CancellationToken cancellationToken = default)
    {
        Store store = await GetStoreOrThrowAsync(storePublicId, cancellationToken);

        IReadOnlyCollection<Product> products = await Repository.FindAsync(p => p.StoreId == store.Id && p.IsActive, cancellationToken);

        IReadOnlyCollection<ProductResponse> result = [.. products.Select(MapToResponse)];

        return result;
    }

    public async Task<IReadOnlyCollection<ProductResponse>> GetByCategoryPublicIdAsync(Guid storePublicId, Guid categoryPublicId, CancellationToken cancellationToken = default)
    {
        Store store = await GetStoreOrThrowAsync(storePublicId, cancellationToken);

        IReadOnlyCollection<Category> categoryMatches = await _categoryRepository.FindAsync(c => c.PublicId == categoryPublicId && c.StoreId == store.Id && c.IsActive, cancellationToken);

        Category category = categoryMatches.FirstOrDefault() ?? throw new CategoryNotFoundException();

        IReadOnlyCollection<Product> products = await Repository.FindAsync(p => p.StoreId == store.Id && p.CategoryId == category.Id && p.IsActive, cancellationToken);

        IReadOnlyCollection<ProductResponse> result = [.. products.Select(MapToResponse)];

        return result;
    }

    public async Task<ProductResponse> UpdateAsync(Guid parentPublicId, Guid publicId, UpdateProductRequest request, CancellationToken cancellationToken = default)
    {
        Store store = await GetStoreOrThrowAsync(parentPublicId, cancellationToken);

        Product product = await GetByPublicIdOrThrowAsync(publicId, () => new ProductNotFoundException(), cancellationToken);

        if (product.StoreId != store.Id)
        {
            throw new ProductNotFoundException();
        }

        await EnsureTenantOwnerAsync(store.TenantId, cancellationToken);

        string normalizedSlug = request.Slug.Trim();

        bool slugChanged = !string.Equals(product.Slug, normalizedSlug, StringComparison.Ordinal);

        if (slugChanged)
        {
            await EnsureProductSlugIsAvailableAsync(store.Id, normalizedSlug, cancellationToken);
        }

        long? categoryId = await ResolveCategoryIdAsync(store.Id, request.CategoryPublicId, cancellationToken);

        product.Name = request.Name.Trim();
        product.Slug = normalizedSlug;
        product.Description = request.Description?.Trim();
        product.Price = request.Price;
        product.CategoryId = categoryId;

        Update(product);

        await SaveChangesAsync(cancellationToken);

        ProductResponse result = MapToResponse(product);

        return result;
    }

    public async Task DeleteAsync(Guid parentPublicId, Guid publicId, CancellationToken cancellationToken = default)
    {
        Store store = await GetStoreOrThrowAsync(parentPublicId, cancellationToken);

        Product product = await GetByPublicIdOrThrowAsync(publicId, () => new ProductNotFoundException(), cancellationToken);

        if (product.StoreId != store.Id)
        {
            throw new ProductNotFoundException();
        }

        await EnsureTenantOwnerAsync(store.TenantId, cancellationToken);

        product.MarkAsDeleted(DateTimeProvider.UtcNow);

        Update(product);

        await SaveChangesAsync(cancellationToken);
    }
}
