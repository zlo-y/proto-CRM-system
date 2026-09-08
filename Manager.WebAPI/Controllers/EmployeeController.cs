using Microsoft.AspNetCore.Mvc;
using Manager.BusinessLogic.Interfaces;
using Manager.DataAccess.Entities;
using Manager.BusinessLogic.DTOs;
using Microsoft.AspNetCore.Authorization;
using Manager.BusinessLogic.DTOs.Responses;
using Manager.BusinessLogic.Mappings;
using Manager.WebAPI.Extensions;

namespace Manager.WebAPI.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/[controller]")]
public class EmployeesController : ControllerBase
{
    private readonly IEmployeeService _employeeService;

    public EmployeesController(IEmployeeService employeeService)
    {
        _employeeService = employeeService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<EmployeeDto>>> GetEmployees([FromQuery] string? search, CancellationToken cancellationToken)
    {
        /// Get all employees from service, handle null search string
        var employees = await _employeeService.GetAllEmployeesAsync(search ?? "", cancellationToken);
        return Ok(ApiResponse<IEnumerable<EmployeeDto>>.Ok(employees, "Список сотрудников успешно получен."));
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] EmployeeUpdateDto dto, CancellationToken cancellationToken)
    {
        /// Check if route ID matches body ID to prevent data inconsistency
        if (id != dto.Id)
        {
            return BadRequest(ApiResponse.Fail("ID в маршруте не совпадает с ID в теле запроса."));
        }

        await _employeeService.UpdateEmployeeAsync(dto, cancellationToken);
        return NoContent();
    }
    
    [Authorize(Roles = "Admin")]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        /// Try to delete and check if it actually existed
        var isDeleted = await _employeeService.DeleteEmployeeAsync(id, cancellationToken);
        if (!isDeleted)
        {
            return NotFound(ApiResponse.Fail("Сотрудник не найден."));
        }
        return NoContent();
    }
}