using Manager.BusinessLogic.Interfaces;
using Manager.DataAccess.Entities;
using TaskStatus = Manager.DataAccess.Enums.TaskStatus;
using Manager.BusinessLogic.DTOs;
using Manager.DataAccess.Interfaces;
using Manager.BusinessLogic.Models;
using Manager.BusinessLogic.Mappings;
using Manager.BusinessLogic.DTOs.Responses;
using Manager.BusinessLogic.Exceptions;
using Microsoft.EntityFrameworkCore;

// 
// Сервис для управления задачами, реализующий интерфейс ITaskService.
// 

namespace Manager.BusinessLogic.Services;

public class TaskService : ITaskService{
    private readonly IUnitOfWork _unitOfWork;

    public TaskService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }
    
    public async Task<PagedResult<ProjectTaskDto>> GetProjectTasksAsync(int? projectId, TaskStatus? status, string sortBy, string order , int pageNumber = 1, int pageSize = 10, CancellationToken cancellationToken = default)
    {
        /// Load tasks with navigation properties for UI display
        var tasks = _unitOfWork.ProjectTasks
            .GetAllWithIncludes(
            p => p.Author,
            p => p.Executor)
            .AsNoTracking();



        if(projectId.HasValue) tasks = tasks.Where(t => t.ProjectId == projectId.Value);
        if(status.HasValue) tasks = tasks.Where(t => t.Status == status.Value);

        /// Apply dynamic sorting
        bool isDescending = string.Equals(order, "desc", StringComparison.OrdinalIgnoreCase);

        tasks = sortBy?.ToLowerInvariant() switch
        {
            "name" => isDescending ? tasks.OrderByDescending(t => t.Name) : tasks.OrderBy(t => t.Name),
            "status" => isDescending ? tasks.OrderByDescending(t => t.Status) : tasks.OrderBy(t => t.Status),
            _ => isDescending ? tasks.OrderByDescending(t => t.Id) : tasks.OrderBy(t => t.Id)
        };
        var (items, totalCount) = await _unitOfWork.ProjectTasks.GetPaginatedAsync(tasks , pageNumber, pageSize, cancellationToken);
        
        return new PagedResult<ProjectTaskDto>
        {
            Items = items.Select(t => t.ToDto()).ToList(),
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };


    }

    public async Task CreateTaskAsync(TaskCreateDto dto , int authorId, CancellationToken cancellationToken = default)
    {

         var projectExists = await _unitOfWork.Projects.AnyAsync(p => p.Id == dto.ProjectId, cancellationToken);
        if (!projectExists) throw new NotFoundException("Указанный проект не существует!");
         if (dto.ExecutorId.HasValue)
        {
            var executorExists = await _unitOfWork.Employees.AnyAsync(e => e.Id == dto.ExecutorId.Value, cancellationToken);
            if (!executorExists)
                throw new NotFoundException($"Исполнитель с ID {dto.ExecutorId} не найден.");
        }

        var task = new ProjectTask
        {
            Name = dto.Name,
            Comments = dto.Comments,
            Priority = dto.Priority,
            ProjectId = dto.ProjectId,
            ExecutorId = dto.ExecutorId,
            Status = TaskStatus.ToDo, 
            AuthorId = authorId
        };

        await _unitOfWork.ProjectTasks.AddAsync(task, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task AssignTaskExecutorAsync(int taskId, int? executorId,int currentEmployeeId, bool isAdmin, CancellationToken cancellationToken = default)
    {
        /// Find task and update assigned executor
        var task = await _unitOfWork.ProjectTasks.GetByIdAsync(taskId, cancellationToken);
        if (task == null){
            throw new ArgumentException($"Task with ID {taskId} not found.");}
        var project = await _unitOfWork.Projects.GetByIdAsync(task.ProjectId, cancellationToken);
        if (project == null)
        {
            throw new NotFoundException($"Project with ID {task.ProjectId} not found.");
        }
        if(!isAdmin && project.ManagerId != currentEmployeeId)
        {
            throw new ForbiddenException("You do not have permission to assign executors to this task.");
        }
        if (executorId.HasValue)
        {
            var executorExists = await _unitOfWork.Employees.AnyAsync(e => e.Id == executorId.Value, cancellationToken);
            if (!executorExists)
                throw new NotFoundException($"Исполнитель с ID {executorId} не найден.");
        }

        task.ExecutorId = executorId;
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateTaskStatusAsync(int taskId, TaskStatus status, int currentEmployeeId, bool isAdmin,CancellationToken cancellationToken = default)
    {
        var task = await _unitOfWork.ProjectTasks.GetByIdAsync(taskId, cancellationToken);
        if (task == null)
        {
            throw new ArgumentException($"Task with ID {taskId} not found.");
        }
        var isAuthor = task.AuthorId == currentEmployeeId;
        var isExecutor = task.ExecutorId == currentEmployeeId;
        if (!isAdmin && !isAuthor && !isExecutor)
        {
            var project = await _unitOfWork.Projects.GetByIdAsync(task.ProjectId, cancellationToken);
            var isManager = project?.ManagerId == currentEmployeeId;
            if (!isManager)
            {
                throw new ForbiddenException("You do not have permission to update the status of this task.");
            }
        }
        task.Status = status;
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}