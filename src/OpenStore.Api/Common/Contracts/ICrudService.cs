namespace OpenStore.Api.Common.Contracts;

public interface ICrudService<TResponse, TCreateRequest, TUpdateRequest>
{
    Task<TResponse> CreateAsync(TCreateRequest request, CancellationToken cancellationToken = default);

    Task<TResponse> GetByPublicIdAsync(Guid publicId, CancellationToken cancellationToken = default);

    Task<TResponse> UpdateAsync(Guid publicId, TUpdateRequest request, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid publicId, CancellationToken cancellationToken = default);
}
