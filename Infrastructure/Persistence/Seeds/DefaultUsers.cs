using Domain.Entities.Identity;
using Microsoft.AspNetCore.Identity;

namespace Infrastructure.Persistence.Seeds;
public static class DefaultUsers
{
    public static async Task SeedSuperAdminAsync(UserManager<ApplicationUser> userManager, IUnitOfWork unitOfWork)
    {
        ApplicationUser defaultUser = new()
        {
            UserName = Constants.Seeds.DefaultUser.Username,
            Email = Constants.Seeds.DefaultUser.Email,
            Active = true,
            FirstName = Constants.Seeds.DefaultUser.FirstName,
            LastName = Constants.Seeds.DefaultUser.LastName,
            EmailConfirmed = true,
        };

        ApplicationUser? user = await userManager.FindByNameAsync(defaultUser.UserName);
        if (user is not null)
        {
            return;
        }

        IdentityResult result = await userManager.CreateAsync(defaultUser, Constants.Seeds.DefaultUser.Password);
        if (result.Succeeded)
        {
            await userManager.SetLockoutEnabledAsync(defaultUser, false);
            await userManager.AddToRoleAsync(defaultUser, Roles.Admin.ToString());
            await unitOfWork.CandidateRepository.CreateAsync(new Candidate() { UserId = defaultUser.Id });

            await unitOfWork.SaveChangesAsync();
        }
    }
}