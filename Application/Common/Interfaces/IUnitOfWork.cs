using Application.Common.Models.Responses;
using Microsoft.AspNetCore.OData.Query;

namespace Application.Common.Interfaces;

public interface IUnitOfWork : IAsyncDisposable, IDisposable
{
    IIdentityRepository IdentityRepository { get; }

    ICandidateRepository CandidateRepository { get; }

    /// <summary>
    /// Persist set of changes into the data store.
    /// </summary>
    /// <param name="cancellationToken">Token for cancelling the operation.</param>
    /// <returns>The number of state entries written to database.</returns>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new database transaction. <br/>
    /// Mainly used in case of having multiple calls to SaveChangesAsync in the same context.
    /// </summary>
    /// <returns>A disposable IDbContextTransaction object.</returns>
    /// <throws><see cref="InvalidOperationException"/> when trying to being a transaction if another transaction is already started.</throws>
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Rollback Database transaction.<br/>
    /// Mainly used in case of having multiple calls to SaveChangesAsync in the same context.
    /// </summary>
    /// <returns>A disposable IDbContextTransaction object.</returns>
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Commit database transaction.<br/>
    /// Mainly used in case of having multiple calls to SaveChangesAsync in the same context.
    /// </summary>
    /// <returns>A disposable IDbContextTransaction object.</returns>
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);

    IQueryable<T> GetAsQueryable<T>()
        where T : class;

    Task<OdataResponse<T>> ODataGetAsync<T>(ODataQueryOptions<T> oDataQueryOptions, IQueryable<T> source, CancellationToken cancellationToken)
        where T : class;
}
