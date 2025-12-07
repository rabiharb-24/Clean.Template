using System.Linq.Expressions;
using Domain.Entities;

namespace Application.Common.Interfaces.Repositories;

public interface ICandidateRepository : IRepository<Candidate>
{
    Task<IEnumerable<Candidate>> GetCandidatesAsync(Expression<Func<Candidate, bool>> filter, bool tracking = false, bool withDetails = true, CancellationToken cancellationToken = default);

    Task<Candidate?> GetCandidateAsync(int userId, bool tracking = false, bool withDetails = true, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets current candidate.
    /// </summary>
    Task<Candidate> GetCurrentAsync(bool tracking = false, bool withDetails = true, CancellationToken cancellationToken = default);
}
