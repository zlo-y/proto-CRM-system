using Microsoft.AspNetCore.Identity;
using Manager.DataAccess.Entities;

using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;

namespace Manager.WebAPI.Extensions;

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