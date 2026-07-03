using OpenStore.Api.Common.Contracts;
using OpenStore.Api.Common.Services;
using OpenStore.Api.Stores.Contracts;
using OpenStore.Api.Stores.Dtos;
using OpenStore.Api.Stores.Exceptions;
using OpenStore.Api.Stores.Models;
using OpenStore.Api.Tenancy.Models;

namespace OpenStore.Api.Stores.Services;

public sealed class StoreService : EntityServiceBase<Store>, IStoreService
{
    private readonly IRepository<Tenant> _tenantRepository;
    private readonly IRepository<TenantMembership> _membershipRepository;
    private readonly ICurrentUserContext _currentUser;

    public StoreService(
        IRepository<Store> storeRepository,
        IRepository<Tenant> tenantRepository,
        IRepository<TenantMembership> membershipRepository,
        IUnitOfWork unitOfWork,
        ICurrentUserContext currentUser,
        IDateTimeProvider dateTimeProvider)
        : base(storeRepository, unitOfWork, dateTimeProvider)
    {
        _tenantRepository = tenantRepository;
        _membershipRepository = membershipRepository;
        _currentUser = currentUser;
    }

    private static StoreResponse MapToResponse(Store store)
    {
        StoreResponse result = new()
        {
            PublicId = store.PublicId,
            Name = store.Name,
            Slug = store.Slug,
            CreatedAtUtc = store.CreatedAtUtc,
            UpdatedAtUtc = store.UpdatedAtUtc
        };

        return result;
    }

    private async Task<Tenant> GetTenantOrThrowAsync(Guid tenantPublicId, CancellationToken cancellationToken)
    {
        IReadOnlyCollection<Tenant> matches = await _tenantRepository.FindAsync(t => t.PublicId == tenantPublicId, cancellationToken);

        Tenant result = matches.FirstOrDefault() ?? throw new TenantNotFoundException();

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

    private async Task EnsureStoreSlugIsAvailableAsync(long tenantId, string slug, CancellationToken cancellationToken)
    {
        bool slugExists = await Repository.AnyAsync(s => s.TenantId == tenantId && s.Slug == slug, cancellationToken);

        if (slugExists)
        {
            throw new DuplicateStoreSlugException();
        }
    }

    public async Task<StoreResponse> CreateAsync(CreateStoreRequest request, CancellationToken cancellationToken = default)
    {
        string normalizedSlug = request.Slug.Trim();

        Tenant tenant = await GetTenantOrThrowAsync(request.TenantPublicId, cancellationToken);
        await EnsureTenantOwnerAsync(tenant.Id, cancellationToken);
        await EnsureStoreSlugIsAvailableAsync(tenant.Id, normalizedSlug, cancellationToken);

        Store store = new()
        {
            TenantId = tenant.Id,
            Name = request.Name.Trim(),
            Slug = normalizedSlug
        };

        Add(store);

        await SaveChangesAsync(cancellationToken);

        StoreResponse result = MapToResponse(store);

        return result;
    }

    public async Task<StoreResponse> GetByPublicIdAsync(Guid publicId, CancellationToken cancellationToken = default)
    {
        Store store = await GetByPublicIdOrThrowAsync(publicId, () => new StoreNotFoundException(), cancellationToken);

        StoreResponse result = MapToResponse(store);

        return result;
    }

    public async Task<IReadOnlyCollection<StoreResponse>> GetByTenantPublicIdAsync(Guid tenantPublicId, CancellationToken cancellationToken = default)
    {
        Tenant tenant = await GetTenantOrThrowAsync(tenantPublicId, cancellationToken);

        IReadOnlyCollection<Store> stores = await Repository.FindAsync(s => s.TenantId == tenant.Id && s.IsActive, cancellationToken);

        IReadOnlyCollection<StoreResponse> result = [.. stores.Select(MapToResponse)];

        return result;
    }

    public async Task<StoreResponse> UpdateAsync(Guid publicId, UpdateStoreRequest request, CancellationToken cancellationToken = default)
    {
        Store store = await GetByPublicIdOrThrowAsync(publicId, () => new StoreNotFoundException(), cancellationToken);

        await EnsureTenantOwnerAsync(store.TenantId, cancellationToken);

        string normalizedSlug = request.Slug.Trim();

        bool slugChanged = !string.Equals(store.Slug, normalizedSlug, StringComparison.Ordinal);

        if (slugChanged)
        {
            await EnsureStoreSlugIsAvailableAsync(store.TenantId, normalizedSlug, cancellationToken);
        }

        store.Name = request.Name.Trim();
        store.Slug = normalizedSlug;

        Update(store);

        await SaveChangesAsync(cancellationToken);

        StoreResponse result = MapToResponse(store);

        return result;
    }

    public async Task DeleteAsync(Guid publicId, CancellationToken cancellationToken = default)
    {
        Store store = await GetByPublicIdOrThrowAsync(publicId, () => new StoreNotFoundException(), cancellationToken);

        await EnsureTenantOwnerAsync(store.TenantId, cancellationToken);

        store.MarkAsDeleted(DateTimeProvider.UtcNow);

        Update(store);

        await SaveChangesAsync(cancellationToken);
    }
}
