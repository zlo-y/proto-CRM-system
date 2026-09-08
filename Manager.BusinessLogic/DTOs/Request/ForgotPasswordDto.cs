using System.ComponentModel.DataAnnotations;

namespace Manager.BusinessLogic.DTOs;

public class ForgotPasswordDto
{
    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Invalid email address.")]
    public string Email { get; set; } = string.Empty;
}