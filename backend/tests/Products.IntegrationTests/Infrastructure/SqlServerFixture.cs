using Products.TestSupport;

[assembly: AssemblyFixture(typeof(Products.IntegrationTests.Infrastructure.SqlServerFixture))]

namespace Products.IntegrationTests.Infrastructure;

// One SQL Server container for the whole test run (needs Docker).
public sealed class SqlServerFixture : IAsyncLifetime
{
    public SqlServerContainer Server { get; } = new();

    public ValueTask InitializeAsync() => new(Server.StartAsync());

    public ValueTask DisposeAsync() => Server.DisposeAsync();
}

// Base class: xUnit creates a new instance per test, so every test gets its own API and database
// (fresh seed data, no shared state, tests can run in any order).
public abstract class IntegrationTest(SqlServerFixture sqlServer) : IAsyncLifetime
{
    protected ApiFactory Api { get; } = new(sqlServer.Server.NewDatabase());

    protected HttpClient Anonymous => Api.ClientFor(null);
    protected HttpClient AsAdmin => Api.ClientFor(TestUsers.Admin);
    protected HttpClient AsEditor => Api.ClientFor(TestUsers.Editor);
    protected HttpClient AsUser => Api.ClientFor(TestUsers.User);

    public ValueTask InitializeAsync()
    {
        // Creating the client starts the API, which applies the migrations.
        Api.CreateClient();
        return ValueTask.CompletedTask;
    }

    public ValueTask DisposeAsync() => Api.DisposeAsync();
}
