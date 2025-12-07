using Application.Common.Interfaces;

namespace Application.Features.Identity.Commands;

public sealed record EnableAuthenticatorCommand() : IRequest<Result<GetAuthenticatorResponseDto>>;

public class EnableAuthenticatorCommandHandler : IRequestHandler<EnableAuthenticatorCommand, Result<GetAuthenticatorResponseDto>>
{
    private readonly IUnitOfWork unitOfWork;
    private readonly ICurrentUserService currentUserService;

    public EnableAuthenticatorCommandHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
    {
        this.unitOfWork = unitOfWork;
        this.currentUserService = currentUserService;
    }

    public async Task<Result<GetAuthenticatorResponseDto>> Handle(EnableAuthenticatorCommand request, CancellationToken cancellationToken)
    {
        var user = await unitOfWork.IdentityRepository.GetUserAsync(currentUserService.GetId(), cancellationToken);
        if(user is null)
        {
            return Result<GetAuthenticatorResponseDto>.CreateFailure([Constants.Errors.UserNotFound]);
        }

        var key = await unitOfWork.IdentityRepository.GetAuthenticatorKeyAsync(user);
        if (string.IsNullOrEmpty(key))
        {
            await unitOfWork.IdentityRepository.ResetAuthenticatorKeyAsync(user);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            key = await unitOfWork.IdentityRepository.GetAuthenticatorKeyAsync(user);
        }

        var uri = $"otpauth://totp/**Tenant_Name**:{user.UserName}?secret={key}&issuer=**IssuerName**";

        return Result<GetAuthenticatorResponseDto>.CreateSuccess(new() { AuthenticatorUri = uri, TwoFactorSetupKey = key });
    }
}
