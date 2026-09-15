using Microsoft.AspNetCore.Mvc;
using Manager.BusinessLogic.Interfaces;
using Manager.DataAccess.Entities;
using TaskStatus = Manager.DataAccess.Enums.TaskStatus;
using Manager.BusinessLogic.DTOs;
using Microsoft.AspNetCore.Authorization;
using Manager.BusinessLogic.DTOs.Responses;
using Manager.WebAPI.Extensions;
using Manager.BusinessLogic.Models;


namespace Manager.WebAPI.Controllers;

// 
// Контроллер для управления задачами, предоставляющий методы для получения списка задач проекта, создания задачи, назначения исполнителя и обновления статуса задачи. 
// 
[Authorize]
[ApiController]
[Route("api/v1/[controller]")]
public class TasksController : ControllerBase
{
    private readonly ITaskService _taskService;
    private readonly ICurrentUserProvider _currentUserProvider;
    public TasksController(ITaskService taskService, ICurrentUserProvider currentUserProvider)
    {
        _taskService = taskService;
        _currentUserProvider = currentUserProvider;
    }

    [HttpGet]
    public async Task<ActionResult<List<ProjectTaskDto>>> GetProjectTasks([FromQuery] int? projectId, [FromQuery] TaskStatus? status,
        [FromQuery] string? sortBy, [FromQuery] string? order , [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10, CancellationToken cancellationToken = default)
    {
        var tasks = await _taskService.GetProjectTasksAsync(projectId, status, sortBy ?? "", order ?? "", pageNumber, pageSize, cancellationToken);
        return Ok(ApiResponse<PagedResult<ProjectTaskDto>>.Ok(tasks, "Список задач успешно получен."));
    }

    [HttpPost]
    public async Task<IActionResult> CreateTask([FromBody] TaskCreateDto dto, CancellationToken cancellationToken)
    {
            int currentEmployeeId = _currentUserProvider.GetEmployeeId();
            await _taskService.CreateTaskAsync(dto , currentEmployeeId, cancellationToken);
            return StatusCode(201, ApiResponse.Ok("Задача успешно создана."));
    }

    [HttpPatch("{id:int}/assign")]
    public async Task<IActionResult> AssignTask(int id, [FromBody] int? executorId, CancellationToken cancellationToken)
    {
            await _taskService.AssignTaskExecutorAsync(id, executorId, _currentUserProvider.GetEmployeeId(), _currentUserProvider.IsAdmin(), cancellationToken);
            return NoContent();
    }

    [HttpPatch("{id:int}/status")]
    public async Task<IActionResult> UpdateStatus(int id, [FromQuery] TaskStatus status, CancellationToken cancellationToken)
    {
            await _taskService.UpdateTaskStatusAsync(id, status, _currentUserProvider.GetEmployeeId(), _currentUserProvider.IsAdmin(), cancellationToken);
            return NoContent();
    }
}