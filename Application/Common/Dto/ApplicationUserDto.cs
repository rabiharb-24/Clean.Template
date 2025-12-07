using Domain.Entities.Identity;

namespace Application.Common.Dto;

public sealed record ApplicationUserDto
{
    public int Id { get; init; }

    public string Username { get; init; } = string.Empty;

    public string? NormalizedUsername { get; init; }

    public string Email { get; init; } = string.Empty;

    public string? NormalizedEmail { get; init; }

    public string? PhoneNumber { get; init; }

    public string Password { get; init; } = string.Empty;

    public bool TwoFactorEnabled { get; init; }

    public bool ReCaptchaTokenEnabled { get; init; } = true;

    public string ReCaptchaToken { get; init; } = string.Empty;

    public bool EmailConfirmed { get; init; }

    public string EmailConfirmedStatus { get; init; } = string.Empty;

    public string? SecurityStamp { get; init; }

    public string? ConcurrencyStamp { get; init; }

    public bool PhoneNumberConfirmed { get; init; }

    public DateTimeOffset? LockoutEnd { get; init; }

    public bool LockoutEnabled { get; init; }

    public int AccessFailedCount { get; init; }

    public string FirstName { get; init; } = string.Empty;

    public string? MiddleName { get; init; }

    public string LastName { get; init; } = string.Empty;

    public string? FullName { get; init; }

    public bool Active { get; init; }

    public string ActiveStatus { get; init; } = string.Empty;

    public string? OldConfirmedEmail { get; init; } = string.Empty;

    public TwoFactorTypes? TwoFactorType { get; set; }

    public DateTimeOffset CreatedAt { get; init; }

    public string RoleName { get; init; } = string.Empty;

    public ApplicationUserRole? ApplicationUserRole { get; init; }

    public IEnumerable<PermissionDto> Permissions { get; init; } = [];

    public class Mapping : Profile
    {
        public Mapping()
        {
            CreateMap<ApplicationUser, ApplicationUserDto>()
                .ForMember(x => x.Password, opt => opt.Ignore())
                .ForMember(x => x.ReCaptchaToken, opt => opt.Ignore());

            CreateMap<ApplicationUserDto, ApplicationUser>()
                .ForMember(x => x.PasswordHash, opt => opt.Ignore());
        }
    }
}