using System.ComponentModel.DataAnnotations;

namespace Manager.BusinessLogic.DTOs;

public class RegisterDto
{
    [Required(ErrorMessage = "Email обязателен.")]
    [EmailAddress(ErrorMessage = "Некорректный формат Email.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Пароль обязателен.")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Пароль должен быть не менее 6 символов.")]
    public string Password { get; set; } = string.Empty;
    [Required(ErrorMessage = "Имя обязательно.")]
    public string FirstName { get; set; } = string.Empty;
    [Required(ErrorMessage = "Фамилия обязательна.")]
    public string LastName { get; set; } = string.Empty;
}