using Manager.DataAccess.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;


namespace Manager.DataAccess;

public class AppDbContext: IdentityDbContext<User, IdentityRole<int>, int>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options){}

    public DbSet<Employee> Employees { get; set; }
    public DbSet<Project> Projects { get; set; }
    public DbSet<ProjectEmployee> ProjectEmployees { get; set; }
    public DbSet<ProjectDocument> ProjectDocuments { get; set; }
    public DbSet<ProjectTask> ProjectTasks { get; set; }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ProjectEmployee>()
            .HasKey(pe => new { pe.ProjectId, pe.EmployeeId });

        modelBuilder.Entity<Employee>()
            .HasOne(e => e.User)
            .WithOne()
            .HasForeignKey<Employee>(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);

// 
// Модель для связи многие-ко-многим между проектами и сотрудниками.
// 
        modelBuilder.Entity<ProjectEmployee>()
            .HasOne(pe => pe.Project)
            .WithMany(p => p.ProjectEmployees)
            .HasForeignKey(pe => pe.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ProjectEmployee>()
            .HasOne(pe => pe.Employee)
            .WithMany(e => e.ProjectEmployees)
            .HasForeignKey(pe => pe.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);

// 
// Рук.проекта 
// 
        modelBuilder.Entity<Project>()
            .HasOne(p => p.Manager)
            .WithMany(m => m.ManagedProjects)
            .HasForeignKey(p => p.ManagerId)
            .OnDelete(DeleteBehavior.Restrict);

// 
// Доки проекта
// 
        modelBuilder.Entity<ProjectDocument>()
            .HasOne(pd => pd.Project)
            .WithMany(p => p.ProjectDocuments)
            .HasForeignKey(pd => pd.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

// 
// Задача => Автор, Исполнитель, Проект
// 
        modelBuilder.Entity<ProjectTask>()
            .HasOne(pt => pt.Author)
            .WithMany()
            .HasForeignKey(pt => pt.AuthorId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ProjectTask>()
            .HasOne(pt => pt.Executor)
            .WithMany()
            .HasForeignKey(pt => pt.ExecutorId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<ProjectTask>()
            .HasOne(pt => pt.Project)
            .WithMany(p => p.ProjectTasks)
            .HasForeignKey(pt => pt.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ProjectTask>()
            .Property(pt => pt.Name)
            .IsRequired();
            

        
    }
}