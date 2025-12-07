using Application.Common.Interfaces.Services;
using Application.Common.Models;
using Application.Common.Models.Responses;
using Application.Features.Candidates.Commands;
using Application.Features.Candidates.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.OData.Query;

namespace Api.Controllers;

[Authorize]
public sealed class CandidatesController : BaseController
{
    public CandidatesController(IMediator mediator, ICurrentUserService currentUserService)
        : base(mediator, currentUserService)
    {
    }

    /// <summary>
    /// Retrieves a candidate by their user ID.
    /// </summary>
    /// <param name="id">The unique user identifier of the candidate.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>Returns the candidate details if found; otherwise, returns a bad request with errors.</returns>
    [HttpGet]
    [Route("{id:int}")]
    [ProducesResponseType(typeof(CandidateDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Get([FromRoute] int id, CancellationToken cancellationToken)
    {
        Result<CandidateDto> result = await Mediator.Send(new GetCandidateQuery(id), cancellationToken);
        if (!result.Success)
        {
            return StatusCode(result.StatusCode, result.Errors);
        }

        return Ok(result.Value);
    }

    /// <summary>
    ///  Retrieves a list of candidates using OData query options for filtering, sorting, and pagination.
    /// </summary>
    /// Note: If role is admin, retrieve all candidates.</param>
    /// <param name="filterOptions">The OData query options to apply for filtering, sorting, and pagination.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>An <see cref="OdataResponse{CandidateInfo}"/> containing the filtered and paginated list of candidates.</returns>
    [HttpGet]
    [Route("odata")]
    [ProducesResponseType(typeof(OdataResponse<CandidateDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ODataGet(ODataQueryOptions<CandidateDto> filterOptions, CancellationToken cancellationToken)
    {
        Result<OdataResponse<CandidateDto>> result = await Mediator.Send(new GetCandidatesOdataQuery(filterOptions), cancellationToken);
        if (!result.Success)
        {
            return StatusCode(result.StatusCode, result.Errors);
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Update candidate
    /// </summary>
    /// <param name="candidate">The candidate data to update.</param>
    /// <param name="cancellation">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>Returns OK if the update was successful; otherwise, returns a bad request with errors.</returns>
    [HttpPut]
    [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
    public async Task<IActionResult> Update([FromBody] CandidateDto candidate, CancellationToken cancellation)
    {
        Result result = await Mediator.Send(new UpdateCandidateCommand(candidate), cancellation);
        if (!result.Success)
        {
            return StatusCode(result.StatusCode, result.Errors);
        }

        return Ok(candidate.Id);
    }

    /// <summary>
    /// Create candidate
    /// </summary>
    /// <param name="candidate">The candidate data to create.</param>
    /// <param name="cancellation">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>Returns OK if the update was successful; otherwise, returns a bad request with errors.</returns>
    [HttpPost]
    [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
    public async Task<IActionResult> Create
        ([FromBody] CandidateDto candidate, CancellationToken cancellation)
    {
        Result result = await Mediator.Send(new CreateCandidateCommand(candidate), cancellation);
        if (!result.Success)
        {
            return StatusCode(result.StatusCode, result.Errors);
        }

        return Ok(candidate.Id);
    }
}
