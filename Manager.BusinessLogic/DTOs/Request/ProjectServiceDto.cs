using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace Manager.BusinessLogic.DTOs;

/// DTO for handling project creation requests including files
public class ProjectServiceDto
{
    [Required(ErrorMessage = "Название проекта обязательно.")]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Заказчик обязателен.")]
    public string Customer { get; set; } = string.Empty;

    [Required(ErrorMessage = "Исполнитель обязателен.")]
    public string Executor { get; set; } = string.Empty;

    [Required]
    public DateTime StartDate { get; set; }
    [Required]
    public DateTime EndDate { get; set; }

    [Range(1, 3, ErrorMessage = "Приоритет должен быть от 1 до 10.")]
    public int Priority { get; set; }

    [Required]

    public int ManagerId { get; set; }
    public List<int> ExecutorId { get; set; } = new();

    /// Files attached during project creation
    public List<IFormFile> UploadedFiles { get; set; } = new();
}