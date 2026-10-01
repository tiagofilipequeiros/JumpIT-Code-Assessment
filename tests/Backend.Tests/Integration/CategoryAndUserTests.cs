using System.Net;
using Backend.Dtos;
using Backend.Models;
using Backend.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Backend.Tests.Integration;

public class CategoryAndUserTests(SqlServerFixture sqlServer) : IntegrationTest(sqlServer)
{
    private const int Objectives = 2;

    [Fact]
    public async Task Normal_users_do_not_see_uncategorized_or_disabled_categories()
    {
        var categories = await (await AsUser.GetAsync("/api/categories?includeHidden=true")).ReadAsync<List<CategoryResponse>>();

        Assert.DoesNotContain(categories, c => c.Name is "Uncategorized" or "Legacy");
    }

    [Fact]
    public async Task Deleting_a_category_moves_its_products_to_uncategorized()
    {
        var response = await AsAdmin.DeleteAsync($"/api/categories/{Objectives}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var product = await (await AsAdmin.GetAsync("/api/products/100000")).ReadAsync<ProductResponse>();
        Assert.Equal(Category.UncategorizedId, product.CategoryId);
        Assert.Equal(HttpStatusCode.NotFound, (await AsUser.GetAsync("/api/products/100000")).StatusCode);
    }

    [Fact]
    public async Task Uncategorized_is_protected()
    {
        var delete = await AsAdmin.DeleteAsync($"/api/categories/{Category.UncategorizedId}");
        var disable = await AsAdmin.PostAsync($"/api/categories/{Category.UncategorizedId}/disable", null);

        Assert.Equal("CategoryProtected", await delete.ErrorCodeAsync());
        Assert.Equal("CategoryProtected", await disable.ErrorCodeAsync());
    }

    [Fact]
    public async Task Category_names_are_unique_ignoring_case()
    {
        var response = await AsEditor.PostJsonAsync("/api/categories", new { name = "objectives" });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("CategoryNameTaken", await response.ErrorCodeAsync());
    }

    [Fact]
    public async Task Disabling_a_category_hides_its_products_and_enabling_restores_them()
    {
        await AsEditor.PostAsync($"/api/categories/{Objectives}/disable", null);
        var whileDisabled = await (await AsUser.GetAsync("/api/products")).ReadAsync<List<ProductResponse>>();

        await AsEditor.PostAsync($"/api/categories/{Objectives}/enable", null);
        var afterEnable = await (await AsUser.GetAsync("/api/products")).ReadAsync<List<ProductResponse>>();

        Assert.DoesNotContain(whileDisabled, p => p.CategoryId == Objectives);
        Assert.Equal(3, afterEnable.Count(p => p.CategoryId == Objectives));
    }

    [Fact]
    public async Task Editor_cannot_delete_a_category()
    {
        var response = await AsEditor.DeleteAsync($"/api/categories/{Objectives}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Login_returns_the_user_with_permissions_and_records_a_metric()
    {
        var response = await Anonymous.PostJsonAsync("/api/auth/login", new { email = "editor@example.com" });
        var user = await response.ReadAsync<UserResponse>();

        Assert.Equal(Role.Editor, user.Role);
        Assert.Contains(Backend.Auth.Permission.Edit, user.Permissions);
        var metric = await Api.WithDbAsync(db => db.UserMetrics.SingleAsync());
        Assert.Equal((MetricEntity.User, MetricAction.Login, Users.Editor), (metric.Entity, metric.Action, metric.UserId));
    }

    [Fact]
    public async Task Login_with_unknown_email_is_404()
    {
        var response = await Anonymous.PostJsonAsync("/api/auth/login", new { email = "nobody@example.com" });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("UserNotFound", await response.ErrorCodeAsync());
    }

    [Fact]
    public async Task Unknown_routes_return_problem_details_with_a_code()
    {
        var response = await Anonymous.GetAsync("/api/does-not-exist");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("NotFound", await response.ErrorCodeAsync());
    }
}
