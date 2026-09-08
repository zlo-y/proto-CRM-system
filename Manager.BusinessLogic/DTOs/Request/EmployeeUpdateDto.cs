using System.ComponentModel.DataAnnotations;

namespace Manager.BusinessLogic.DTOs;

public class EmployeeUpdateDto 
{
    [Required]
    public int Id { get; set; }

    [Required(ErrorMessage = "Имя обязательно для заполнения.")]
    [StringLength(50, ErrorMessage = "Имя не может превышать 50 символов.")]
    public string Name { get; set; } = string.Empty;
    [Required(ErrorMessage = "Фамилия обязательна для заполнения.")]
    [StringLength(50, ErrorMessage = "Фамилия не может превышать 50 символов.")]
    public string LastName { get; set; } = string.Empty;
    public string MiddleName { get; set; } = string.Empty;
    [Required(ErrorMessage = "Email обязателен.")]
    [EmailAddress(ErrorMessage = "Некорректный формат Email.")]
    public string Email { get; set; } = string.Empty;
}