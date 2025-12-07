using Domain.Entities.Identity;
using Infrastructure.Persistence.Contexts;
using Microsoft.AspNetCore.Identity;
using Serilog;

namespace Infrastructure.Persistence;

public sealed class DatabaseInitializer
{
    public DatabaseInitializer(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager,
        IUnitOfWork unitOfWork)
    {
        this.context = context;
        this.userManager = userManager;
        this.roleManager = roleManager;
        this.unitOfWork = unitOfWork;
    }

    private readonly ApplicationDbContext context;
    private readonly UserManager<ApplicationUser> userManager;
    private readonly RoleManager<ApplicationRole> roleManager;
    private readonly IUnitOfWork unitOfWork;

    public async Task InitializeAsync()
    {
        try
        {
            await context.Database.MigrateAsync();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "DatabaseInitialization");
            throw;
        }
    }

    public async Task SeedAsync()
    {
        try
        {
            await TrySeedAsync();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "DatabaseInitialization");
            throw;
        }
    }

    public async Task TrySeedAsync()
    {
        await Seeds.DefaultRoles.SeedAsync(roleManager);
        await Seeds.DefaultUsers.SeedSuperAdminAsync(userManager, unitOfWork);
    }
}