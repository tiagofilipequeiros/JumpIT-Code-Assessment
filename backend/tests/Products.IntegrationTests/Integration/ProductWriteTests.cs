using System.Net;
using Products.Application.Dtos.Products;
using Products.Domain.Entities;
using Products.IntegrationTests.Infrastructure;
using Products.TestSupport;
using Microsoft.EntityFrameworkCore;

namespace Products.IntegrationTests;

public class ProductWriteTests(SqlServerFixture sqlServer) : IntegrationTest(sqlServer)
{
    private static object NewProduct(string name = "New Lens") =>
        new { name, description = "Test product", price = 12.50m, stock = 4, categoryId = 2 };

    [Fact]
    public async Task Editor_creates_product_with_a_6_digit_id_after_the_seed_range()
    {
        var response = await AsEditor.PostJsonAsync("/api/products", NewProduct());
        var product = await response.ReadAsync<ProductResponse>();

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal($"/api/products/{product.Id}", response.Headers.Location?.AbsolutePath);
        Assert.InRange(product.Id, ProductLimits.SequenceStart, ProductLimits.IdMax);
        Assert.Equal("Erin Editor", product.UpdatedByName);
    }

    [Fact]
    public async Task Concurrent_creates_never_get_duplicate_ids()
    {
        var creates = Enumerable.Range(0, 25)
            .Select(i => Api.ClientFor(TestUsers.Editor).PostJsonAsync("/api/products", NewProduct($"Parallel {i}")));
        var responses = await Task.WhenAll(creates);

        var ids = await Task.WhenAll(responses.Select(async r => (await r.ReadAsync<ProductResponse>()).Id));
        Assert.All(responses, r => Assert.Equal(HttpStatusCode.Created, r.StatusCode));
        Assert.Equal(ids.Length, ids.Distinct().Count());
    }

    [Fact]
    public async Task Create_without_user_is_401()
    {
        var response = await Anonymous.PostJsonAsync("/api/products", NewProduct());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("UserRequired", await response.ErrorCodeAsync());
    }

    [Fact]
    public async Task Create_as_normal_user_is_403()
    {
        var response = await AsUser.PostJsonAsync("/api/products", NewProduct());

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal("Forbidden", await response.ErrorCodeAsync());
    }

    [Fact]
    public async Task Create_with_invalid_fields_returns_validation_errors_per_field()
    {
        var response = await AsEditor.PostJsonAsync("/api/products", new { name = "X", price = -1, stock = 1 });
        var body = await response.ReadAsync<Microsoft.AspNetCore.Mvc.ValidationProblemDetails>();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("ValidationFailed", body.Extensions["code"]?.ToString());
        Assert.Contains("name", body.Errors.Keys);
        Assert.Contains("price", body.Errors.Keys);
        Assert.Contains("categoryId", body.Errors.Keys);
    }

    [Fact]
    public async Task Names_are_trimmed_before_the_length_rule_applies()
    {
        var response = await AsEditor.PostJsonAsync("/api/products", new { name = "  a  ", price = 1m, stock = 1, categoryId = 2 });
        var body = await response.ReadAsync<Microsoft.AspNetCore.Mvc.ValidationProblemDetails>();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("name", body.Errors.Keys);
    }

    [Fact]
    public async Task Update_without_row_version_is_a_validation_error_not_a_conflict()
    {
        var response = await AsEditor.PutJsonAsync("/api/products/100000", new { name = "Lens", price = 10m, stock = 1, categoryId = 2 });
        var body = await response.ReadAsync<Microsoft.AspNetCore.Mvc.ValidationProblemDetails>();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("rowVersion", body.Errors.Keys);
    }

