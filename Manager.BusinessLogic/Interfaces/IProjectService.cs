using Manager.BusinessLogic.DTOs;
using Manager.DataAccess.Entities;
using Manager.BusinessLogic.Models;
using Manager.BusinessLogic.DTOs.Responses;
using System.Threading.Channels;

namespace Manager.BusinessLogic.Interfaces;

public interface IProjectService
{
    // В Manager.BusinessLogic.Interfaces.IProjectService
    Task<PagedResult<ProjectListDto>> GetAllProjectsAsync(
    DateTime? startFrom, DateTime? startTo, int? priority, 
    string sortBy, string order, 
    int pageNumber, int pageSize, CancellationToken cancellationToken = default); 
    Task CreateProjectAsync(ProjectServiceDto dto , CancellationToken cancellationToken = default);
    Task<ProjectDetailDto?> GetProjectByIdAsync(int id, CancellationToken cancellationToken = default);


    Task UpdateProjectAsync(ProjectUpdateDto project, int currentEmployeeId, bool isAdmin, CancellationToken cancellationToken = default);
    Task <bool> DeleteProjectAsync(int id,int currentEmployeeId, bool isAdmin, CancellationToken cancellationToken = default);
    Task AddEmployeeProject(int projectId , int employeeId,int currentEmployeeId, bool isAdmin, CancellationToken cancellationToken = default);
    Task RemoveEmployeeFromProjectAsync(int projectId, int employeeId,int currentEmployeeId, bool isAdmin, CancellationToken cancellationToken = default);

}

