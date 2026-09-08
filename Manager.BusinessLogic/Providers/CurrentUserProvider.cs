using System.Security.Claims;
using Manager.BusinessLogic.Interfaces;
using Microsoft.AspNetCore.Http;

namespace Manager.BusinessLogic.Providers;

public class CurrentUserProvider : ICurrentUserProvider
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserProvider(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public int GetCurrentUserId()
    {
        var userIdClaim = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId))
        {
            return userId;
        }
        throw new InvalidOperationException("User ID claim is missing or invalid.");
    }

    public int GetEmployeeId()
    {
        var employeeIdClaim = _httpContextAccessor.HttpContext?.User?.FindFirst("EmployeeId");
        if (employeeIdClaim != null && int.TryParse(employeeIdClaim.Value, out int employeeId))
        {
            return employeeId;
        }
        throw new InvalidOperationException("Employee ID claim is missing or invalid.");
    }

    public bool IsAdmin()
    {
        return _httpContextAccessor.HttpContext?.User?.IsInRole("Admin") ?? false;
    }
}