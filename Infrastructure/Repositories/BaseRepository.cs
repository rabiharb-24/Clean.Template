using System.Linq.Expressions;
using Domain.Entities.Abstraction;
using Infrastructure.Persistence.Contexts;

namespace Infrastructure.Repositories;

public class BaseRepository<TEntity> : IRepository<TEntity>
    where TEntity : EntityBase, new()
{
    protected ApplicationDbContext Context { get; }

    public BaseRepository(ApplicationDbContext context)
    {
        Context = context;
        _dbSet = Context.Set<TEntity>();
    }

    private readonly DbSet<TEntity> _dbSet;

    public async Task<TEntity?> GetAsync(int id, bool tracking = false, CancellationToken cancellationToken = default)
    {
        return tracking ? await _dbSet.FindAsync([id], cancellationToken: cancellationToken)
                        : await _dbSet.AsNoTracking().FirstOrDefaultAsync(e => e.Id.Equals(id), cancellationToken);
    }

    public async Task<IEnumerable<TEntity>> GetAllAsync(bool tracking = false, CancellationToken cancellationToken = default)
    {
        return tracking ? await _dbSet.ToListAsync(cancellationToken)
                        : await _dbSet.AsNoTracking().ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<TEntity>> WhereAsync(Expression<Func<TEntity, bool>> filter, bool tracking = false, CancellationToken cancellationToken = default)
    {
        IQueryable<TEntity> values = _dbSet.Where(filter);
        return tracking ? await values.ToListAsync(cancellationToken)
                        : await values.AsNoTracking().ToListAsync(cancellationToken);
    }

    public async Task<TEntity?> FirstOrDefaultAsync(Expression<Func<TEntity, bool>> filter, bool tracking = false, CancellationToken cancellationToken = default)
    {
        return tracking ? await _dbSet.FirstOrDefaultAsync(filter, cancellationToken)
                        : await _dbSet.AsNoTracking().FirstOrDefaultAsync(filter, cancellationToken);
    }

    public async Task<TEntity> CreateAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        await _dbSet.AddAsync(entity, cancellationToken);
        return entity;
    }

    public async Task CreateAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
    {
        await _dbSet.AddRangeAsync(entities, cancellationToken);
    }

    public void Update(TEntity entity)
    {
       _dbSet.Update(entity);
    }

    public void Update(IEnumerable<TEntity> entities)
    {
        _dbSet.UpdateRange(entities);
    }

    public void Delete(int entityId)
    {
        TEntity? entity = _dbSet.Find(entityId);
        if (entity != default)
        {
            Delete(new TEntity { Id = entityId });
        }
    }

    public void Delete(IEnumerable<int> keys)
    {
        IQueryable<TEntity> entities = _dbSet.Where(x => keys.Contains(x.Id));

        Delete(entities);
    }

    public void Delete(TEntity entity)
    {
        TEntity? existingEntity = _dbSet.Find(entity.Id);
        if (existingEntity != default)
        {
            _dbSet.Remove(existingEntity);
        }
    }

    public void Delete(IEnumerable<TEntity> entities)
    {
        IQueryable<TEntity> existingEntities = _dbSet.Where(x => entities.Select(x => x.Id).Contains(x.Id));
        _dbSet.RemoveRange(existingEntities);
    }
}
