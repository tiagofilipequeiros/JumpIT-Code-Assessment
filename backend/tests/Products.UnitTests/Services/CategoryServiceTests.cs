using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using Products.Application.Abstractions;
using Products.Application.Dtos.Categories;
using Products.Application.Services;
using Products.Domain.Entities;
using Products.Domain.Errors;
using Products.UnitTests.Fakes;

namespace Products.UnitTests.Services;

public class CategoryServiceTests
{
    private readonly ICategoryRepository _categories = Substitute.For<ICategoryRepository>();
    private readonly IProductRepository _products = Substitute.For<IProductRepository>();
    private readonly IUserMetricRepository _metrics = Substitute.For<IUserMetricRepository>();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly FakeTimeProvider _clock = new(TestData.Now);

    private CategoryService Service(Role? role)
    {
        _categories.GetAsync(Arg.Any<int>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(new CategorySummary(TestData.Objectives, 0));
        return new CategoryService(_categories, _products, _unitOfWork, TestData.CurrentUser(role), new MetricsService(_metrics, _clock), _clock);
    }

    [Fact]
    public async Task Delete_moves_the_products_to_uncategorized_before_removing_the_category()
    {
        var category = new Category { Id = 2, Name = "Objectives" };
        _categories.FindForUpdateAsync(2, Arg.Any<CancellationToken>()).Returns(category);
        _products.MoveToCategoryAsync(2, Category.UncategorizedId, 7, TestData.Now.UtcDateTime, Arg.Any<CancellationToken>()).Returns(3);

        await Service(Role.Admin).DeleteAsync(2, default);

        Received.InOrder(() =>
        {
            _products.MoveToCategoryAsync(2, Category.UncategorizedId, 7, TestData.Now.UtcDateTime, Arg.Any<CancellationToken>());
            _categories.Remove(category);
        });
        _metrics.Received(1).Add(Arg.Is<UserMetric>(m => m.Details == "Objectives; 3 product(s) moved to Uncategorized"));
    }

    [Fact]
    public async Task Editors_cannot_delete()
    {
        var error = await Assert.ThrowsAsync<AppException>(() => Service(Role.Editor).DeleteAsync(2, default));

        Assert.Equal(ErrorCode.Forbidden, error.Code);
    }

    [Fact]
    public async Task Uncategorized_cannot_be_deleted_renamed_or_disabled()
    {
        var service = Service(Role.Admin);
        var rename = new UpdateCategoryRequest { Name = "Other", RowVersion = [1] };

        var errors = new[]
        {
            await Assert.ThrowsAsync<AppException>(() => service.DeleteAsync(Category.UncategorizedId, default)),
            await Assert.ThrowsAsync<AppException>(() => service.UpdateAsync(Category.UncategorizedId, rename, default)),
            await Assert.ThrowsAsync<AppException>(() => service.SetActiveAsync(Category.UncategorizedId, false, default)),
        };

        Assert.All(errors, error => Assert.Equal(ErrorCode.CategoryProtected, error.Code));
    }

    [Fact]
    public async Task Create_with_an_existing_name_is_a_conflict()
    {
        _categories.NameExistsAsync("Objectives", null, Arg.Any<CancellationToken>()).Returns(true);

        var error = await Assert.ThrowsAsync<AppException>(() =>
            Service(Role.Editor).CreateAsync(new CategoryRequest { Name = " Objectives " }, default));

        Assert.Equal(ErrorCode.CategoryNameTaken, error.Code);
        _categories.DidNotReceive().Add(Arg.Any<Category>());
    }

    [Fact]
    public async Task Create_losing_a_race_on_the_unique_index_is_the_same_conflict()
    {
        // The name was free when checked, but another request saved it first.
        _unitOfWork.FailNextSaveWith = new UniqueConstraintViolationException(new Exception());

        var error = await Assert.ThrowsAsync<AppException>(() =>
            Service(Role.Editor).CreateAsync(new CategoryRequest { Name = "Filters" }, default));

        Assert.Equal(ErrorCode.CategoryNameTaken, error.Code);
    }
}
