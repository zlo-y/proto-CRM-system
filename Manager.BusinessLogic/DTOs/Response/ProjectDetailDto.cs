

namespace Manager.BusinessLogic.DTOs.Responses;

// Для карточки проекта — с полными связанными данными
public class ProjectDetailDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Customer { get; set; } = string.Empty;
    public string Executor { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int Priority { get; set; }

    public int ManagerId { get; set; }
    public string? ManagerFullName { get; set; }

    public List<EmployeeDto> Employees { get; set; } = new();
    public List<ProjectTaskDto> Tasks { get; set; } = new();
    public List<ProjectDocumentDto> Documents { get; set; } = new();
}