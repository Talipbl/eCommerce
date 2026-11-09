using ECommerce.Core.Domain;
using ECommerce.Core.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace ECommerce.Persistence;

public class EfRepository<TEntity>(ECommerceContext context) : IRepository<TEntity> where TEntity : class, IEntity
{
    private readonly DbSet<TEntity> _set = context.Set<TEntity>();

    public IQueryable<TEntity> Query() => _set;

    public async Task<TEntity?> GetByIdAsync(object id, CancellationToken cancellationToken = default) =>
        await _set.FindAsync([id], cancellationToken);

    public Task<TEntity?> FirstOrDefaultAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default) =>
        _set.FirstOrDefaultAsync(predicate, cancellationToken);

    public async Task<IReadOnlyList<TEntity>> ListAsync(Expression<Func<TEntity, bool>>? predicate = null, CancellationToken cancellationToken = default) =>
        await (predicate is null ? _set : _set.Where(predicate)).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<TEntity>> ListAsync(Func<IQueryable<TEntity>, IQueryable<TEntity>> query, CancellationToken cancellationToken = default) =>
        await query(_set).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<TResult>> ListAsync<TResult>(Func<IQueryable<TEntity>, IQueryable<TResult>> query, CancellationToken cancellationToken = default) =>
        await query(_set).ToListAsync(cancellationToken);

    public Task<TResult?> FirstOrDefaultAsync<TResult>(Func<IQueryable<TEntity>, IQueryable<TResult>> query, CancellationToken cancellationToken = default) =>
        query(_set).FirstOrDefaultAsync(cancellationToken);

    public Task<int> CountAsync<TResult>(Func<IQueryable<TEntity>, IQueryable<TResult>> query, CancellationToken cancellationToken = default) =>
        query(_set).CountAsync(cancellationToken);

    public Task<bool> AnyAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default) =>
        _set.AnyAsync(predicate, cancellationToken);

    public Task<int> CountAsync(Expression<Func<TEntity, bool>>? predicate = null, CancellationToken cancellationToken = default) =>
        predicate is null ? _set.CountAsync(cancellationToken) : _set.CountAsync(predicate, cancellationToken);

    public void Add(TEntity entity) => _set.Add(entity);

    public void Update(TEntity entity) => _set.Update(entity);

    public void Remove(TEntity entity) => _set.Remove(entity);
}
