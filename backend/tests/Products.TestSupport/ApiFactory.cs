using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Products.Api.Auth;
using Products.Infrastructure.Persistence;

namespace Products.TestSupport;

// Starts the real API against the given database (migrations + seed data are applied on startup).
// Extra settings override appsettings.json, e.g. { ["Seeding:DemoActivity"] = "true" }.
public class ApiFactory(string connectionString, IReadOnlyDictionary<string, string>? settings = null) : WebApplicationFactory<Program>
{
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:Default", connectionString);
        foreach (var (key, value) in settings ?? new Dictionary<string, string>())
        {
            builder.UseSetting(key, value);
        }
    }

    public HttpClient ClientFor(int? userId)
    {
        var client = CreateClient();
        if (userId is not null)
        {
            client.DefaultRequestHeaders.Add(HeaderCurrentUserAccessor.HeaderName, userId.ToString());
        }

        return client;
    }

    public async Task<T> WithDbAsync<T>(Func<AppDbContext, Task<T>> action)
    {
        using var scope = Services.CreateScope();
        return await action(scope.ServiceProvider.GetRequiredService<AppDbContext>());
    }
}

// The seeded users.
public static class TestUsers
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
