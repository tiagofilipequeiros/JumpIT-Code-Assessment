using System.Net;
using Products.Application.Dtos.Products;
using Products.Domain.Entities;
using Products.IntegrationTests.Infrastructure;
using Products.TestSupport;

namespace Products.IntegrationTests;

public class ProductReadTests(SqlServerFixture sqlServer) : IntegrationTest(sqlServer)
{
    // Seed data: 13 products; 100010 is uncategorized, 100011 is disabled, 100012 is in a disabled category.
    private static readonly int[] HiddenSeedIds = [100010, 100011, 100012];

    [Fact]
    public async Task Get_all_returns_visible_products_with_stock()
    {
        var products = await (await Anonymous.GetAsync("/api/products")).ReadAsync<List<ProductResponse>>();

        Assert.Equal(10, products.Count);
        Assert.DoesNotContain(products, p => HiddenSeedIds.Contains(p.Id));
        Assert.Equal(42, products.Single(p => p.Id == 100000).Stock);
    }

    [Fact]
    public async Task Normal_user_cannot_see_hidden_products_even_when_asking()
    {
        var products = await (await AsUser.GetAsync("/api/products?statuses=Disabled&statuses=Uncategorized"))
            .ReadAsync<List<ProductResponse>>();

        Assert.Equal(10, products.Count);
        Assert.All(products, p => Assert.Equal(ProductStatus.Active, p.Status));
    }

    [Fact]
    public async Task Editors_see_every_product_with_its_status()
    {
        var products = await (await AsEditor.GetAsync("/api/products")).ReadAsync<List<ProductResponse>>();

        Assert.Equal(13, products.Count);
        Assert.Equal(ProductStatus.Uncategorized, products.Single(p => p.Id == 100010).Status);
        Assert.Equal(ProductStatus.Disabled, products.Single(p => p.Id == 100011).Status);
        Assert.Equal(ProductStatus.CategoryDisabled, products.Single(p => p.Id == 100012).Status);
        Assert.Equal(StockStatus.OutOfStock, products.Single(p => p.Id == 100004).StockStatus);
        Assert.Equal(StockStatus.LowStock, products.Single(p => p.Id == 100002).StockStatus);
    }

    [Fact]
    public async Task Get_by_id_returns_the_product()
    {
        var product = await (await Anonymous.GetAsync("/api/products/100002")).ReadAsync<ProductResponse>();

        Assert.Equal("Microscope Objective 100x Oil", product.Name);
        Assert.Equal("Objectives", product.CategoryName);
        Assert.Equal(3, product.Stock);
    }

    [Fact]
    public async Task Get_by_id_returns_404_with_error_code_for_unknown_product()
    {
        var response = await Anonymous.GetAsync("/api/products/999999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("ProductNotFound", await response.ErrorCodeAsync());
    }

    [Fact]
    public async Task Hidden_product_is_not_found_for_normal_user_but_visible_to_editor()
    {
        Assert.Equal(HttpStatusCode.NotFound, (await AsUser.GetAsync("/api/products/100011")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await AsEditor.GetAsync("/api/products/100011")).StatusCode);
    }

    [Fact]
    public async Task Search_matches_partial_names_case_insensitively()
    {
        var products = await (await Anonymous.GetAsync("/api/products/search?name=oBjEcTiVe")).ReadAsync<List<ProductResponse>>();

        Assert.Equal([100000, 100001, 100002], products.Select(p => p.Id));
    }

    [Fact]
    public async Task Health_reports_the_database()
    {
        var response = await Anonymous.GetAsync("/api/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("""{"status":"ok","database":"ok"}""", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Search_without_name_is_rejected()
    {
        var response = await Anonymous.GetAsync("/api/products/search?name=%20");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("ValidationFailed", await response.ErrorCodeAsync());
    }

    [Fact]
    public async Task Stock_level_returns_products_within_range_inclusive()
    {
        var products = await (await Anonymous.GetAsync("/api/products/stock-level?min=3&max=18")).ReadAsync<List<ProductResponse>>();

        Assert.Equal([100001, 100002, 100007, 100008], products.Select(p => p.Id));
        Assert.All(products, p => Assert.InRange(p.Stock, 3, 18));
    }

    [Fact]
    public async Task Stock_level_with_only_max_returns_products_up_to_it()
    {
        var products = await (await Anonymous.GetAsync("/api/products/stock-level?max=0")).ReadAsync<List<ProductResponse>>();

        Assert.Equal([100004], products.Select(p => p.Id));
    }

    [Theory]
    [InlineData("min=10&max=5")]
    [InlineData("min=-1")]
    public async Task Stock_level_rejects_invalid_ranges(string query)
    {
        var response = await Anonymous.GetAsync($"/api/products/stock-level?{query}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("InvalidStockRange", await response.ErrorCodeAsync());
    }
}