    [Fact]
    public async Task Update_of_unknown_product_is_404()
    {
        var response = await AsEditor.PutJsonAsync("/api/products/999999", new
        {
            name = "Lens", price = 10m, stock = 1, categoryId = 2, rowVersion = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 },
        });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("ProductNotFound", await response.ErrorCodeAsync());
    }

    [Fact]
    public async Task Delete_of_unknown_product_is_404()
    {
        var response = await AsAdmin.DeleteAsync("/api/products/999999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Running_out_of_ids_is_a_clear_conflict_not_a_server_error()
    {
        await Api.WithDbAsync(db => db.Database.ExecuteSqlRawAsync("ALTER SEQUENCE ProductIds RESTART WITH 999999"));

        var last = await AsEditor.PostJsonAsync("/api/products", NewProduct("Last one"));
        var tooMany = await AsEditor.PostJsonAsync("/api/products", NewProduct("One too many"));

        Assert.Equal(999999, (await last.ReadAsync<ProductResponse>()).Id);
        Assert.Equal(HttpStatusCode.Conflict, tooMany.StatusCode);
        Assert.Equal("IdRangeExhausted", await tooMany.ErrorCodeAsync());
    }

    [Fact]
    public async Task Create_in_uncategorized_is_rejected()
    {
        var response = await AsEditor.PostJsonAsync("/api/products",
            new { name = "Lens", price = 1m, stock = 1, categoryId = Category.UncategorizedId });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("InvalidCategory", await response.ErrorCodeAsync());
    }

    [Fact]
    public async Task Update_changes_the_product_and_records_a_metric()
    {
        var product = await (await AsEditor.GetAsync("/api/products/100000")).ReadAsync<ProductResponse>();

        var response = await AsEditor.PutJsonAsync("/api/products/100000", new
        {
            name = product.Name,
            description = product.Description,
            price = 299.00m,
            stock = product.Stock,
            categoryId = product.CategoryId,
            rowVersion = product.RowVersion,
        });
        var updated = await response.ReadAsync<ProductResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(299.00m, updated.Price);
        var metric = await Api.WithDbAsync(db => db.UserMetrics.SingleAsync(m => m.Action == MetricAction.Update));
        Assert.Equal(TestUsers.Editor, metric.UserId);
        Assert.Equal("Price 249.90 → 299.00", metric.Details);
    }

    [Fact]
    public async Task Update_with_an_old_row_version_is_409_and_keeps_the_newer_change()
    {
        var product = await (await AsEditor.GetAsync("/api/products/100000")).ReadAsync<ProductResponse>();
        object Edit(decimal price) => new
        {
            name = product.Name, price, stock = product.Stock, categoryId = product.CategoryId, rowVersion = product.RowVersion,
        };

        var first = await AsEditor.PutJsonAsync("/api/products/100000", Edit(300m));
        var second = await AsAdmin.PutJsonAsync("/api/products/100000", Edit(400m));

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
        Assert.Equal("ConcurrencyConflict", await second.ErrorCodeAsync());
        var current = await (await Anonymous.GetAsync("/api/products/100000")).ReadAsync<ProductResponse>();
        Assert.Equal(300m, current.Price);
    }

    [Fact]
    public async Task Only_admin_can_delete()
    {
        var asEditor = await AsEditor.DeleteAsync("/api/products/100001");
        var asAdmin = await AsAdmin.DeleteAsync("/api/products/100001");

        Assert.Equal(HttpStatusCode.Forbidden, asEditor.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, asAdmin.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await AsAdmin.GetAsync("/api/products/100001")).StatusCode);
    }

    [Fact]
    public async Task Delete_keeps_the_metric_history()
    {
        await AsAdmin.DeleteAsync("/api/products/100001");

        var metric = await Api.WithDbAsync(db => db.UserMetrics.SingleAsync(m => m.Action == MetricAction.Delete));
        Assert.Equal(100001, metric.EntityId);
        Assert.Equal(MetricEntity.Product, metric.Entity);
    }

    [Fact]
    public async Task Disabled_product_is_hidden_from_normal_users_until_enabled()
    {
        await AsEditor.PostAsync("/api/products/100000/disable", null);
        Assert.Equal(HttpStatusCode.NotFound, (await AsUser.GetAsync("/api/products/100000")).StatusCode);

        await AsEditor.PostAsync("/api/products/100000/enable", null);
        Assert.Equal(HttpStatusCode.OK, (await AsUser.GetAsync("/api/products/100000")).StatusCode);
    }

    [Fact]
    public async Task Normal_user_cannot_disable()
    {
        var response = await AsUser.PostAsync("/api/products/100000/disable", null);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task History_contains_every_version()
    {
        var product = await (await AsEditor.GetAsync("/api/products/100000")).ReadAsync<ProductResponse>();
        await AsEditor.PutJsonAsync("/api/products/100000", new
        {
            name = product.Name, price = 260m, stock = product.Stock, categoryId = product.CategoryId, rowVersion = product.RowVersion,
        });
        await AsUser.PostAsync("/api/products/100000/decrement-stock/2", null);

        var history = await (await Anonymous.GetAsync("/api/products/100000/history")).ReadAsync<List<ProductHistoryResponse>>();

        Assert.Equal([249.90m, 260m, 260m], history.Select(h => h.Price));
        Assert.Equal([42, 42, 40], history.Select(h => h.Stock));
        Assert.Null(history[^1].ValidTo);
        Assert.Equal("Sam User", history[^1].UpdatedByName);
    }
}
