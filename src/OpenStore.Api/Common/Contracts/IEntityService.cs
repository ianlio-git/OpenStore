namespace OpenStore.Api.Common.Contracts;

public interface IEntityService<TEntity> where TEntity : class
{
    Task<TEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<TEntity>> GetAllAsync(CancellationToken cancellationToken = default);

    Task AddAsync(TEntity entity, CancellationToken cancellationToken = default);

    void Update(TEntity entity);

    void Remove(TEntity entity);
}
