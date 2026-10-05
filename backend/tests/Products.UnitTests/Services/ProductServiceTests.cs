using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using Products.Application.Abstractions;
using Products.Application.Dtos.Products;
using Products.Application.Services;
using Products.Domain.Entities;
using Products.Domain.Errors;
using Products.UnitTests.Fakes;

namespace Products.UnitTests.Services;

public class ProductServiceTests
{
    private readonly IProductRepository _products = Substitute.For<IProductRepository>();
    private readonly ICategoryRepository _categories = Substitute.For<ICategoryRepository>();
    private readonly IUserMetricRepository _metrics = Substitute.For<IUserMetricRepository>();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly FakeTimeProvider _clock = new(TestData.Now);

    private ProductService Service(Role? role)
    {
        // Reads after a change return whatever product the test set up.
        _products.GetAsync(Arg.Any<int>(), Arg.Any<bool>(), Arg.Any<CancellationToken>()).Returns(TestData.Product());
        return new ProductService(
            _products,
            _categories,
            Substitute.For<IUserRepository>(),
            _unitOfWork,
            TestData.CurrentUser(role),
            new UserMetricService(_metrics, _clock),
            _clock);
    }

    private static ProductRequest NewProduct(int categoryId = 2) =>
        new() { Name = "  Polarizer  ", Description = " ", Price = 12.5m, Stock = 3, CategoryId = categoryId };

    [Fact]
    public async Task Create_requires_a_user()
    {
        var error = await Assert.ThrowsAsync<AppException>(() => Service(role: null).CreateAsync(NewProduct(), default));

        Assert.Equal(ErrorCode.UserRequired, error.Code);
    }

    [Fact]
    public async Task Create_is_forbidden_for_normal_users_and_saves_nothing()
    {
        var error = await Assert.ThrowsAsync<AppException>(() => Service(Role.User).CreateAsync(NewProduct(), default));

        Assert.Equal(ErrorCode.Forbidden, error.Code);
        _products.DidNotReceive().Add(Arg.Any<Product>());
        Assert.Equal(0, _unitOfWork.Saves);
    }

    [Theory]
    [InlineData(Category.UncategorizedId)]
    [InlineData(999)]
    public async Task Create_rejects_uncategorized_and_unknown_categories(int categoryId)
    {
        _categories.ExistsAsync(999, Arg.Any<CancellationToken>()).Returns(false);

        var error = await Assert.ThrowsAsync<AppException>(() => Service(Role.Editor).CreateAsync(NewProduct(categoryId), default));

        Assert.Equal(ErrorCode.InvalidCategory, error.Code);
    }

    [Fact]
    public async Task Create_cleans_input_sets_server_fields_and_records_a_metric_with_the_new_id()
    {
        _categories.ExistsAsync(2, Arg.Any<CancellationToken>()).Returns(true);
        Product? added = null;
        _products.When(p => p.Add(Arg.Any<Product>())).Do(call =>
        {
            added = call.Arg<Product>();
            added.Id = 100100; // The database would assign it from the sequence.
        });

        await Service(Role.Editor).CreateAsync(NewProduct(), default);

        Assert.NotNull(added);
        Assert.Equal("Polarizer", added.Name);
        Assert.Null(added.Description);
        Assert.Equal(TestData.Now.UtcDateTime, added.CreatedAt);
        Assert.Equal(7, added.UpdatedByUserId);
        _metrics.Received(1).Add(Arg.Is<UserMetric>(m =>
            m.Action == MetricAction.Create && m.Entity == MetricEntity.Product && m.EntityId == 100100 && m.UserId == 7));
    }

    [Fact]
    public async Task Update_checks_the_version_and_records_what_changed()
    {
        var product = TestData.Product(price: 10m);
        _products.FindForUpdateAsync(100000, Arg.Any<CancellationToken>()).Returns(product);
        var request = new UpdateProductRequest { Name = "Lens", Price = 12.5m, Stock = 10, CategoryId = 2, RowVersion = [9, 9] };

        await Service(Role.Editor).UpdateAsync(100000, request, default);

        _products.Received(1).ExpectVersion(product, Arg.Is<byte[]>(v => v.SequenceEqual(new byte[] { 9, 9 })));
        _metrics.Received(1).Add(Arg.Is<UserMetric>(m => m.Details == "Price 10 → 12.5"));
        Assert.Equal(1, _unitOfWork.Saves);
    }

