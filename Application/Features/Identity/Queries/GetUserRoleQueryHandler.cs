using Application.Common.Interfaces;
using Domain.Entities.Identity;

namespace Application.Features.Identity.Queries;

public sealed record GetUserRoleQuery()
    : IRequest<RoleDto?>;

public class GetUserRoleQueryHandler : IRequestHandler<GetUserRoleQuery, RoleDto?>
{
    private readonly ICurrentUserService currentUser;
    private readonly IUnitOfWork unitOfWork;
    private readonly IMapper mapper;

    public GetUserRoleQueryHandler(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ICurrentUserService currentUser)
    {
        this.currentUser = currentUser;
        this.unitOfWork = unitOfWork;
        this.mapper = mapper;
    }

    public async Task<RoleDto?> Handle(GetUserRoleQuery request, CancellationToken cancellationToken)
    {
        var userId = currentUser.GetId();
        if(userId == default)
        {
            throw new InvalidOperationException(Constants.Errors.UserNotFound);
        }

        ApplicationRole role = await unitOfWork.IdentityRepository.GetUserRoleAsync(userId, cancellationToken);
        if (role.Id == default)
        {
            throw new InvalidOperationException(Constants.Errors.RoleDoesNotExist);
        }

        return mapper.Map<RoleDto>(role);
    }
}
