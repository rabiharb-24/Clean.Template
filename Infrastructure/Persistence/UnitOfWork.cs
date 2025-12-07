using Application.Common.Models.Responses;
using Infrastructure.Persistence.Contexts;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Persistence;

public class UnitOfWork : IUnitOfWork
{
    public UnitOfWork(ApplicationDbContext context, IServiceProvider serviceProvider)
    {
        dbContext = context;
        this.serviceProvider = serviceProvider;
    }

    private readonly ApplicationDbContext dbContext;
    private readonly IServiceProvider serviceProvider;
    private bool disposed = false;
    private IDbContextTransaction? transaction = null;

    public IIdentityRepository IdentityRepository => serviceProvider.GetRequiredService<IIdentityRepository>();

    public ICandidateRepository CandidateRepository => serviceProvider.GetRequiredService<ICandidateRepository>();

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        int result = await dbContext.SaveChangesAsync(cancellationToken);
        return result;
    }

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (transaction is not null)
        {
            throw new InvalidOperationException(Constants.ExceptionMessages.TransactionAlreadyCreated);
        }

        transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
    }

    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (transaction is not null)
        {
            await transaction.RollbackAsync(cancellationToken);
        }
    }

    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (transaction is not null)
        {
            try
            {
                await transaction.CommitAsync(cancellationToken);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
            }
        }
    }

    public void Dispose()
    {
        if (!disposed)
        {
            transaction?.Dispose();

            dbContext.Dispose();
            GC.SuppressFinalize(this);
        }

        disposed = true;
    }

    public async ValueTask DisposeAsync()
    {
        if (!disposed)
        {
            transaction?.Dispose();

            await dbContext.DisposeAsync();
            GC.SuppressFinalize(this);
        }

        disposed = true;
    }

    public IQueryable<T> GetAsQueryable<T>()
        where T : class
    {
        return dbContext.Set<T>().AsNoTracking();
    }

    public async Task<OdataResponse<T>> ODataGetAsync<T>(ODataQueryOptions<T> oDataQueryOptions, IQueryable<T> source, CancellationToken cancellationToken)
      where T : class
    {
        if (oDataQueryOptions is null)
        {
            return new OdataResponse<T>();
        }

        IQueryable<T> data = (oDataQueryOptions.Filter?.ApplyTo(source, new ODataQuerySettings()) as IQueryable<T>) ?? source;
        int count = await data.CountAsync(cancellationToken);

        List<T> list = await (oDataQueryOptions.ApplyTo(source, new ODataQuerySettings()) as IQueryable<T>)!.ToListAsync(cancellationToken: cancellationToken);

        return new OdataResponse<T> { Value = list, TotalCount = count };
    }

    ~UnitOfWork()
    {
        disposed = true;
    }
}
