using Application.Common.Interfaces;
using Domain.Entities.Identity;

namespace Application.Features.Identity.Commands;

public sealed record DeleteUserCommand(int UserId) : IRequest<Result>;

public class DeleteUserCommandHandler : IRequestHandler<DeleteUserCommand, Result>
{
    private readonly IUnitOfWork unitOfWork;

    public DeleteUserCommandHandler(
        IUnitOfWork unitOfWork)
    {
        this.unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(DeleteUserCommand request, CancellationToken cancellationToken)
    {
        ApplicationUser user = await unitOfWork.IdentityRepository.GetUserAsync(request.UserId, cancellationToken);
        if (user.Id == default)
        {
            throw new InvalidOperationException(Constants.Errors.UserNotFound);
        }

        bool result = await unitOfWork.IdentityRepository.DeleteUserAsync(user, cancellationToken);

        if (!result)
        {
            return Result.CreateFailure([Constants.Errors.UserNotDeleted]);
        }

        return Result.CreateSuccess();
    }
}
