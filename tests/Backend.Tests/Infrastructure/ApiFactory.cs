using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Backend.Auth;
using Backend.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Backend.Tests.Infrastructure;

// Starts the real API against its own fresh database (migrations + seed data).
public class ApiFactory(SqlServerFixture sqlServer) : WebApplicationFactory<Program>
{
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly string _database = $"Tests_{Guid.NewGuid():N}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:Default", sqlServer.ConnectionString(_database));
    }

    public HttpClient ClientFor(int? userId)
    {
        var client = CreateClient();
        if (userId is not null)
        {
            client.DefaultRequestHeaders.Add(CurrentUser.HeaderName, userId.ToString());
        }

        return client;
    }

    public async Task<T> WithDbAsync<T>(Func<AppDbContext, Task<T>> action)
    {
        using var scope = Services.CreateScope();
        return await action(scope.ServiceProvider.GetRequiredService<AppDbContext>());
    }
}

public static class Users
{
    public const int Admin = 1;
    public const int Editor = 2;
    public const int User = 3;
}

public static class HttpExtensions
{
    public static async Task<T> ReadAsync<T>(this HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<T>(ApiFactory.Json))!;

    public static async Task<string?> ErrorCodeAsync(this HttpResponseMessage response)
    {
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return body.TryGetProperty("code", out var code) ? code.GetString() : null;
    }

    public static Task<HttpResponseMessage> PostJsonAsync<T>(this HttpClient client, string url, T body) =>
        client.PostAsJsonAsync(url, body, ApiFactory.Json);

    public static Task<HttpResponseMessage> PutJsonAsync<T>(this HttpClient client, string url, T body) =>
        client.PutAsJsonAsync(url, body, ApiFactory.Json);
}

// Base class: xUnit creates a new instance per test, so every test gets its own API and database
// (fresh seed data, no shared state, tests can run in any order).
public abstract class IntegrationTest(SqlServerFixture sqlServer) : IAsyncLifetime
{
    protected ApiFactory Api { get; } = new(sqlServer);

    protected HttpClient Anonymous => Api.ClientFor(null);
    protected HttpClient AsAdmin => Api.ClientFor(Users.Admin);
    protected HttpClient AsEditor => Api.ClientFor(Users.Editor);
    protected HttpClient AsUser => Api.ClientFor(Users.User);

    public ValueTask InitializeAsync()
    {
        // Creating the client starts the API, which applies the migrations.
        Api.CreateClient();
        return ValueTask.CompletedTask;
    }

    public ValueTask DisposeAsync() => Api.DisposeAsync();
}
