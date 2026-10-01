using Testcontainers.MsSql;

[assembly: AssemblyFixture(typeof(Backend.Tests.Infrastructure.SqlServerFixture))]

namespace Backend.Tests.Infrastructure;

// One real SQL Server container for the whole test run (needs Docker).
// Real SQL Server, not an in-memory fake: sequences, temporal tables and atomic updates are what we test.
public class SqlServerFixture : IAsyncLifetime
{
    private readonly MsSqlContainer _container = new MsSqlBuilder()
        .WithImage("mcr.microsoft.com/mssql/server:2025-latest")
        .Build();

    public string ConnectionString(string database) =>
        new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(_container.GetConnectionString())
        {
            InitialCatalog = database,
        }.ConnectionString;

    public ValueTask InitializeAsync() => new(_container.StartAsync());

    public ValueTask DisposeAsync() => _container.DisposeAsync();
}
