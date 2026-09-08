using Manager.BusinessLogic.DTOs;
using Manager.BusinessLogic.DTOs.Responses;
using Manager.BusinessLogic.Models;
using Manager.DataAccess.Entities;
using TaskStatus = Manager.DataAccess.Enums.TaskStatus;

namespace Manager.BusinessLogic.Interfaces;

public interface ITaskService
{
    Task<PagedResult<ProjectTaskDto>> GetProjectTasksAsync(int? projectId, TaskStatus? status, string sortBy, string order , int pageNumber, int pageSize,CancellationToken cancellationToken = default);
    Task CreateTaskAsync(TaskCreateDto task , int authorId,CancellationToken cancellationToken = default);
    Task AssignTaskExecutorAsync(int taskId, int? executorId, int currentEmployeeId, bool isAdmin,CancellationToken cancellationToken = default);
    Task UpdateTaskStatusAsync(int taskId, TaskStatus status, int currentEmployeeId, bool isAdmin,CancellationToken cancellationToken = default);

}