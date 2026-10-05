using Products.Application.Abstractions;
using Products.Application.Dtos.Metrics;
using Products.Domain.Authorization;
using Products.Domain.Errors;

namespace Products.Application.Services;

// Metrics tab. Product metrics: editors and admins. User metrics (personal activity): admins only.
public class MetricsService(IMetricsRepository metrics, CurrentUser currentUser, TimeProvider clock)
{
    public const int MaxDays = 365;
    public const int MaxStockHistoryProducts = 5;
    private const int TopProducts = 10;
    private const int ActiveUserDays = 7;

    public async Task<ProductMetricsResponse> GetProductMetricsAsync(int days, CancellationToken cancellationToken)
    {
        await currentUser.RequireAsync(Permission.ViewProductMetrics, cancellationToken);
        var from = PeriodStart(days);

        return new ProductMetricsResponse(
            await metrics.GetProductKpisAsync(from, cancellationToken),
            FillDays(await metrics.GetMovementsPerDayAsync(from, cancellationToken), from, p => p.Date, d => new StockMovementPoint(d, 0, 0)),
            await metrics.GetTopRemovedAsync(from, TopProducts, cancellationToken));
    }

    public async Task<List<ProductStockHistoryResponse>> GetStockHistoryAsync(
        IReadOnlyCollection<int> productIds, int days, CancellationToken cancellationToken)
    {
        await currentUser.RequireAsync(Permission.ViewProductMetrics, cancellationToken);
        var ids = productIds.Distinct().ToList();
        if (ids.Count is 0 or > MaxStockHistoryProducts)
        {
            throw new AppException(ErrorCode.ValidationFailed, $"Choose between 1 and {MaxStockHistoryProducts} products.", "productIds");
        }

        return await metrics.GetStockHistoryAsync(ids, PeriodStart(days), Now(), cancellationToken);
    }

    public async Task<UserMetricsResponse> GetUserMetricsAsync(int days, CancellationToken cancellationToken)
    {
        await currentUser.RequireAsync(Permission.ViewUserMetrics, cancellationToken);
        var from = PeriodStart(days);

        return new UserMetricsResponse(
            await metrics.GetUserKpisAsync(from, Now().AddDays(-ActiveUserDays), cancellationToken),
            FillDays(await metrics.GetActivityPerDayAsync(from, cancellationToken), from, p => p.Date, d => new ActivityPoint(d, 0, 0, 0)),
            await metrics.GetActivityPerUserAsync(from, cancellationToken),
            await metrics.GetActivityPerHourAsync(from, cancellationToken));
    }

    // The period starts at midnight (UTC), so the first day is complete.
    private DateTime PeriodStart(int days)
    {
        if (days is < 1 or > MaxDays)
        {
            throw new AppException(ErrorCode.InvalidTimeRange, $"Days must be between 1 and {MaxDays}.", "days");
        }

        return Now().Date.AddDays(-(days - 1));
    }

    // Timelines need a point for every day, also days without activity.
    private List<T> FillDays<T>(List<T> points, DateTime from, Func<T, DateOnly> dateOf, Func<DateOnly, T> empty)
    {
        var byDate = points.ToDictionary(dateOf);
        var first = DateOnly.FromDateTime(from);
        var last = DateOnly.FromDateTime(Now());
        var result = new List<T>();
        for (var date = first; date <= last; date = date.AddDays(1))
        {
            result.Add(byDate.TryGetValue(date, out var point) ? point : empty(date));
        }

        return result;
    }

    private DateTime Now() => clock.GetUtcNow().UtcDateTime;
}
