using Domain.Entities.Identity;
using Microsoft.AspNetCore.Identity;

namespace Infrastructure.Persistence.Seeds;

public static class DefaultRoles
{
    public static async Task SeedAsync(RoleManager<ApplicationRole> roleManager)
    {
        // Create admin role.
        if (await roleManager.FindByNameAsync(Roles.Admin.ToString()) is null)
        {
            await roleManager.CreateAsync(new ApplicationRole() { Name = Roles.Admin.ToString() });
        }
    }
}
