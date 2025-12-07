using Application.Common.Interfaces;

namespace Application.Features.Candidates.Queries;
public sealed record GetCandidateQuery(int UserId)
    : IRequest<Result<CandidateDto>>;

public class GetCandidateQueryHandler : IRequestHandler<GetCandidateQuery, Result<CandidateDto>>
{
    private readonly IUnitOfWork unitOfWork;
    private readonly IMapper mapper;
    private readonly ICurrentUserService currentUserService;

    public GetCandidateQueryHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IMapper mapper)
    {
        this.unitOfWork = unitOfWork;
        this.mapper = mapper;
        this.currentUserService = currentUserService;
    }

    public async Task<Result<CandidateDto>> Handle(GetCandidateQuery request, CancellationToken cancellationToken)
    {
        var candidate = await unitOfWork.CandidateRepository.GetCandidateAsync(request.UserId, cancellationToken: cancellationToken);
        if (candidate is null)
        {
            return Result<CandidateDto>.CreateFailure([Constants.Errors.CandidateNotFound]);
        }

        return Result<CandidateDto>.CreateSuccess(mapper.Map<CandidateDto>(candidate));
    }
}
