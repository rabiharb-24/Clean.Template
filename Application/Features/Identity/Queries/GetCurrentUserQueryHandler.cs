using Application.Common.Interfaces;

namespace Application.Features.Identity.Queries;

public sealed record GetCurrentUserQuery()
    : IRequest<Result<ApplicationUserDto>>;

public class GetCurrentUserQueryHandler : IRequestHandler<GetCurrentUserQuery, Result<ApplicationUserDto>>
{
    private readonly IMapper mapper;
    private readonly IUnitOfWork unitOfWork;
    private readonly ICurrentUserService currentUserService;

    public GetCurrentUserQueryHandler(
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork,
        IMapper mapper)
    {
        this.mapper = mapper;
        this.unitOfWork = unitOfWork;
        this.currentUserService = currentUserService;
    }

    public async Task<Result<ApplicationUserDto>> Handle(GetCurrentUserQuery request, CancellationToken cancellationToken)
    {
        var user = await unitOfWork.IdentityRepository.GetUserAsync(currentUserService.GetId(), cancellationToken);
        if (user.Id == default)
        {
            throw new InvalidOperationException(Constants.Errors.UserNotFound);
        }

        ApplicationUserDto mappedUser = mapper.Map<ApplicationUserDto>(user);

        return Result<ApplicationUserDto>.CreateSuccess(mappedUser);
    }
}
