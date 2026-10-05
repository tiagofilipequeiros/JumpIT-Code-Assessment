using System.Net;
using Products.Application.Dtos.Products;
using Products.Domain.Entities;
using Products.IntegrationTests.Infrastructure;
using Products.TestSupport;
using Microsoft.EntityFrameworkCore;

namespace Products.IntegrationTests;

public class StockTests(SqlServerFixture sqlServer) : IntegrationTest(sqlServer)
{
    [Fact]
    public async Task Add_to_stock_increases_stock_and_records_a_metric()
    {
        var response = await AsUser.PostAsync("/api/products/100000/add-to-stock/8", null);
        var product = await response.ReadAsync<ProductResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(50, product.Stock);
        var metric = await Api.WithDbAsync(db => db.UserMetrics.SingleAsync(m => m.Action == MetricAction.AddStock));
        Assert.Equal("Stock +8 (42 → 50)", metric.Details);
    }

    [Fact]
    public async Task Decrement_stock_decreases_stock()
    {
        var product = await (await AsUser.PostAsync("/api/products/100000/decrement-stock/40", null)).ReadAsync<ProductResponse>();

        Assert.Equal(2, product.Stock);
    }

    [Fact]
    public async Task Decrement_below_zero_is_409_and_stock_is_unchanged()
    {
        var response = await AsUser.PostAsync("/api/products/100002/decrement-stock/4", null);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("InsufficientStock", await response.ErrorCodeAsync());
        var product = await (await Anonymous.GetAsync("/api/products/100002")).ReadAsync<ProductResponse>();
        Assert.Equal(3, product.Stock);
    }

    [Fact]
    public async Task Concurrent_decrements_never_go_below_zero()
    {
        // Product 100002 has stock 3; 10 users try to take 1 at the same time.
        var attempts = Enumerable.Range(0, 10)
            .Select(_ => Api.ClientFor(TestUsers.User).PostAsync("/api/products/100002/decrement-stock/1", null));
        var responses = await Task.WhenAll(attempts);

        Assert.Equal(3, responses.Count(r => r.StatusCode == HttpStatusCode.OK));
        Assert.Equal(7, responses.Count(r => r.StatusCode == HttpStatusCode.Conflict));
        var product = await (await Anonymous.GetAsync("/api/products/100002")).ReadAsync<ProductResponse>();
        Assert.Equal(0, product.Stock);
    }

    [Fact]
    public async Task Adding_beyond_the_maximum_stock_is_409()
    {
        var product = await (await AsEditor.GetAsync("/api/products/100000")).ReadAsync<ProductResponse>();
        await AsEditor.PutJsonAsync("/api/products/100000", new
        {
            name = product.Name, price = product.Price, stock = ProductLimits.StockMax - 1, categoryId = product.CategoryId, rowVersion = product.RowVersion,
        });

        var response = await AsUser.PostAsync("/api/products/100000/add-to-stock/2", null);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("StockLimitExceeded", await response.ErrorCodeAsync());
    }

    [Theory]
    [InlineData("add-to-stock", 0)]
    [InlineData("add-to-stock", -3)]
    [InlineData("decrement-stock", -3)]
    [InlineData("add-to-stock", ProductLimits.QuantityMax + 1)]
    public async Task Quantity_out_of_range_is_400_and_stock_is_unchanged(string action, int quantity)
    {
        var response = await AsUser.PostAsync($"/api/products/100000/{action}/{quantity}", null);
        var product = await (await Anonymous.GetAsync("/api/products/100000")).ReadAsync<ProductResponse>();
        Assert.Equal(42, product.Stock);

        // Same shape as model validation: the error names the field.
        var body = await response.ReadAsync<Microsoft.AspNetCore.Mvc.ValidationProblemDetails>();
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("InvalidQuantity", body.Extensions["code"]?.ToString());
        Assert.Contains("quantity", body.Errors.Keys);
    }

    [Fact]
    public async Task Stock_change_requires_a_user()
    {
        var response = await Anonymous.PostAsync("/api/products/100000/add-to-stock/1", null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Normal_user_cannot_change_stock_of_hidden_product()
    {
        var response = await AsUser.PostAsync("/api/products/100011/add-to-stock/1", null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("ProductNotFound", await response.ErrorCodeAsync());
    }

    [Fact]
    public async Task Unknown_product_is_404()
    {
        var response = await AsUser.PostAsync("/api/products/999999/decrement-stock/1", null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
