namespace Manager.BusinessLogic.DTOs.Responses;

// Для списков — только сводные поля, без вложенных коллекций
public class ProjectListDto
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
}