using System.Net;
using Products.Application.Dtos.Metrics;
using Products.IntegrationTests.Infrastructure;
using Products.TestSupport;

namespace Products.IntegrationTests;

public class MetricsTests(SqlServerFixture sqlServer) : IntegrationTest(sqlServer)
{
    [Fact]
    public async Task Stock_changes_show_up_in_the_product_metrics_with_their_quantity()
    {
        await AsUser.PostAsync("/api/products/100000/decrement-stock/5", null);
        await AsUser.PostAsync("/api/products/100000/decrement-stock/3", null);
        await AsEditor.PostAsync("/api/products/100002/add-to-stock/10", null);

        var metrics = await (await AsEditor.GetAsync("/api/metrics/products?days=7")).ReadAsync<ProductMetricsResponse>();

        Assert.Equal(10, metrics.Kpis.UnitsAdded);
        Assert.Equal(8, metrics.Kpis.UnitsRemoved);
        Assert.Equal(7, metrics.MovementsPerDay.Count);
        // Summed over the period, so the test also passes when it runs across midnight (UTC).
        Assert.Equal((10, 8), (metrics.MovementsPerDay.Sum(d => d.Added), metrics.MovementsPerDay.Sum(d => d.Removed)));
        Assert.Equal(new ProductUnits(100000, "Microscope Objective 10x", 8), metrics.TopRemoved.Single());
    }

    [Fact]
    public async Task Product_kpis_reflect_the_current_active_products()
    {
        var metrics = await (await AsAdmin.GetAsync("/api/metrics/products")).ReadAsync<ProductMetricsResponse>();

        // Seed data: LED Illuminator has 0 (out of stock), Objective 100x Oil has 3 (low stock).
        Assert.Equal(1, metrics.Kpis.OutOfStock);
        Assert.Equal(1, metrics.Kpis.LowStock);
        Assert.True(metrics.Kpis.InventoryValue > 0);
    }

    [Fact]
    public async Task Stock_history_steps_through_every_stock_change()
    {
        await AsUser.PostAsync("/api/products/100000/decrement-stock/2", null);
        await AsUser.PostAsync("/api/products/100000/decrement-stock/10", null);

        var history = await (await AsEditor.GetAsync("/api/metrics/products/stock-history?productIds=100000&days=7"))
            .ReadAsync<List<ProductStockHistoryResponse>>();

        Assert.Equal([42, 40, 30, 30], history.Single().Points.Select(p => p.Stock));
    }

    [Fact]
    public async Task Logins_and_actions_show_up_in_the_user_metrics()
    {
        await Anonymous.PostJsonAsync("/api/auth/login", new { email = "sam@example.com" });
        await AsUser.PostAsync("/api/products/100000/decrement-stock/1", null);

        var metrics = await (await AsAdmin.GetAsync("/api/metrics/users?days=7")).ReadAsync<UserMetricsResponse>();

        Assert.Equal(new UserKpis(1, 1, 0, 1), metrics.Kpis);
        var sam = metrics.PerUser.Single(u => u.UserId == TestUsers.User);
        Assert.Equal((1, 0, 1), (sam.Logins, sam.Edits, sam.StockChanges));
        Assert.Equal(4, metrics.PerUser.Count);
        Assert.Equal(2, metrics.PerHour.Sum(h => h.Count));
    }

    [Theory]
    [InlineData("/api/metrics/products", TestUsers.User, HttpStatusCode.Forbidden)]
    [InlineData("/api/metrics/users", TestUsers.User, HttpStatusCode.Forbidden)]
    [InlineData("/api/metrics/users", TestUsers.Editor, HttpStatusCode.Forbidden)]
    [InlineData("/api/metrics/products", TestUsers.Editor, HttpStatusCode.OK)]
    [InlineData("/api/metrics/users", TestUsers.Admin, HttpStatusCode.OK)]
    public async Task Metrics_are_limited_by_role(string url, int userId, HttpStatusCode expected)
    {
        var response = await Api.ClientFor(userId).GetAsync(url);

        Assert.Equal(expected, response.StatusCode);
    }

    [Fact]
    public async Task Invalid_time_range_is_400()
    {
        var response = await AsAdmin.GetAsync("/api/metrics/products?days=0");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("InvalidTimeRange", await response.ErrorCodeAsync());
    }
}
