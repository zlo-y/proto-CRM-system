using Manager.BusinessLogic.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Manager.DataAccess.Entities;
using Manager.BusinessLogic.DTOs;
using Microsoft.AspNetCore.Authorization;
using Manager.BusinessLogic.Models;
using Manager.BusinessLogic.DTOs.Responses;
using Manager.WebAPI.Extensions;
using Manager.BusinessLogic.Exceptions;

namespace Manager.WebAPI.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/[controller]")]
public class ProjectsController : ControllerBase
{
    private readonly IProjectService _projectService;
    private readonly IWebHostEnvironment _webHostEnvironment;
    private readonly ICurrentUserProvider _currentUserProvider;

    public ProjectsController(IProjectService projectService, IWebHostEnvironment webHostEnvironment, ICurrentUserProvider currentUserProvider)
    {
        _projectService = projectService;
        _webHostEnvironment = webHostEnvironment;
        _currentUserProvider = currentUserProvider;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<ProjectListDto>>> GetAllProjects([FromQuery] DateTime? startFrom,
        [FromQuery] DateTime? startTo,
        [FromQuery] int? priority,
        [FromQuery] string? sortBy,
        [FromQuery] string? order,
        [FromQuery] int pageNumber = 1 ,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        /// Fetch filtered and sorted projects from service
        var projects = await _projectService.GetAllProjectsAsync(startFrom, startTo, priority, sortBy ?? "", order ?? "" , pageNumber, pageSize, cancellationToken);
        return Ok(ApiResponse<PagedResult<ProjectListDto>>.Ok(projects, "Список проектов успешно получен."));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ProjectDetailDto>> GetById(int id, CancellationToken cancellationToken)
    {
        /// Return 404 if project doesn't exist
        var project = await _projectService.GetProjectByIdAsync(id, cancellationToken);
        if (project == null)
        {
            return NotFound(ApiResponse.Fail("Проект не найден."));
        }
        return Ok(ApiResponse<ProjectDetailDto>.Ok(project, "Детали проекта успешно получены."));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromForm] ProjectServiceDto dto, CancellationToken cancellationToken)
    {
            /// Pass web root path for potential file saving logic
            await _projectService.CreateProjectAsync(dto, cancellationToken);
            return StatusCode(201, ApiResponse.Ok("Проект успешно создан."));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] ProjectUpdateDto project, CancellationToken cancellationToken)
    {
        /// Ensure ID consistency between URL and request body
        if (id != project.Id)
        {
            return BadRequest(ApiResponse.Fail("ID в маршруте не совпадает с ID в теле запроса."));
        }

        await _projectService.UpdateProjectAsync(project, _currentUserProvider.GetEmployeeId(), _currentUserProvider.IsAdmin(), cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        /// Verify existence before returning success status
        var isDeleted = await _projectService.DeleteProjectAsync(id, _currentUserProvider.GetEmployeeId(), _currentUserProvider.IsAdmin(), cancellationToken);
        if (!isDeleted)
        {
            return NotFound(ApiResponse.Fail("Проект не найден."));
        }
        return NoContent();
    }

    [HttpPost("{projectId}/employees/{employeeId}")]
    public async Task<IActionResult> AddEmployee(int projectId, int employeeId, CancellationToken cancellationToken)
    {
        /// Link employee to project
        await _projectService.AddEmployeeProject(projectId, employeeId, _currentUserProvider.GetEmployeeId(), _currentUserProvider.IsAdmin(), cancellationToken);
        return Ok();
    }

    [HttpDelete("{projectId}/employees/{employeeId}")]
    public async Task<IActionResult> RemoveEmployee(int projectId, int employeeId, CancellationToken cancellationToken)
    {
        /// Unlink employee from project
        await _projectService.RemoveEmployeeFromProjectAsync(projectId, employeeId, _currentUserProvider.GetEmployeeId(), _currentUserProvider.IsAdmin(), cancellationToken);
        return NoContent();
    }
}