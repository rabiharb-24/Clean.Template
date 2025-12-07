using Microsoft.AspNetCore.Identity;

namespace Domain.Entities.Identity;

public sealed class ApplicationUserRole : IdentityUserRole<int>
{
    public ApplicationUser User { get; set; } = null!;

    public ApplicationRole Role { get; set; } = null!;
}