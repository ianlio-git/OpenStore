using OpenStore.Api.Common.Contracts;
using OpenStore.Api.Products.Dtos;

namespace OpenStore.Api.Products.Contracts;

public interface IProductService : IChildCrudService<ProductResponse, CreateProductRequest, UpdateProductRequest>
{
    Task<IReadOnlyCollection<ProductResponse>> GetByStorePublicIdAsync(Guid storePublicId, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<ProductResponse>> GetByCategoryPublicIdAsync(Guid storePublicId, Guid categoryPublicId, CancellationToken cancellationToken = default);
}
