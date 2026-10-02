using Products.Application.Abstractions;

namespace Products.UnitTests.Fakes;

// Runs the work directly and counts saves. Can be told to fail the next save.
public class FakeUnitOfWork : IUnitOfWork
{
    public int Saves { get; private set; }
    public Exception? FailNextSaveWith { get; set; }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        if (FailNextSaveWith is { } exception)
        {
            FailNextSaveWith = null;
            throw exception;
        }

        Saves++;
        return Task.CompletedTask;
    }

    public Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> work, CancellationToken cancellationToken) => work();
}
