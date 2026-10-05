namespace Products.Application.Abstractions;

public interface IUnitOfWork
{
    // Saves tracked changes. Throws AppException(ConcurrencyConflict) when the expected version no longer matches,
    // AppException(IdRangeExhausted) when the product ID sequence is used up, UniqueConstraintViolationException when a
    // unique index is violated and ForeignKeyViolationException when a referenced row no longer exists.
    Task SaveChangesAsync(CancellationToken cancellationToken);

    // Runs the work in one database transaction. On a transient database error the whole work is retried
    // from a clean state, so the work must create the entities it adds itself.
    Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> work, CancellationToken cancellationToken);
}

// A unique index rejected the change (e.g. two requests creating the same category name at the same time).
public class UniqueConstraintViolationException(Exception innerException)
    : Exception("A unique constraint was violated.", innerException);

// A foreign key rejected the change (e.g. a product saved into a category that was deleted at the same time).
public class ForeignKeyViolationException(Exception innerException)
    : Exception("A foreign key constraint was violated.", innerException);
