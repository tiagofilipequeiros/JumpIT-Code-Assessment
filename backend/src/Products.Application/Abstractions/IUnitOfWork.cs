namespace Products.Application.Abstractions;

public interface IUnitOfWork
{
    // Saves tracked changes. Throws AppException(ConcurrencyConflict) when the expected version no longer matches,
    // and UniqueConstraintViolationException when a unique index is violated.
    Task SaveChangesAsync(CancellationToken cancellationToken);

    // Runs the work in one database transaction (retried as a whole on transient database errors).
    Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> work, CancellationToken cancellationToken);
}

// A unique index rejected the change (e.g. two requests creating the same category name at the same time).
public class UniqueConstraintViolationException(Exception innerException)
    : Exception("A unique constraint was violated.", innerException);
