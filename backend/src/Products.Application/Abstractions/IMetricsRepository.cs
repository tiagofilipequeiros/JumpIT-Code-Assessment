using Products.Application.Dtos.Metrics;

namespace Products.Application.Abstractions;

// Aggregations over UserMetrics, the current products and the product history. All run in the database.
public interface IMetricsRepository
{
    Task<ProductKpis> GetProductKpisAsync(DateTime from, CancellationToken cancellationToken);
    Task<List<StockMovementPoint>> GetMovementsPerDayAsync(DateTime from, CancellationToken cancellationToken);
    Task<List<ProductUnits>> GetTopRemovedAsync(DateTime from, int top, CancellationToken cancellationToken);
    Task<List<ProductStockHistoryResponse>> GetStockHistoryAsync(IReadOnlyCollection<int> productIds, DateTime from, DateTime to, CancellationToken cancellationToken);

    Task<UserKpis> GetUserKpisAsync(DateTime from, DateTime activeSince, CancellationToken cancellationToken);
    Task<List<ActivityPoint>> GetActivityPerDayAsync(DateTime from, CancellationToken cancellationToken);
    Task<List<UserActivity>> GetActivityPerUserAsync(DateTime from, CancellationToken cancellationToken);
    Task<List<HourlyActivity>> GetActivityPerHourAsync(DateTime from, CancellationToken cancellationToken);
}
