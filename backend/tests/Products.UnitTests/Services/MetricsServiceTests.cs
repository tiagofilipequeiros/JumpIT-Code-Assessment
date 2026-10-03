using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using Products.Application.Abstractions;
using Products.Application.Dtos.Metrics;
using Products.Application.Services;
using Products.Domain.Entities;
using Products.Domain.Errors;
using Products.UnitTests.Fakes;

namespace Products.UnitTests.Services;

public class MetricsServiceTests
{
    private readonly IMetricsRepository _metrics = Substitute.For<IMetricsRepository>();
    private readonly FakeTimeProvider _clock = new(TestData.Now);

    private MetricsService Service(Role? role)
    {
        _metrics.GetProductKpisAsync(Arg.Any<DateTime>(), Arg.Any<CancellationToken>()).Returns(new ProductKpis(0, 0, 0, 0, 0));
        _metrics.GetUserKpisAsync(Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>()).Returns(new UserKpis(0, 0, 0, 0));
        _metrics.GetMovementsPerDayAsync(Arg.Any<DateTime>(), Arg.Any<CancellationToken>()).Returns([]);
        _metrics.GetActivityPerDayAsync(Arg.Any<DateTime>(), Arg.Any<CancellationToken>()).Returns([]);
        _metrics.GetTopRemovedAsync(Arg.Any<DateTime>(), Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns([]);
        _metrics.GetActivityPerUserAsync(Arg.Any<DateTime>(), Arg.Any<CancellationToken>()).Returns([]);
        _metrics.GetActivityPerHourAsync(Arg.Any<DateTime>(), Arg.Any<CancellationToken>()).Returns([]);
        return new MetricsService(_metrics, TestData.CurrentUser(role), _clock);
    }

    [Fact]
    public async Task Editors_see_product_metrics_but_not_user_metrics()
    {
        var service = Service(Role.Editor);

        await service.GetProductMetricsAsync(30, default);
        var error = await Assert.ThrowsAsync<AppException>(() => service.GetUserMetricsAsync(30, default));

        Assert.Equal(ErrorCode.Forbidden, error.Code);
    }

    [Fact]
    public async Task Normal_users_see_no_metrics()
    {
        var error = await Assert.ThrowsAsync<AppException>(() => Service(Role.User).GetProductMetricsAsync(30, default));

        Assert.Equal(ErrorCode.Forbidden, error.Code);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(MetricsService.MaxDays + 1)]
    public async Task Days_out_of_range_are_rejected(int days)
    {
        var error = await Assert.ThrowsAsync<AppException>(() => Service(Role.Admin).GetProductMetricsAsync(days, default));

        Assert.Equal(ErrorCode.InvalidTimeRange, error.Code);
    }

    [Fact]
    public async Task The_period_starts_at_midnight_and_includes_today()
    {
        await Service(Role.Admin).GetProductMetricsAsync(7, default);

        // Now is 1 March 12:00 UTC; 7 days = 23 February 00:00 up to now.
        await _metrics.Received(1).GetProductKpisAsync(new DateTime(2026, 2, 23, 0, 0, 0, DateTimeKind.Utc), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Timelines_have_a_point_for_every_day_with_zeros_for_quiet_days()
    {
        _metrics.GetMovementsPerDayAsync(Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns([new StockMovementPoint(new DateOnly(2026, 2, 25), 10, 4)]);
        var service = new MetricsService(_metrics, TestData.CurrentUser(Role.Admin), _clock);
        _metrics.GetProductKpisAsync(Arg.Any<DateTime>(), Arg.Any<CancellationToken>()).Returns(new ProductKpis(0, 0, 0, 0, 0));
        _metrics.GetTopRemovedAsync(Arg.Any<DateTime>(), Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns([]);

        var result = await service.GetProductMetricsAsync(7, default);

        Assert.Equal(7, result.MovementsPerDay.Count);
        Assert.Equal(new DateOnly(2026, 2, 23), result.MovementsPerDay[0].Date);
        Assert.Equal(new DateOnly(2026, 3, 1), result.MovementsPerDay[^1].Date);
        Assert.Equal(10, result.MovementsPerDay.Single(p => p.Date == new DateOnly(2026, 2, 25)).Added);
        Assert.Equal(6, result.MovementsPerDay.Count(p => p.Added == 0 && p.Removed == 0));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(MetricsService.MaxStockHistoryProducts + 1)]
    public async Task Stock_history_needs_one_to_five_products(int count)
    {
        var ids = Enumerable.Range(100000, count).ToList();

        var error = await Assert.ThrowsAsync<AppException>(() => Service(Role.Editor).GetStockHistoryAsync(ids, 30, default));

        Assert.Equal(ErrorCode.ValidationFailed, error.Code);
    }
}
