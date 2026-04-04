using BackendProjectTemplate.Domain.Common.Entities;
using BackendProjectTemplate.Domain.Common.Persistence;

namespace BackendProjectTemplate.UnitTests.Fakes;

public sealed class FakeRepository<TEntity>(IEnumerable<TEntity>? seed = null) : IRepository<TEntity>
    where TEntity : Entity
{
    private readonly List<TEntity> _items = seed?.ToList() ?? [];

    public IReadOnlyCollection<TEntity> Items => _items;

    public Task<TEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_items.FirstOrDefault(item => item.Id == id));

    public Task<TEntity?> FirstOrDefaultAsync(ISpecification<TEntity> specification, CancellationToken cancellationToken = default) =>
        Task.FromResult(Apply(specification).FirstOrDefault());

    public Task<IReadOnlyList<TEntity>> ListAsync(ISpecification<TEntity> specification, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<TEntity>>(Apply(specification).ToList());

    public Task<bool> AnyAsync(ISpecification<TEntity> specification, CancellationToken cancellationToken = default) =>
        Task.FromResult(Apply(specification).Any());

    public Task<int> CountAsync(ISpecification<TEntity> specification, CancellationToken cancellationToken = default) =>
        Task.FromResult(Apply(specification).Count());

    public Task AddAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        _items.Add(entity);
        return Task.CompletedTask;
    }

    public void Update(TEntity entity)
    {
    }

    public void Remove(TEntity entity) => _items.Remove(entity);

    private IEnumerable<TEntity> Apply(ISpecification<TEntity> specification)
    {
        IEnumerable<TEntity> query = _items;

        if (specification.Criteria is not null)
        {
            query = query.Where(specification.Criteria.Compile());
        }

        if (specification.OrderBy is not null)
        {
            query = query.OrderBy(specification.OrderBy.Compile());
        }
        else if (specification.OrderByDescending is not null)
        {
            query = query.OrderByDescending(specification.OrderByDescending.Compile());
        }

        if (specification.Skip.HasValue)
        {
            query = query.Skip(specification.Skip.Value);
        }

        if (specification.Take.HasValue)
        {
            query = query.Take(specification.Take.Value);
        }

        return query;
    }
}
