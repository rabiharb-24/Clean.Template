using Application.Common.Interfaces;
using Domain.Entities;

namespace Application.Features.Candidates.Commands;

public sealed record UpdateCandidateCommand(CandidateDto Candidate) : IRequest<Result>;

public class UpdateCandidateCommandHandler : IRequestHandler<UpdateCandidateCommand, Result>
{
    private readonly IMapper mapper;
    private readonly IUnitOfWork unitOfWork;

    public UpdateCandidateCommandHandler(
        IMapper mapper,
        IUnitOfWork unitOfWork)
    {
        this.mapper = mapper;
        this.unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(UpdateCandidateCommand request, CancellationToken cancellationToken)
    {
        Candidate? oldCandidate = await unitOfWork.CandidateRepository.GetCurrentAsync(withDetails: false, cancellationToken: cancellationToken);
        if (oldCandidate is null)
        {
            return Result.CreateFailure([Constants.Errors.CandidateNotFound]);
        }

        Candidate mappedCandidate = mapper.Map<Candidate>(request.Candidate);

        mappedCandidate.Id = oldCandidate.Id;
        mappedCandidate.ProfilePictureId = oldCandidate.ProfilePictureId;

        unitOfWork.CandidateRepository.Update(mappedCandidate);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.CreateSuccess();
    }
}
