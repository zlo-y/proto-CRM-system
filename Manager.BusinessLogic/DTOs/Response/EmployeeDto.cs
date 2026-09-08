namespace Manager.BusinessLogic.DTOs.Responses;

public class EmployeeDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string MiddleName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FullName => $"{LastName} {Name} {MiddleName}".Trim();
}