using System.Net;
using Products.Application.Dtos.Products;
using Products.Application.Dtos.Categories;
using Products.Application.Dtos.Users;
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
        var products = await (await AsUser.GetAsync("/api/products?includeHidden=true")).ReadAsync<List<ProductResponse>>();

        Assert.Equal(10, products.Count);
    }

    [Fact]
    public async Task Editor_can_include_hidden_products()
    {
        var products = await (await AsEditor.GetAsync("/api/products?includeHidden=true")).ReadAsync<List<ProductResponse>>();

        Assert.Equal(13, products.Count);
        Assert.All(HiddenSeedIds, id => Assert.Contains(products, p => p.Id == id));
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
    public async Task Stock_level_with_only_max_returns_low_stock()
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
