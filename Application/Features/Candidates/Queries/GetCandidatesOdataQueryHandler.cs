using Application.Common.Interfaces;
using Application.Common.Models.Responses;
using Domain.Entities;
using Microsoft.AspNetCore.OData.Query;

namespace Application.Features.Candidates.Queries;

public sealed record GetCandidatesOdataQuery(ODataQueryOptions<CandidateDto> FilterOptions)
    : IRequest<Result<OdataResponse<CandidateDto>>>;

public class GetCandidatesOdataQueryHandler : IRequestHandler<GetCandidatesOdataQuery, Result<OdataResponse<CandidateDto>>>
{
    private readonly IUnitOfWork unitOfWork;
    private readonly ICurrentUserService currentUserService;

    public GetCandidatesOdataQueryHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
    {
        this.unitOfWork = unitOfWork;
        this.currentUserService = currentUserService;
    }

    public async Task<Result<OdataResponse<CandidateDto>>> Handle(GetCandidatesOdataQuery request, CancellationToken cancellationToken)
    {
        var query = unitOfWork.GetAsQueryable<Candidate>()
          .Select(x => new CandidateDto
          {
              Id = x.Id,
              UserId = x.UserId,
              FullName = x.FullName
          });

        var result = await unitOfWork.ODataGetAsync(request.FilterOptions, query, cancellationToken);

        return Result<OdataResponse<CandidateDto>>.CreateSuccess(result);
    }
}
