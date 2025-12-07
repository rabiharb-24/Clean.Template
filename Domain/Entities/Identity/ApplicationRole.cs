using Microsoft.AspNetCore.Identity;

namespace Domain.Entities.Identity;

public sealed class ApplicationRole : IdentityRole<int>
{
    public ICollection<ApplicationUserRole> UserRoles { get; set; } = new List<ApplicationUserRole>();
}
