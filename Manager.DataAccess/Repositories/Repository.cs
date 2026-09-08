using Manager.DataAccess.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Manager.DataAccess.Repositories;

public class Repository<T> : IRepository<T> where T : class
{
    protected readonly AppDbContext _context;
    protected readonly DbSet<T> _dbSet;

    public Repository(AppDbContext context)
    {
        _context = context;
        _dbSet = _context.Set<T>();
    }

    public IQueryable<T> GetAll() => _dbSet.AsQueryable();
    public IQueryable<T> GetAllWithIncludes(params Expression<Func<T, object?>>[] includes)
    {
        IQueryable<T> query = _dbSet.AsQueryable();
        foreach (var include in includes)
        {
            query = query.Include(include);
        }
        return query;
    }
    public async Task<T?> GetByIdAsync(int id, CancellationToken cancellationToken = default) => await _dbSet.FindAsync(id , cancellationToken);
    public async Task AddAsync(T entity, CancellationToken cancellationToken = default) => await _dbSet.AddAsync(entity, cancellationToken);
    public void Update(T entity) => _dbSet.Update(entity);
    public void Delete(T entity) => _dbSet.Remove(entity);
    public async Task<bool> AnyAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default) => await _dbSet.AnyAsync(predicate, cancellationToken);
    public async Task<List<T>> ToListAsync(IQueryable<T> query, CancellationToken cancellationToken = default) => await query.ToListAsync(cancellationToken);

    public async Task<(IEnumerable<T> Items, int TotalCount)> GetPaginatedAsync(IQueryable<T> query, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var totalCount = await query.CountAsync(cancellationToken);
        var orderedQuery = query.OrderBy(e => EF.Property<int>(e, "Id")); // Assuming "Id" is the primary key
        var items = await orderedQuery
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }


}