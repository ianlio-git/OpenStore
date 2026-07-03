using OpenStore.Api.Categories.Dtos;
using OpenStore.Api.Common.Contracts;

namespace OpenStore.Api.Categories.Contracts;

public interface ICategoryService : IChildCrudService<CategoryResponse, CreateCategoryRequest, UpdateCategoryRequest>
{
    Task<IReadOnlyCollection<CategoryResponse>> GetByStorePublicIdAsync(Guid storePublicId, CancellationToken cancellationToken = default);
}
