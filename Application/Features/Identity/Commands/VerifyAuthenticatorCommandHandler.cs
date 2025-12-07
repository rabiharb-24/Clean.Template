using Application.Common.Interfaces;

namespace Application.Features.Identity.Commands;

public sealed record VerifyAuthenticatorCommand(VerifyAuthenticatorDto Dto) : IRequest<Result>;

public class VerifyAuthenticatorCommandHandler : IRequestHandler<VerifyAuthenticatorCommand, Result>
{
    private readonly IUnitOfWork unitOfWork;
    private readonly ICurrentUserService currentUserService;

    public VerifyAuthenticatorCommandHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
    {
        this.unitOfWork = unitOfWork;
        this.currentUserService = currentUserService;
    }

    public async Task<Result> Handle(VerifyAuthenticatorCommand request, CancellationToken cancellationToken)
    {
        var user = await unitOfWork.IdentityRepository.GetUserAsync(currentUserService.GetId(), cancellationToken);
        if(user is null)
        {
            return Result.CreateFailure([Constants.Errors.UserNotFound]);
        }

        var isValid = await unitOfWork.IdentityRepository.VerifyTwoFactorTokenAsync(user, request.Dto.Code);
        if (!isValid)
        {
            return Result.CreateFailure(["InvalidCode"]);
        }

        await unitOfWork.BeginTransactionAsync(cancellationToken);

        await unitOfWork.IdentityRepository.SetTwoFactorEnabled(user, true);

        user.TwoFactorType = TwoFactorTypes.AuthenticatorApp;

        await unitOfWork.IdentityRepository.UpdateUserAsync(user, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        await unitOfWork.CommitTransactionAsync(cancellationToken);

        return Result.CreateSuccess();
    }
}
