using AlNeda.Core.Entities;

namespace AlNeda.Data.Repositories;

public interface IReadOnlyRepository<TEntity> where TEntity : class, IEntity
{
    Task<TEntity?> GetByIdAsync(int id);
    Task<IEnumerable<TEntity>> GetAllAsync();
    Task<IEnumerable<TEntity>> FindAsync(Func<TEntity, bool> predicate);
    Task<int> CountAsync();
}

public interface IWriteRepository<TEntity> where TEntity : class, IEntity
{
    Task AddAsync(TEntity entity);
    Task UpdateAsync(TEntity entity);
    Task DeleteAsync(int id);
}

public interface IRepository<TEntity> : IReadOnlyRepository<TEntity>, IWriteRepository<TEntity> 
    where TEntity : class, IEntity
{
    Task<int> CountAsync(Func<TEntity, bool> predicate);
}