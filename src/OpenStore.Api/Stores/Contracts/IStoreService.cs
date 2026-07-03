using OpenStore.Api.Common.Contracts;
using OpenStore.Api.Stores.Dtos;

namespace OpenStore.Api.Stores.Contracts;

public interface IStoreService : ICrudService<StoreResponse, CreateStoreRequest, UpdateStoreRequest>
{
    Task<IReadOnlyCollection<StoreResponse>> GetByTenantPublicIdAsync(Guid tenantPublicId, CancellationToken cancellationToken = default);
}
