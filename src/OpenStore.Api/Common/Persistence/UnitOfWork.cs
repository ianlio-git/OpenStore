using OpenStore.Api.Common.Contracts;

namespace OpenStore.Api.Common.Persistence;

public sealed class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;

    public UnitOfWork(AppDbContext context)
    {
        _context = context;
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        int result = await _context.SaveChangesAsync(cancellationToken);

        return result;
    }
}
