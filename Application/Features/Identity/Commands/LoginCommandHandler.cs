using Application.Common.Interfaces;
using Application.Common.Models.Responses;

namespace Application.Features.Identity.Commands;

public sealed record LoginCommand(LoginParametersDto Parameters) : IRequest<Result<LoginResponse>>;

public class LoginCommandHandler : IRequestHandler<LoginCommand, Result<LoginResponse>>
{
    private readonly IUnitOfWork unitOfWork;

    public LoginCommandHandler(IUnitOfWork unitOfWork)
    {
        this.unitOfWork = unitOfWork;
    }

    public async Task<Result<LoginResponse>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var result = await unitOfWork.IdentityRepository.LoginAsync(request.Parameters, cancellationToken);
        if (!result.Success)
        {
            return Result<LoginResponse>.CreateFailure([result.Error ?? new(Constants.Errors.ErrorOccured)], result.StatusCode);
        }

        return Result<LoginResponse>.CreateSuccess(result);
    }
}
