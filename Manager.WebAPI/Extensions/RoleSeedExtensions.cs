using Microsoft.AspNetCore.Identity;

namespace Manager.WebAPI.Extensions;

// 
// Используется для инициализации ролей в системе, создавая необходимые роли, если они еще не существуют.
// 

public static class RoleSeedExtensions
{
    public static async Task SeedRolesAsync(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<int>>>();

        string[] roles =  { "Admin", "Employee" };

        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole<int>(role));
            }
        }
    }
}