    [Fact]
    public async Task Decrement_more_than_available_is_insufficient_stock_and_records_nothing()
    {
        _products.TryChangeStockAsync(100000, -5, Arg.Any<bool>(), Arg.Any<int>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns((int?)null);
        _products.ExistsAsync(100000, Arg.Any<bool>(), Arg.Any<CancellationToken>()).Returns(true);

        var error = await Assert.ThrowsAsync<AppException>(() => Service(Role.User).DecrementStockAsync(100000, 5, default));

        Assert.Equal(ErrorCode.InsufficientStock, error.Code);
        _metrics.DidNotReceive().Add(Arg.Any<UserMetric>());
    }

    [Fact]
    public async Task Stock_change_on_unknown_or_hidden_product_is_not_found()
    {
        _products.TryChangeStockAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<bool>(), Arg.Any<int>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns((int?)null);
        _products.ExistsAsync(Arg.Any<int>(), Arg.Any<bool>(), Arg.Any<CancellationToken>()).Returns(false);

        var error = await Assert.ThrowsAsync<AppException>(() => Service(Role.User).AddStockAsync(123456, 1, default));

        Assert.Equal(ErrorCode.ProductNotFound, error.Code);
    }

    [Fact]
    public async Task Add_stock_records_the_old_and_new_stock()
    {
        _products.TryChangeStockAsync(100000, 5, Arg.Any<bool>(), 7, TestData.Now.UtcDateTime, Arg.Any<CancellationToken>())
            .Returns(17);

        await Service(Role.User).AddStockAsync(100000, 5, default);

        _metrics.Received(1).Add(Arg.Is<UserMetric>(m => m.Action == MetricAction.AddStock && m.Details == "Stock +5 (12 → 17)"));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    [InlineData(ProductLimits.QuantityMax + 1)]
    public async Task Invalid_quantity_is_rejected_before_touching_the_database(int quantity)
    {
        var error = await Assert.ThrowsAsync<AppException>(() => Service(Role.User).AddStockAsync(100000, quantity, default));

        Assert.Equal(ErrorCode.InvalidQuantity, error.Code);
        await _products.DidNotReceiveWithAnyArgs().TryChangeStockAsync(default, default, default, default, default, default);
    }

    [Fact]
    public async Task Normal_users_never_get_hidden_products_even_when_asking()
    {
        _products.ListAsync(Arg.Any<ProductFilter>(), Arg.Any<CancellationToken>()).Returns([]);

        await Service(Role.User).GetAllAsync(new ProductQuery { Statuses = [ProductStatus.Disabled] }, default);

        await _products.Received(1).ListAsync(
            Arg.Is<ProductFilter>(f => f.Statuses.SequenceEqual(new[] { ProductStatus.Active })), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Editors_get_hidden_products_when_asking()
    {
        _products.ListAsync(Arg.Any<ProductFilter>(), Arg.Any<CancellationToken>()).Returns([]);

        await Service(Role.Editor).GetAllAsync(new ProductQuery { Statuses = [ProductStatus.Disabled] }, default);

        await _products.Received(1).ListAsync(
            Arg.Is<ProductFilter>(f => f.Statuses.SequenceEqual(new[] { ProductStatus.Disabled })), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("   ")]
    public async Task Search_without_a_name_is_a_validation_error(string? name)
    {
        var error = await Assert.ThrowsAsync<AppException>(() => Service(role: null).SearchAsync(name, default));

        Assert.Equal(ErrorCode.ValidationFailed, error.Code);
    }

    [Fact]
    public async Task Search_trims_the_name()
    {
        _products.ListAsync(Arg.Any<ProductFilter>(), Arg.Any<CancellationToken>()).Returns([]);

        await Service(role: null).SearchAsync("  lens ", default);

        await _products.Received(1).ListAsync(Arg.Is<ProductFilter>(f => f.NameContains == "lens"), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Filters_are_passed_on_together()
    {
        _products.ListAsync(Arg.Any<ProductFilter>(), Arg.Any<CancellationToken>()).Returns([]);
        var query = new ProductQuery
        {
            Search = " lens ",
            CategoryIds = [2, 3, 2],
            StockStatuses = [StockStatus.LowStock],
            MinPrice = 10,
            MaxPrice = 300,
        };

        await Service(Role.Editor).GetAllAsync(query, default);

        await _products.Received(1).ListAsync(
            Arg.Is<ProductFilter>(f =>
                f.NameContains == "lens" &&
                f.CategoryIds.SequenceEqual(new[] { 2, 3 }) &&
                f.StockStatuses.SequenceEqual(new[] { StockStatus.LowStock }) &&
                f.MinPrice == 10 && f.MaxPrice == 300),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Min_price_above_max_price_is_rejected()
    {
        var error = await Assert.ThrowsAsync<AppException>(() =>
            Service(role: null).GetAllAsync(new ProductQuery { MinPrice = 50, MaxPrice = 10 }, default));

        Assert.Equal(ErrorCode.InvalidPriceRange, error.Code);
    }

    [Theory]
    [InlineData(10, 5)]
    [InlineData(-1, null)]
    [InlineData(null, -1)]
    public async Task Invalid_stock_range_is_rejected(int? min, int? max)
    {
        var error = await Assert.ThrowsAsync<AppException>(() => Service(role: null).GetByStockLevelAsync(min, max, default));

        Assert.Equal(ErrorCode.InvalidStockRange, error.Code);
    }
}
