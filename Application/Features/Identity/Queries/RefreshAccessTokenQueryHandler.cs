using Application.Common.Interfaces;
using Application.Common.Models.Responses;

namespace Application.Features.Identity.Queries;

public sealed record RefreshAccessTokenQuery(UserCredentials UserCredentials)
    : IRequest<Result<AuthenticateResponse>>;

public sealed class RefreshAccessTokenQueryHandler : IRequestHandler<RefreshAccessTokenQuery, Result<AuthenticateResponse>>
{
    private readonly IUnitOfWork unitOfWork;

    public RefreshAccessTokenQueryHandler(
        IUnitOfWork unitOfWork)
    {
        this.unitOfWork = unitOfWork;
    }

    public async Task<Result<AuthenticateResponse>> Handle(RefreshAccessTokenQuery request, CancellationToken cancellationToken)
    {
        AuthenticateResponse result = await unitOfWork.IdentityRepository.RefreshTokenAsync(request.UserCredentials, cancellationToken);

        return string.IsNullOrEmpty(result.Error)
            ? Result<AuthenticateResponse>.CreateSuccess(result)
            : Result<AuthenticateResponse>.CreateFailure([result.Error]);
    }
}
