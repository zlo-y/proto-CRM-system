using Manager.BusinessLogic.Interfaces;
using Manager.DataAccess.Entities;
using Manager.BusinessLogic.DTOs;
using Manager.DataAccess.Interfaces;
using Manager.BusinessLogic.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Manager.BusinessLogic.DTOs.Responses;
using Manager.BusinessLogic.Mappings;
using Manager.BusinessLogic.Exceptions;


// 
// Сервис для управления проектами, реализующий интерфейс IProjectService. 
// 

namespace Manager.BusinessLogic.Services;

public class ProjectService: IProjectService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly FileSettings _fileSettings;
    private readonly IFileStorageService _fileStorageService;

    public ProjectService(IUnitOfWork unitOfWork , IOptions<FileSettings> fileSettings, IFileStorageService fileStorageService)
    {
        _unitOfWork = unitOfWork;
        _fileSettings = fileSettings.Value;
        _fileStorageService = fileStorageService;
    }
    
    public async Task<PagedResult<ProjectListDto>> GetAllProjectsAsync(DateTime? startFrom, DateTime? startTo, int? priority, string sortBy, string order , int pageNumber = 1, int pageSize = 10,CancellationToken cancellationToken = default)
    {
        /// Load projects with manager details for display
        var projects = _unitOfWork.Projects
              .GetAllWithIncludes(p => p.Manager)
              .AsNoTracking();

        if(startFrom.HasValue) projects = projects.Where(p => p.StartDate >= startFrom.Value);
        if(startTo.HasValue) projects = projects.Where(p => p.StartDate <= startTo.Value);
        if(priority.HasValue) projects = projects.Where(p => p.Priority == priority.Value);

        /// Dynamic sorting logic based on query parameters
        bool isDescending = order?.ToLower() == "desc";

        projects = sortBy?.ToLowerInvariant() switch
        {
            "name" => isDescending ? projects.OrderByDescending(p => p.Name) : projects.OrderBy(p => p.Name),
            "priority" => isDescending ? projects.OrderByDescending(p => p.Priority) : projects.OrderBy(p => p.Priority),
            "startdate" => isDescending ? projects.OrderByDescending(p => p.StartDate) : projects.OrderBy(p => p.StartDate),
            _ => projects.OrderBy(p => p.Id)
        };

        var (items, totalCount) = await _unitOfWork.Projects.GetPaginatedAsync(projects, pageNumber, pageSize,cancellationToken );

        var result = new PagedResult<Project>
        {
            Items = items.ToList(),
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };

        return result.ToPagedDto(p => p.ToListDto());
    }


    public async Task CreateProjectAsync(ProjectServiceDto dto, CancellationToken cancellationToken = default)
    {
        var managerExists = await _unitOfWork.Employees.AnyAsync(e => e.Id == dto.ManagerId ,cancellationToken);
        
        if (!managerExists)
        {
            throw new ArgumentException($"Ошибка создания проекта: Выбранный руководитель (ID: {dto.ManagerId}) не найден в системе.");
        }

        if(dto.StartDate > dto.EndDate)
            throw new ArgumentException("Start date must be before end date.");

        if(dto.UploadedFiles != null && dto.UploadedFiles.Count > 0)
        {
            foreach(var file in dto.UploadedFiles)
            {
                if(file.Length > _fileSettings.MaxFileSize)
                {
                    throw new ValidationAppException($"Файл '{file.FileName}' превышает допустимый размер.");
                }
                if (!_fileSettings.IsExtensionAllowed(file.FileName))
                {
                    throw new ValidationAppException($"Тип файла '{Path.GetExtension(file.FileName)}' запрещен.");
                }
            }
        }


        var project = new Project
        {
            Name = dto.Name,
            Customer = dto.Customer,
            Executor = dto.Executor,
            StartDate = dto.StartDate,
            EndDate = dto.EndDate,
            Priority = dto.Priority,
            ManagerId = dto.ManagerId
        };

        /// Link selected employees to the new project
        if (dto.ExecutorId != null && dto.ExecutorId.Any())
        {
                project.ProjectEmployees = dto.ExecutorId
                    .Distinct()
                    .Select(empId => new ProjectEmployee { EmployeeId = empId })
                    .ToList();
        }

       var savedFiles = new List<string>();

    try
    {
        if (dto.UploadedFiles != null && dto.UploadedFiles.Count > 0)
        {
            foreach (var file in dto.UploadedFiles)
            {
                var relativeFilePath = await _fileStorageService.SaveFileAsync(file, cancellationToken);
                savedFiles.Add(relativeFilePath);

                project.ProjectDocuments.Add(new ProjectDocument
                {
                    FileName = file.FileName,
                    FilePath = relativeFilePath
                });
            }
        }

        await _unitOfWork.Projects.AddAsync(project, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
    catch
    {
        // Откатываем сохраненные файлы, если запись упадет на этапе сохранения на диск или в БД
        foreach (var file in savedFiles)
        {
            try
            {
                _fileStorageService.DeleteFile(file);
            }
            catch
            {
            
            }
        }
        throw;
    }
}

    public async Task<ProjectDetailDto?> GetProjectByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var project = await _unitOfWork.Projects.GetAll()
            .AsNoTracking()
        .Include(p => p.Manager)
        .Include(p => p.ProjectDocuments)
        .Include(p => p.ProjectEmployees)
            .ThenInclude(pe => pe.Employee)
                .ThenInclude(e => e.User)
        .Include(p => p.ProjectTasks)
            .ThenInclude(t => t.Author)
        .Include(p => p.ProjectTasks)
            .ThenInclude(t => t.Executor)
        .AsSplitQuery() 
        .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

        return project?.ToDetailDto();
        
   } 

    public async Task UpdateProjectAsync(ProjectUpdateDto project, int currentEmployeeId, bool isAdmin, CancellationToken cancellationToken = default)
    {
        var projectEntity = await _unitOfWork.Projects.GetByIdAsync(project.Id, cancellationToken);
        if(projectEntity == null)
        {
            throw new ArgumentException($"Project with ID {project.Id} not found.");
        }
        if(!isAdmin && projectEntity.ManagerId != currentEmployeeId)
        {
            throw new ForbiddenException("You do not have permission to update this project.");
        }

        projectEntity.Name = project.Name;
        projectEntity.Customer = project.Customer;
        projectEntity.Executor = project.Executor;
        projectEntity.Priority = project.Priority;
        projectEntity.EndDate = project.EndDate;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> DeleteProjectAsync(int id, int currentEmployeeId, bool isAdmin,CancellationToken cancellationToken = default)
    {
        var project = await _unitOfWork.Projects.GetByIdAsync(id, cancellationToken);
        if (project == null)
        {
            throw new NotFoundException($"Project with ID{id} not found");
        }
        

        if(!isAdmin && project.ManagerId != currentEmployeeId)
        {
            throw new ForbiddenException("You do not have permission to delete this project.");
        }

        var documetsQuery = _unitOfWork.ProjectDocuments.GetAll().Where(pd => pd.ProjectId == id);
        var documents = await _unitOfWork.ProjectDocuments.ToListAsync(documetsQuery, cancellationToken);

        
        _unitOfWork.Projects.Delete(project);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        foreach (var document in documents)
        {
            _fileStorageService.DeleteFile(document.FilePath);
        }

        return true;
    }

    public async Task AddEmployeeProject(int projectId, int employeeId, int currentEmployeeId, bool isAdmin,CancellationToken cancellationToken = default)
    {
        var project = await _unitOfWork.Projects.GetByIdAsync(projectId, cancellationToken);
        if (project == null)
        {
            throw new ArgumentException($"Project with ID {projectId} not found.");
        }
        if(!isAdmin && project.ManagerId != currentEmployeeId)
        {
            throw new ForbiddenException("You do not have permission to add employees to this project.");
        }

        var employeeExists = await _unitOfWork.Employees.AnyAsync(e => e.Id == employeeId, cancellationToken);

    if (!employeeExists)
        throw new NotFoundException($"Сотрудник с ID {employeeId} не найден.");

    var exist = await _unitOfWork.ProjectEmployees.AnyAsync(
        pe => pe.ProjectId == projectId && pe.EmployeeId == employeeId, cancellationToken);

    if (exist)
    {
        throw new ValidationAppException($"Employee with ID {employeeId} is already assigned to this project.");
    }
        await _unitOfWork.ProjectEmployees.AddAsync(
            new ProjectEmployee { ProjectId = projectId, EmployeeId = employeeId }, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveEmployeeFromProjectAsync(int projectId, int employeeId, int currentEmployeeId, bool isAdmin,CancellationToken cancellationToken = default)
    {

        var project = await _unitOfWork.Projects.GetByIdAsync(projectId, cancellationToken);

        if (project == null)
        {
            throw new ArgumentException($"Project with ID {projectId} not found.");
        }

        if(!isAdmin && project.ManagerId != currentEmployeeId)
        {
            throw new ForbiddenException("You do not have permission to remove employees from this project.");
        }

        var link = await _unitOfWork.ProjectEmployees.GetAll()
            .FirstOrDefaultAsync(pe=> pe.ProjectId == projectId && pe.EmployeeId == employeeId , cancellationToken);
        if (link == null)
        {
            throw new NotFoundException($"Employee with ID {employeeId} is not assigned to project {projectId}.");
        }

        _unitOfWork.ProjectEmployees.Delete(link);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}