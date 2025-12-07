using Application.Common.Extensions;
using Application.Common.Interfaces;
using Domain.Entities.Identity;

namespace Application.Features.Identity.Commands;

public sealed record DeactivateAccountCommand() : IRequest<Result>;

public class DeactivateAccountCommandHandler : IRequestHandler<DeactivateAccountCommand, Result>
{
    private readonly ICurrentUserService currentUserService;
    private readonly IUnitOfWork unitOfWork;

    public DeactivateAccountCommandHandler(
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork)
    {
        this.currentUserService = currentUserService;
        this.unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(DeactivateAccountCommand request, CancellationToken cancellationToken)
    {
        int currentUser = currentUserService.GetId();
        if (currentUser == default)
        {
            throw new InvalidOperationException(Constants.Errors.UserNotFound);
        }

        ApplicationUser user = await unitOfWork.IdentityRepository.GetUserAsync(currentUser, cancellationToken);
        if (user.Id == default)
        {
            throw new InvalidOperationException(Constants.Errors.UserNotFound);
        }

        user.Active = false;

        return (await unitOfWork.IdentityRepository.UpdateUserAsync(user, cancellationToken)).ToApplicationResult();
    }
}
