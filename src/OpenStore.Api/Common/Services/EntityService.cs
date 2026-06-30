using OpenStore.Api.Common.Contracts;

namespace OpenStore.Api.Common.Services;

public sealed class EntityService<TEntity> : IEntityService<TEntity> where TEntity : class
{
    private readonly IRepository<TEntity> _repository;
    private readonly IUnitOfWork _unitOfWork;

    public EntityService(IRepository<TEntity> repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<TEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        TEntity? result = await _repository.GetByIdAsync(id, cancellationToken);

        return result;
    }

    public async Task<IReadOnlyCollection<TEntity>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyCollection<TEntity> result = await _repository.GetAllAsync(cancellationToken);

        return result;
    }

    public async Task AddAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        _repository.Add(entity);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public void Update(TEntity entity) => _repository.Update(entity);

    public void Remove(TEntity entity) => _repository.Remove(entity);
}
