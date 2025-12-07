using Microsoft.AspNetCore.Identity;

namespace Application.Common.Dto;

public sealed record PermissionDto
{
    public string Permission { get; set; } = string.Empty;

    public string PermissionType { get; set; } = string.Empty;

    public class Mapping : Profile
    {
        public Mapping()
        {
            CreateMap<IdentityUserClaim<int>, PermissionDto>()
                .ForMember(x => x.Permission, opt => opt.MapFrom(src => src.ClaimValue ?? string.Empty))
                 .ForMember(x => x.PermissionType, opt => opt.MapFrom(src => src.ClaimType ?? string.Empty));
        }
    }
}
