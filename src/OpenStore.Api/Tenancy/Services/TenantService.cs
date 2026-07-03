using OpenStore.Api.Common.Contracts;
using OpenStore.Api.Tenancy.Contracts;
using OpenStore.Api.Tenancy.Dtos;
using OpenStore.Api.Tenancy.Exceptions;
using OpenStore.Api.Tenancy.Models;

namespace OpenStore.Api.Tenancy.Services;

public sealed class TenantService : ITenantService
{
    private readonly IRepository<Tenant> _tenantRepository;
    private readonly IRepository<TenantMembership> _membershipRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserContext _currentUser;

    public TenantService(
        IRepository<Tenant> tenantRepository,
        IRepository<TenantMembership> membershipRepository,
        IUnitOfWork unitOfWork,
        ICurrentUserContext currentUser)
    {
        _tenantRepository = tenantRepository;
        _membershipRepository = membershipRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<CreateTenantResponse> CreateAsync(CreateTenantRequest request, CancellationToken cancellationToken = default)
    {
        string normalizedSlug = request.Slug.Trim();

        await ValidateSlugIsAvailableAsync(normalizedSlug, cancellationToken);

        Guid userId = _currentUser.GetRequiredUserId();

        Tenant tenant = new()
        {
            Name = request.Name.Trim(),
            Slug = normalizedSlug
        };

        _tenantRepository.Add(tenant);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        TenantMembership membership = new()
        {
            TenantId = tenant.Id,
            UserId = userId,
            Role = "Owner"
        };

        _membershipRepository.Add(membership);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        CreateTenantResponse response = new()
        {
            PublicId = tenant.PublicId,
            Name = tenant.Name,
            Slug = tenant.Slug,
            CreatedAtUtc = tenant.CreatedAtUtc
        };

        return response;
    }

    private async Task ValidateSlugIsAvailableAsync(string slug, CancellationToken cancellationToken)
    {
        bool slugExists = await _tenantRepository.AnyAsync(t => t.Slug == slug, cancellationToken);

        if (slugExists)
        {
            throw new DuplicateTenantSlugException();
        }
    }
}
