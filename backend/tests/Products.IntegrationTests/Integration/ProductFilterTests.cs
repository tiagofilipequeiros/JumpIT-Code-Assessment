using System.Net;
using Products.Application.Dtos.Products;
using Products.Domain.Entities;
using Products.IntegrationTests.Infrastructure;
using Products.TestSupport;

namespace Products.IntegrationTests;

// GET /api/products with filters: all filters combine with AND, values within one filter with OR.
public class ProductFilterTests(SqlServerFixture sqlServer) : IntegrationTest(sqlServer)
{
    private async Task<int[]> IdsAsync(HttpClient client, string query) =>
        (await (await client.GetAsync($"/api/products?{query}")).ReadAsync<List<ProductResponse>>()).Select(p => p.Id).ToArray();

    [Fact]
    public async Task Search_and_stock_range_combine_instead_of_replacing_each_other()
    {
        // "Objective" matches 100000 (42), 100001 (18) and 100002 (3); only two have 3 to 20 in stock.
        var ids = await IdsAsync(Anonymous, "search=objective&minStock=3&maxStock=20");

        Assert.Equal([100001, 100002], ids);
    }

    [Fact]
    public async Task Several_categories_match_any_of_them()
    {
        var ids = await IdsAsync(Anonymous, "categoryIds=3&categoryIds=6");

        Assert.Equal([100003, 100008, 100009], ids);
    }

    [Fact]
    public async Task Price_range_is_inclusive()
    {
        var ids = await IdsAsync(Anonymous, "minPrice=9.90&maxPrice=19.90");

        Assert.Equal([100005, 100006, 100009], ids);
    }

    [Fact]
    public async Task Stock_statuses_match_any_of_them()
    {
        // LED Illuminator is out of stock (0), the 100x Oil objective is low (3).
        var ids = await IdsAsync(Anonymous, "stockStatuses=LowStock&stockStatuses=OutOfStock");

        Assert.Equal([100002, 100004], ids);
    }

    [Fact]
    public async Task Editors_can_filter_on_hidden_statuses()
    {
        var ids = await IdsAsync(AsEditor, "statuses=Disabled&statuses=CategoryDisabled");

        Assert.Equal([100011, 100012], ids);
    }

    [Fact]
    public async Task Every_filter_together()
    {
        var ids = await IdsAsync(AsEditor,
            "search=o&categoryIds=2&categoryIds=5&statuses=Active&stockStatuses=InStock&minStock=10&maxPrice=400");

        // Objectives 10x (42, 249.90) and 40x (18, 389.00); Slides (500, 12.90) and Cover glasses (350, 9.90). Oil is low stock.
        Assert.Equal([100000, 100001, 100005, 100006], ids);
    }

    [Fact]
    public async Task Min_price_above_max_price_is_400()
    {
        var response = await Anonymous.GetAsync("/api/products?minPrice=50&maxPrice=10");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("InvalidPriceRange", await response.ErrorCodeAsync());
    }

    [Fact]
    public async Task Unknown_status_value_is_a_validation_error()
    {
        var response = await Anonymous.GetAsync("/api/products?statuses=Gone");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("ValidationFailed", await response.ErrorCodeAsync());
    }
}
