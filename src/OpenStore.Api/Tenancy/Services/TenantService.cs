using OpenStore.Api.Common.Contracts;
using OpenStore.Api.Tenancy.Contracts;
using OpenStore.Api.Tenancy.Dtos;
using OpenStore.Api.Tenancy.Models;
using OpenStore.Api.Tenancy.Validators;

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
        string normalizedSlug = TenantValidator.Validate(request);

        await TenantValidator.ValidateSlugIsAvailableAsync(normalizedSlug, _tenantRepository, cancellationToken);

        Tenant tenant = new()
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            Slug = normalizedSlug
        };

        _tenantRepository.Add(tenant);

        TenantMembership membership = new()
        {
            TenantId = tenant.Id,
            UserId = _currentUser.GetRequiredUserId(),
            Role = "Owner"
        };

        _membershipRepository.Add(membership);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        CreateTenantResponse result = new()
        {
            Id = tenant.Id,
            Name = tenant.Name,
            Slug = tenant.Slug,
            CreatedAtUtc = tenant.CreatedAtUtc
        };

        return result;
    }
}
