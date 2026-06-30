using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using OpenStore.Api.Common.Contracts;

namespace OpenStore.Api.Common.Persistence;

public sealed class Repository<TEntity> : IRepository<TEntity>
    where TEntity : class
{
    private readonly DbSet<TEntity> _set;

    public Repository(AppDbContext context)
    {
        _set = context.Set<TEntity>();
    }

    public async Task<TEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        TEntity? result = await _set.FindAsync([id], cancellationToken);

        return result;
    }

    public async Task<IReadOnlyCollection<TEntity>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        List<TEntity> result = await _set.ToListAsync(cancellationToken);

        return result;
    }

    public async Task<IReadOnlyCollection<TEntity>> FindAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default)
    {
        List<TEntity> result = await _set.Where(predicate).ToListAsync(cancellationToken);

        return result;
    }

    public async Task<bool> AnyAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default)
    {
        bool result = await _set.AnyAsync(predicate, cancellationToken);

        return result;
    }

    public void Add(TEntity entity)
    {
        _set.Add(entity);
    }

    public void Update(TEntity entity)
    {
        _set.Update(entity);
    }

    public void Remove(TEntity entity)
    {
        _set.Remove(entity);
    }
}
