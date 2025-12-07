using System.Linq.Expressions;
using Infrastructure.Persistence.Contexts;

namespace Infrastructure.Repositories;

public sealed class CandidateRepository : BaseRepository<Candidate>, ICandidateRepository
{
    private readonly ICurrentUserService currentUserService;

    public CandidateRepository(
        ApplicationDbContext context,
        ICurrentUserService currentUserService)
        : base(context)
    {
        this.currentUserService = currentUserService;
    }

    public async Task<Candidate?> GetCandidateAsync(int userId, bool tracking = false, bool withDetails = true, CancellationToken cancellationToken = default)
    {
        return (await GetCandidatesAsync(x => x.UserId == userId, tracking, withDetails, cancellationToken)).FirstOrDefault();
    }

    public async Task<IEnumerable<Candidate>> GetCandidatesAsync(Expression<Func<Candidate, bool>> filter, bool tracking = false, bool withDetails = true, CancellationToken cancellationToken = default)
    {
        if (withDetails)
        {
            return await GetCandidatesWithDetailsAsync(filter, tracking, cancellationToken);
        }

        return await GetCandidatesAsync(filter, tracking, cancellationToken);
    }

    public async Task<Candidate> GetCurrentAsync(bool tracking = false, bool withDetails = true, CancellationToken cancellationToken = default)
    {
        int currentUser = currentUserService.GetId();
        if (currentUser == default)
        {
            throw new InvalidOperationException(Constants.Errors.UserNotFound);
        }

        return await GetCandidateAsync(currentUser, tracking, withDetails, cancellationToken) ?? throw new InvalidOperationException(Constants.Errors.CandidateNotFound);
    }

    #region Private Methods

    private async Task<IEnumerable<Candidate>> GetCandidatesWithDetailsAsync(Expression<Func<Candidate, bool>> filter, bool tracking = false, CancellationToken cancellationToken = default)
    {
        IQueryable<Candidate> query = Context.Candidate.Where(filter);

        return tracking ? await query.ToListAsync(cancellationToken)
                        : await query.AsNoTracking().ToListAsync(cancellationToken);
    }

    private async Task<IEnumerable<Candidate>> GetCandidatesAsync(Expression<Func<Candidate, bool>> filter, bool tracking = false, CancellationToken cancellationToken = default)
    {
        IQueryable<Candidate> query = Context.Candidate.Where(filter);

        return tracking ? await query.ToListAsync(cancellationToken)
                        : await query.AsNoTracking().ToListAsync(cancellationToken);
    }

    #endregion
}
