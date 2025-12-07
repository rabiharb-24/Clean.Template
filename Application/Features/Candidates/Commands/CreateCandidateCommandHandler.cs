using Application.Common.Interfaces;
using Domain.Entities;

namespace Application.Features.Candidates.Commands;

public sealed record CreateCandidateCommand(CandidateDto Candidate) : IRequest<Result>;

public class CreateCandidateCommandHandler : IRequestHandler<UpdateCandidateCommand, Result>
{
    private readonly IMapper mapper;
    private readonly IUnitOfWork unitOfWork;

    public CreateCandidateCommandHandler(
        IMapper mapper,
        IUnitOfWork unitOfWork)
    {
        this.mapper = mapper;
        this.unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(UpdateCandidateCommand request, CancellationToken cancellationToken)
    {
        Candidate mappedCandidate = mapper.Map<Candidate>(request.Candidate);

        await unitOfWork.CandidateRepository.CreateAsync(mappedCandidate, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.CreateSuccess();
    }
}
