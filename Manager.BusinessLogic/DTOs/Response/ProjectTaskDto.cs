using TaskStatus = Manager.DataAccess.Enums.TaskStatus;

namespace Manager.BusinessLogic.DTOs.Responses;

public class ProjectTaskDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Comments { get; set; } = string.Empty;
    public int Priority { get; set; }
    public TaskStatus Status { get; set; }
    public int ProjectId { get; set; }

    public int? AuthorId { get; set; }
    public string? AuthorFullName { get; set; }

    public int? ExecutorId { get; set; }
    public string? ExecutorFullName { get; set; }
}