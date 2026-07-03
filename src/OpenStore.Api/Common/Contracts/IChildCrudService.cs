namespace OpenStore.Api.Common.Contracts;

public interface IChildCrudService<TResponse, TCreateRequest, TUpdateRequest>
{
    Task<TResponse> CreateAsync(Guid parentPublicId, TCreateRequest request, CancellationToken cancellationToken = default);

    Task<TResponse> GetByPublicIdAsync(Guid parentPublicId, Guid publicId, CancellationToken cancellationToken = default);

    Task<TResponse> UpdateAsync(Guid parentPublicId, Guid publicId, TUpdateRequest request, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid parentPublicId, Guid publicId, CancellationToken cancellationToken = default);
}
