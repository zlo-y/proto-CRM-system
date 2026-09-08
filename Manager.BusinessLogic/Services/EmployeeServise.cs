using Manager.BusinessLogic.DTOs;
using Manager.BusinessLogic.DTOs.Responses;
using Manager.BusinessLogic.Exceptions;
using Manager.BusinessLogic.Interfaces;
using Manager.BusinessLogic.Mappings;
using Microsoft.EntityFrameworkCore;
using Manager.DataAccess.Interfaces;
using Manager.DataAccess.Entities;
using Microsoft.AspNetCore.Identity;


namespace Manager.BusinessLogic.Services;

public class EmployeeService : IEmployeeService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly UserManager<User> _userManager;

    public EmployeeService(IUnitOfWork unitOfWork, UserManager<User> userManager)
    {
        _unitOfWork = unitOfWork;
        _userManager = userManager;
    }

    public async Task<IEnumerable<EmployeeDto>> GetAllEmployeesAsync(string search, CancellationToken cancellationToken = default)
    {
        var query = _unitOfWork.Employees
            .GetAllWithIncludes(e => e.User)
            .AsNoTracking();

        /// Case-insensitive search across name fields
        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.ToLower();
            query = query.Where(e => 
                e.Name.Contains(search) || 
                e.LastName.Contains(search) ||
                e.MiddleName.Contains(search));
        }

        var result = await _unitOfWork.Employees.ToListAsync(query, cancellationToken);
        return result.Select(p => p.ToDto());
    }



    public async Task UpdateEmployeeAsync(EmployeeUpdateDto dto, CancellationToken cancellationToken = default)
    {
        var employee = await _unitOfWork.Employees.GetAllWithIncludes(e => e.User)
            .FirstOrDefaultAsync(e => e.Id == dto.Id, cancellationToken);
        if (employee == null)
        {
            throw new NotFoundException($"Employee with ID {dto.Id} not found.");
        }

        if(!string.Equals(employee.User.Email, dto.Email, StringComparison.OrdinalIgnoreCase))
        {
            var setEmailResult = await _userManager.SetEmailAsync(employee.User, dto.Email);
            if(!setEmailResult.Succeeded)
            {
                var errors = string.Join(", ", setEmailResult.Errors.Select(e => e.Description));
                throw new ValidationAppException($"Ошибка при обновлении email пользователя: {errors}");
            }
            var setUserNameResult = await _userManager.SetUserNameAsync(employee.User, dto.Email);
            if (!setUserNameResult.Succeeded)
            {
                var errors = string.Join(", ", setUserNameResult.Errors.Select(e => e.Description));
                throw new ValidationAppException($"Ошибка при обновлении имени пользователя: {errors}");
            }
        }
        

        employee.Name = dto.Name;
        employee.LastName = dto.LastName;
        employee.MiddleName = dto.MiddleName;


        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> DeleteEmployeeAsync(int id,CancellationToken cancellationToken = default)
    {
        /// Check if exists before removing
        var employee = await _unitOfWork.Employees.GetByIdAsync(id, cancellationToken);
        if (employee == null) return false;

        _unitOfWork.Employees.Delete(employee);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }
}