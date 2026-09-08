// Manager.BusinessLogic/Mappings/DtoMappings.cs
using Manager.BusinessLogic.DTOs.Responses;
using Manager.BusinessLogic.Models;
using Manager.DataAccess.Entities;

namespace Manager.BusinessLogic.Mappings;

public static class DtoMappings
{
    public static EmployeeDto ToDto(this Employee employee) => new()
    {
        Id = employee.Id,
        Name = employee.Name,
        LastName = employee.LastName,
        MiddleName = employee.MiddleName,
        Email = employee.User?.Email ?? string.Empty,
    };

    public static ProjectDocumentDto ToDto(this ProjectDocument document) => new()
    {
        Id = document.Id,
        FileName = document.FileName,
        FilePath = document.FilePath
    };

    public static ProjectTaskDto ToDto(this ProjectTask task) => new()
    {
        Id = task.Id,
        Name = task.Name,
        Comments = task.Comments,
        Priority = task.Priority,
        Status = task.Status,
        ProjectId = task.ProjectId,
        AuthorId = task.AuthorId,
        AuthorFullName = task.Author?.ToDto().FullName,
        ExecutorId = task.ExecutorId,
        ExecutorFullName = task.Executor?.ToDto().FullName
    };

    public static ProjectListDto ToListDto(this Project project) => new()
    {
        Id = project.Id,
        Name = project.Name,
        Customer = project.Customer,
        Executor = project.Executor,
        StartDate = project.StartDate,
        EndDate = project.EndDate,
        Priority = project.Priority,
        ManagerId = project.ManagerId,
        ManagerFullName = project.Manager?.ToDto().FullName
    };

    public static ProjectDetailDto ToDetailDto(this Project project) => new()
    {
        Id = project.Id,
        Name = project.Name,
        Customer = project.Customer,
        Executor = project.Executor,
        StartDate = project.StartDate,
        EndDate = project.EndDate,
        Priority = project.Priority,
        ManagerId = project.ManagerId,
        ManagerFullName = project.Manager?.ToDto().FullName,
        Employees = project.ProjectEmployees
            .Where(pe => pe.Employee != null)
            .Select(pe => pe.Employee!.ToDto())
            .ToList(),
        Tasks = project.ProjectTasks.Select(t => t.ToDto()).ToList(),
        Documents = project.ProjectDocuments.Select(d => d.ToDto()).ToList()
    };

    public static PagedResult<TDto> ToPagedDto<TEntity, TDto>(
        this PagedResult<TEntity> source, Func<TEntity, TDto> map) => new()
    {
        Items = source.Items.Select(map).ToList(),
        TotalCount = source.TotalCount,
        PageNumber = source.PageNumber,
        PageSize = source.PageSize
    };
}