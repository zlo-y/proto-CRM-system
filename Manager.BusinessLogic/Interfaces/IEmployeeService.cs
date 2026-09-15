using Manager.BusinessLogic.DTOs;
using Manager.BusinessLogic.DTOs.Responses;
using Manager.DataAccess.Entities;

namespace Manager.BusinessLogic.Interfaces;

// 
// Интерфейс для сервиса управления сотрудниками, предоставляющий методы для получения списка сотрудников, обновления информации о сотруднике и удаления сотрудника.
// 

public interface IEmployeeService
{
    Task<IEnumerable<EmployeeDto>> GetAllEmployeesAsync(string search,CancellationToken cancellationToken = default);
    Task UpdateEmployeeAsync(EmployeeUpdateDto dto, CancellationToken cancellationToken = default);
    Task<bool> DeleteEmployeeAsync(int id,CancellationToken cancellationToken = default);
}