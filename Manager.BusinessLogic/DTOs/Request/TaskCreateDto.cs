using System.ComponentModel.DataAnnotations;

namespace Manager.BusinessLogic.DTOs;

/// DTO for creating a new task and linking it to a project
public class TaskCreateDto
{
    [Required(ErrorMessage = "Название задачи обязательно.")]
    [StringLength(150)]
    public string Name { get; set; } = string.Empty;
    public string Comments { get; set; } = string.Empty;
    [Range(1, 3, ErrorMessage = "Приоритет должен быть от 1 до 3.")]
    public int Priority { get; set; }
    [Required]
    public int ProjectId { get; set; }
    public int? ExecutorId { get; set; }
}