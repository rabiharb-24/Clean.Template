using Application.Common.Interfaces;
using Application.Common.Models.Responses;

namespace Application.Features.Identity.Commands;

public sealed record LoginCallbackCommand(string Code, string State) : IRequest<Result<LoginResponse>>;

public class LoginCallbackCommandHandler : IRequestHandler<LoginCallbackCommand, Result<LoginResponse>>
{
    private readonly IUnitOfWork unitOfWork;

    public LoginCallbackCommandHandler(IUnitOfWork unitOfWork)
    {
        this.unitOfWork = unitOfWork;
    }

    public async Task<Result<LoginResponse>> Handle(LoginCallbackCommand request, CancellationToken cancellationToken)
    {
        var result = await unitOfWork.IdentityRepository.LoginCallback(request.Code, request.State, cancellationToken);
        if (!result.Success)
        {
            return Result<LoginResponse>.CreateFailure([result.Error ?? new(Constants.Errors.ErrorOccured)], result.StatusCode);
        }

        return Result<LoginResponse>.CreateSuccess(result);
    }
}
