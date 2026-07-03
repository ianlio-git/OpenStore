using System.Linq.Expressions;
using OpenStore.Api.Common.Contracts;
using OpenStore.Api.Common.Entities;
using OpenStore.Api.Common.Services;

namespace OpenStore.Api.Tests.Common.Services;

public sealed class EntityServiceBaseTests
{
    private sealed class FakeRepository<TEntity> : IRepository<TEntity> where TEntity : class
    {
        private readonly List<TEntity> _entities;

        public FakeRepository(List<TEntity> entities)
        {
            _entities = entities;
        }

        public Task<TEntity?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<IReadOnlyCollection<TEntity>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            IReadOnlyCollection<TEntity> result = _entities.ToArray();

            return Task.FromResult(result);
        }

        public Task<IReadOnlyCollection<TEntity>> FindAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default)
        {
            IReadOnlyCollection<TEntity> result = _entities.Where(predicate.Compile()).ToArray();

            return Task.FromResult(result);
        }

        public Task<bool> AnyAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default)
        {
            bool result = _entities.Any(predicate.Compile());

            return Task.FromResult(result);
        }

        public void Add(TEntity entity)
        {
            _entities.Add(entity);
        }

        public void Update(TEntity entity)
        {
        }

        public void Remove(TEntity entity)
        {
            _entities.Remove(entity);
        }
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public bool SaveChangesCalled { get; private set; }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveChangesCalled = true;

            return Task.FromResult(1);
        }
    }

    private sealed class FakeDateTimeProvider : IDateTimeProvider
    {
        public DateTimeOffset UtcNow { get; } = new(2026, 6, 30, 12, 0, 0, TimeSpan.Zero);
    }

    private sealed class TestEntity : BaseEntity
    {
    }

    private sealed class TestService : EntityServiceBase<TestEntity>
    {
        public TestService(
            IRepository<TestEntity> repository,
            IUnitOfWork unitOfWork,
            IDateTimeProvider dateTimeProvider)
            : base(repository, unitOfWork, dateTimeProvider)
        {
        }

        public new Task<TestEntity> GetByPublicIdOrThrowAsync(Guid publicId, Func<Exception> notFoundFactory, CancellationToken cancellationToken)
        {
            return base.GetByPublicIdOrThrowAsync(publicId, notFoundFactory, cancellationToken);
        }

        public new Task<bool> ExistsAsync(Expression<Func<TestEntity, bool>> predicate, CancellationToken cancellationToken)
        {
            return base.ExistsAsync(predicate, cancellationToken);
        }

        public new Task<IReadOnlyCollection<TestEntity>> FindAsync(Expression<Func<TestEntity, bool>> predicate, CancellationToken cancellationToken)
        {
            return base.FindAsync(predicate, cancellationToken);
        }

        public new void Add(TestEntity entity)
        {
            base.Add(entity);
        }

        public new void Update(TestEntity entity)
        {
            base.Update(entity);
        }

        public new void Remove(TestEntity entity)
        {
            base.Remove(entity);
        }

        public new Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            return base.SaveChangesAsync(cancellationToken);
        }
    }

    [Fact]
    public async Task GetByPublicIdOrThrowAsync_EntityFound_ReturnsEntity()
    {
        Guid publicId = Guid.NewGuid();
        List<TestEntity> entities =
        [
            new() { Id = 1, PublicId = publicId }
        ];

        FakeRepository<TestEntity> repository = new(entities);
        FakeUnitOfWork unitOfWork = new();
        TestService service = new(repository, unitOfWork, new FakeDateTimeProvider());

        TestEntity result = await service.GetByPublicIdOrThrowAsync(publicId, () => new InvalidOperationException(), CancellationToken.None);

        Assert.Equal(1, result.Id);
    }

    [Fact]
    public async Task GetByPublicIdOrThrowAsync_EntityNotFound_Throws()
    {
        List<TestEntity> entities = [];
        FakeRepository<TestEntity> repository = new(entities);
        FakeUnitOfWork unitOfWork = new();
        TestService service = new(repository, unitOfWork, new FakeDateTimeProvider());

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.GetByPublicIdOrThrowAsync(Guid.NewGuid(), () => new InvalidOperationException(), CancellationToken.None));
    }

    [Fact]
    public async Task ExistsAsync_ReturnsTrue()
    {
        List<TestEntity> entities =
        [
            new() { Id = 1 }
        ];

        FakeRepository<TestEntity> repository = new(entities);
        FakeUnitOfWork unitOfWork = new();
        TestService service = new(repository, unitOfWork, new FakeDateTimeProvider());

        bool result = await service.ExistsAsync(e => e.Id == 1, CancellationToken.None);

        Assert.True(result);
    }

    [Fact]
    public async Task ExistsAsync_ReturnsFalse()
    {
        List<TestEntity> entities = [];
        FakeRepository<TestEntity> repository = new(entities);
        FakeUnitOfWork unitOfWork = new();
        TestService service = new(repository, unitOfWork, new FakeDateTimeProvider());

        bool result = await service.ExistsAsync(e => e.Id == 1, CancellationToken.None);

        Assert.False(result);
    }

    [Fact]
    public async Task FindAsync_ReturnsMatchingEntities()
    {
        List<TestEntity> entities =
        [
            new() { Id = 1 },
            new() { Id = 2 }
        ];

        FakeRepository<TestEntity> repository = new(entities);
        FakeUnitOfWork unitOfWork = new();
        TestService service = new(repository, unitOfWork, new FakeDateTimeProvider());

        IReadOnlyCollection<TestEntity> result = await service.FindAsync(e => e.Id > 0, CancellationToken.None);

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task Add_DelegatesToRepository()
    {
        List<TestEntity> entities = [];
        FakeRepository<TestEntity> repository = new(entities);
        FakeUnitOfWork unitOfWork = new();
        TestService service = new(repository, unitOfWork, new FakeDateTimeProvider());

        TestEntity entity = new() { Id = 1 };

        service.Add(entity);

        Assert.Contains(entity, entities);
    }

    [Fact]
    public async Task Remove_DelegatesToRepository()
    {
        TestEntity entity = new() { Id = 1 };
        List<TestEntity> entities = [entity];

        FakeRepository<TestEntity> repository = new(entities);
        FakeUnitOfWork unitOfWork = new();
        TestService service = new(repository, unitOfWork, new FakeDateTimeProvider());

        service.Remove(entity);

        Assert.DoesNotContain(entity, entities);
    }

    [Fact]
    public async Task SaveChangesAsync_DelegatesToUnitOfWork()
    {
        List<TestEntity> entities = [];
        FakeRepository<TestEntity> repository = new(entities);
        FakeUnitOfWork unitOfWork = new();
        TestService service = new(repository, unitOfWork, new FakeDateTimeProvider());

        await service.SaveChangesAsync(CancellationToken.None);

        Assert.True(unitOfWork.SaveChangesCalled);
    }
}
