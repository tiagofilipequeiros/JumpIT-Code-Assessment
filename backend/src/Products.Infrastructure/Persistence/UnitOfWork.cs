using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Products.Application.Abstractions;
using Products.Domain.Errors;

namespace Products.Infrastructure.Persistence;

public class UnitOfWork(AppDbContext db) : IUnitOfWork
{
    // SQL Server error numbers.
    private static readonly int[] UniqueViolationErrors = [2601, 2627];
    private const int ForeignKeyViolationError = 547;
    private const int SequenceExhaustedError = 11728;

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new AppException(ErrorCode.ConcurrencyConflict);
        }
        catch (DbUpdateException exception) when (exception.InnerException is SqlException sql)
        {
            if (UniqueViolationErrors.Contains(sql.Number))
            {
                throw new UniqueConstraintViolationException(exception);
            }

            if (sql.Number == ForeignKeyViolationError)
            {
                throw new ForeignKeyViolationException(exception);
            }

            if (sql.Number == SequenceExhaustedError)
            {
                throw new AppException(ErrorCode.IdRangeExhausted);
            }

            throw;
        }
    }

    // With connection retries enabled, a user transaction must run inside the execution strategy,
    // so that on a transient failure the whole unit (not half of it) is retried. A retry starts from a clean
    // change tracker: entities added by the failed attempt are dropped and the work adds them again.
    public Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> work, CancellationToken cancellationToken)
    {
        var attempt = 0;
        return db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            if (attempt++ > 0)
            {
                db.ChangeTracker.Clear();
            }

            await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
            var result = await work();
            await transaction.CommitAsync(cancellationToken);
            return result;
        });
    }
}
