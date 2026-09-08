using System.ComponentModel.DataAnnotations;

namespace Manager.BusinessLogic.DTOs;

public class ProjectUpdateDto 
{
    [Required]
    public int Id { get; set; }
    [Required, StringLength(100)]
    public string Name { get; set; } = string.Empty;
    [Required]
    public string Customer { get; set; } = string.Empty;
    [Required]
    public string Executor { get; set; } = string.Empty;
    [Range(1, 3)]
    public int Priority { get; set; }
    public DateTime EndDate { get; set; }
}