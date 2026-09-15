using Manager.DataAccess.Entities;

namespace Manager.DataAccess.Interfaces;

// 
// Паттерн Unit of Work, предоставляющий доступ к репозиториям для различных сущностей и методы для управления транзакциями и сохранения изменений в базе данных.
// 
public interface IUnitOfWork: IDisposable
{
    IRepository<Employee> Employees { get; }
    IRepository<Project> Projects { get; }
    IRepository<ProjectEmployee> ProjectEmployees { get; }
    IRepository<ProjectDocument> ProjectDocuments { get; }
    IRepository<ProjectTask> ProjectTasks { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
    
}