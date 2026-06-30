using OpenStore.Api.Tenancy.Dtos;

namespace OpenStore.Api.Tenancy.Contracts;

public interface ITenantService
{
    Task<CreateTenantResponse> CreateAsync(CreateTenantRequest request, CancellationToken cancellationToken = default);
}
