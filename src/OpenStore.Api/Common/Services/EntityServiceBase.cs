using System.Linq.Expressions;
using OpenStore.Api.Common.Contracts;
using OpenStore.Api.Common.Entities;

namespace OpenStore.Api.Common.Services;

public abstract class EntityServiceBase<TEntity> where TEntity : BaseEntity
{
    protected IRepository<TEntity> Repository { get; }

    protected IUnitOfWork UnitOfWork { get; }

    protected IDateTimeProvider DateTimeProvider { get; }

    protected EntityServiceBase(IRepository<TEntity> repository, IUnitOfWork unitOfWork, IDateTimeProvider dateTimeProvider)
    {
        Repository = repository;
        UnitOfWork = unitOfWork;
        DateTimeProvider = dateTimeProvider;
    }

    protected async Task<TEntity> GetByPublicIdOrThrowAsync(Guid publicId, Func<Exception> notFoundFactory, CancellationToken cancellationToken)
    {
        IReadOnlyCollection<TEntity> matches = await Repository.FindAsync(e => e.PublicId == publicId, cancellationToken);

        TEntity result = matches.FirstOrDefault() ?? throw notFoundFactory();

        return result;
    }

    protected async Task<bool> ExistsAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken)
    {
        bool result = await Repository.AnyAsync(predicate, cancellationToken);

        return result;
    }

    protected async Task<IReadOnlyCollection<TEntity>> FindAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken)
    {
        IReadOnlyCollection<TEntity> result = await Repository.FindAsync(predicate, cancellationToken);

        return result;
    }

    protected void Add(TEntity entity) => Repository.Add(entity);

    protected void Update(TEntity entity) => Repository.Update(entity);

    protected void Remove(TEntity entity) => Repository.Remove(entity);

    protected async Task SaveChangesAsync(CancellationToken cancellationToken) => await UnitOfWork.SaveChangesAsync(cancellationToken);
}
