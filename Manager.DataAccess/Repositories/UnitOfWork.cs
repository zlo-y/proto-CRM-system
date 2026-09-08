using Manager.DataAccess.Interfaces;
using Manager.DataAccess.Entities;
using System.Runtime.InteropServices;
using Microsoft.EntityFrameworkCore.Storage;

namespace Manager.DataAccess.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;
    private IDbContextTransaction? _transaction;

    public UnitOfWork(AppDbContext context)
    {
        _context = context;
        Employees = new Repository<Employee>(_context);
        Projects = new Repository<Project>(_context);
        ProjectEmployees = new Repository<ProjectEmployee>(_context);
        ProjectDocuments = new Repository<ProjectDocument>(_context);
        ProjectTasks = new Repository<ProjectTask>(_context);
    }

    public IRepository<Employee> Employees { get; }
    public IRepository<Project> Projects { get; }
    public IRepository<ProjectEmployee> ProjectEmployees { get; }
    public IRepository<ProjectDocument> ProjectDocuments { get; }
    public IRepository<ProjectTask> ProjectTasks { get; }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        _transaction =await _context.Database.BeginTransactionAsync(cancellationToken);
    }

    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        if(_transaction != null)
        {
            await _transaction.CommitAsync(cancellationToken);
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        if(_transaction != null)
        {
            await _transaction.RollbackAsync(cancellationToken);
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public void Dispose()
    {
        _transaction?.Dispose();
        _context.Dispose();
    }
}