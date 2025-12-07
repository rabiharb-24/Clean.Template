using Application.Common.Interfaces;
using Domain.Entities.Identity;

namespace Application.Features.Identity.Queries;

public sealed record UserInRoleQuery(string RoleName)
    : IRequest<bool>;

public class UserInRoleQueryHandler : IRequestHandler<UserInRoleQuery, bool>
{
    private readonly ICurrentUserService currentUser;
    private readonly IUnitOfWork unitOfWork;

    public UserInRoleQueryHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser)
    {
        this.currentUser = currentUser;
        this.unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(UserInRoleQuery request, CancellationToken cancellationToken)
    {
        var userId = currentUser.GetId();
        if (userId == default)
        {
            return false;
        }

        ApplicationUser user = await unitOfWork.IdentityRepository.GetUserAsync(userId, cancellationToken);
        if (user.Id == default)
        {
            return false;
        }

        return await unitOfWork.IdentityRepository.IsInRoleAsync(userId, request.RoleName, cancellationToken);
    }
}
