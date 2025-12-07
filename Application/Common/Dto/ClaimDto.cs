using Microsoft.AspNetCore.Identity;

namespace Application.Common.Dto;

public sealed class ClaimDto
{
    public string ClaimType { get; init; } = string.Empty;

    public string ClaimValue { get; init; } = string.Empty;

    public class Mapping : Profile
    {
        public Mapping()
        {
            CreateMap<IdentityUserClaim<int>, ClaimDto>();

            CreateMap<IdentityRoleClaim<int>, ClaimDto>();
        }
    }
}
