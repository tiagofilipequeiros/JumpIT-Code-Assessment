using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Products.Application.Abstractions;
using Products.Domain.Errors;

namespace Products.Infrastructure.Persistence;

public class UnitOfWork(AppDbContext db) : IUnitOfWork
{
    // SQL Server error numbers for "duplicate key" on a unique index / unique constraint.
    private static readonly int[] UniqueViolationErrors = [2601, 2627];

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
        catch (DbUpdateException exception) when (exception.InnerException is SqlException sql && UniqueViolationErrors.Contains(sql.Number))
        {
            throw new UniqueConstraintViolationException(exception);
        }
    }

    // With connection retries enabled, a user transaction must run inside the execution strategy,
    // so that on a transient failure the whole unit (not half of it) is retried.
    public Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> work, CancellationToken cancellationToken) =>
        db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
            var result = await work();
            await transaction.CommitAsync(cancellationToken);
            return result;
        });
}
