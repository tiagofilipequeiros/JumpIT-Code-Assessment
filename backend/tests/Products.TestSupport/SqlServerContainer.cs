using Microsoft.Data.SqlClient;
using Testcontainers.MsSql;

namespace Products.TestSupport;

// A real SQL Server in Docker, not an in-memory fake: sequences, temporal tables and atomic updates are what we test.
public sealed class SqlServerContainer : IAsyncDisposable
{
    private readonly MsSqlContainer _container = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2025-latest").Build();

    public Task StartAsync() => _container.StartAsync();

    // Each caller gets its own empty database on the shared server.
    public string NewDatabase() =>
        new SqlConnectionStringBuilder(_container.GetConnectionString())
        {
            InitialCatalog = $"Tests_{Guid.NewGuid():N}",
        }.ConnectionString;

    public ValueTask DisposeAsync() => _container.DisposeAsync();
}
