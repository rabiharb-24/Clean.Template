using Application.Common.Interfaces;

namespace Application.Features.Identity.Queries;

public sealed record GetRolesQuery()
    : IRequest<IEnumerable<RoleDto>>;

public class GetRolesQueryHandler : IRequestHandler<GetRolesQuery, IEnumerable<RoleDto>>
{
    private readonly IMapper mapper;
    private readonly IUnitOfWork unitOfWork;

    public GetRolesQueryHandler(
        IUnitOfWork unitOfWork,
        IMapper mapper)
    {
        this.mapper = mapper;
        this.unitOfWork = unitOfWork;
    }

    public async Task<IEnumerable<RoleDto>> Handle(GetRolesQuery request, CancellationToken cancellationToken)
    {
        var roles = await unitOfWork.IdentityRepository.GetRolesAsync(cancellationToken: cancellationToken);
        if (roles is null || !roles.Any())
        {
            return [];
        }

        var claims = await unitOfWork.IdentityRepository.GetRoleClaimsAsync(x => roles.Select(x => x.Id).Contains(x.RoleId), cancellationToken);

        IEnumerable<RoleDto> mappedRoles = roles.Select(role => new RoleDto
        {
            Id = role.Id,
            Name = role.Name ?? string.Empty,
            Claims = claims.Where(c => c.RoleId == role.Id).Select(c => mapper.Map<ClaimDto>(c)),
        });

        return mappedRoles;
    }
}
