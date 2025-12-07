using Application.Common.Interfaces;
using Application.Common.Models.Responses;

namespace Application.Features.Identity.Queries;

public sealed record GetAccessTokenQuery(LoginParametersDto UserCredentials)
    : IRequest<Result<LoginResponse>>;

public sealed class GetAccessTokenQueryHandler : IRequestHandler<GetAccessTokenQuery, Result<LoginResponse>>
{
    private readonly IUnitOfWork unitOfWork;

    public GetAccessTokenQueryHandler(
        IUnitOfWork unitOfWork)
    {
        this.unitOfWork = unitOfWork;
    }

    public async Task<Result<LoginResponse>> Handle(GetAccessTokenQuery request, CancellationToken cancellationToken)
    {
        var result = await unitOfWork.IdentityRepository.GetAccessTokenAsync(request.UserCredentials, cancellationToken);

        return Result<LoginResponse>.CreateSuccess(result);
    }
}
