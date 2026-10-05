using Microsoft.EntityFrameworkCore;
using Products.Application.Dtos.Metrics;
using Products.Application.Dtos.Products;
using Products.IntegrationTests.Infrastructure;
using Products.TestSupport;

namespace Products.IntegrationTests;

// The optional demo activity (Seeding:DemoActivity) that fills the Metrics tab on a fresh install.
public sealed class DemoActivitySeedingTests(SqlServerFixture sqlServer) : IAsyncDisposable
{
    private static readonly Dictionary<string, string> SeedingOn = new() { ["Seeding:DemoActivity"] = "true" };

    private readonly string _database = sqlServer.Server.NewDatabase();
    private readonly List<ApiFactory> _apis = [];

    private ApiFactory StartApi()
    {
        var api = new ApiFactory(_database, SeedingOn);
        api.CreateClient();
        _apis.Add(api);
        return api;
    }

    [Fact]
    public async Task Seeding_creates_90_days_of_activity_for_the_metrics()
    {
        var admin = StartApi().ClientFor(TestUsers.Admin);

        var products = await (await admin.GetAsync("/api/metrics/products?days=90")).ReadAsync<ProductMetricsResponse>();
        var users = await (await admin.GetAsync("/api/metrics/users?days=90")).ReadAsync<UserMetricsResponse>();

        Assert.True(products.MovementsPerDay.Count(d => d.Removed > 0) > 50, "Stock should move on most days.");
        Assert.NotEmpty(products.TopRemoved);
        Assert.All(users.PerUser, user => Assert.True(user.Logins > 0, $"{user.Name} never logged in."));
        Assert.True(users.Kpis.Edits > 0);
    }

    [Fact]
    public async Task Seeded_stock_history_is_never_negative_and_ends_at_the_current_stock()
    {
        var admin = StartApi().ClientFor(TestUsers.Admin);
        var current = await (await admin.GetAsync("/api/products")).ReadAsync<List<ProductResponse>>();

        var history = await (await admin.GetAsync(
                "/api/metrics/products/stock-history?days=90&productIds=100000&productIds=100004&productIds=100005"))
            .ReadAsync<List<ProductStockHistoryResponse>>();

        Assert.Equal(3, history.Count);
        Assert.All(history, product =>
        {
            Assert.True(product.Points.Count > 2, $"{product.Name} has no movements.");
            Assert.All(product.Points, point => Assert.InRange(point.Stock, 0, int.MaxValue));
            Assert.Equal(current.Single(p => p.Id == product.ProductId).Stock, product.Points[^1].Stock);
        });
    }

    [Fact]
    public async Task Seeding_runs_only_once()
    {
        var first = StartApi();
        var count = await first.WithDbAsync(db => db.UserMetrics.CountAsync());

        var second = StartApi();

        Assert.Equal(count, await second.WithDbAsync(db => db.UserMetrics.CountAsync()));
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var api in _apis)
        {
            await api.DisposeAsync();
        }
    }
}
