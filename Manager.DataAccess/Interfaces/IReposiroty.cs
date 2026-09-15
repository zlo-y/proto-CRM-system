using System.Linq.Expressions;
using Manager.DataAccess.Entities;

namespace Manager.DataAccess.Interfaces;

// 
// Интерфейс для репозитория, предоставляющий методы для получения всех сущностей, получения сущности по идентификатору, добавления, обновления и удаления сущности, а также проверки существования сущности и получения пагинированного списка сущностей.
// 

public interface IRepository<T> where T : class
{
    IQueryable<T> GetAll();
    IQueryable<T> GetAllWithIncludes(params Expression<Func<T, object?>>[] includes);
    Task<T?> GetByIdAsync(int id , CancellationToken cancellationToken = default);
    Task AddAsync(T entity, CancellationToken cancellationToken = default);
    void Update(T entity);
    void Delete(T entity);
    Task<bool> AnyAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);
    Task<List<T>> ToListAsync(IQueryable<T> query, CancellationToken cancellationToken = default);
    // Добавляем перегрузку метода, которая принимает готовый IQueryable
    Task<(IEnumerable<T> Items, int TotalCount)> GetPaginatedAsync(IQueryable<T> query, int page, int pageSize, CancellationToken cancellationToken = default);
    
}