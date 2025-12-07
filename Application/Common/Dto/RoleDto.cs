using Domain.Entities.Identity;

namespace Application.Common.Dto;

public sealed record RoleDto
{
    public int Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public IEnumerable<ClaimDto> Claims { get; init; } = [];

    public class Mapping : Profile
    {
        public Mapping()
        {
            CreateMap<RoleDto, ApplicationRole>();

            CreateMap<ApplicationRole, RoleDto>();
        }
    }
}
