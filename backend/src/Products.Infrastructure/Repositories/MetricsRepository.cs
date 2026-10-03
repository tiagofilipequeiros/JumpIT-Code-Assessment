using Microsoft.EntityFrameworkCore;
using Products.Application.Abstractions;
using Products.Application.Dtos.Metrics;
using Products.Domain.Entities;
using Products.Infrastructure.Persistence;

namespace Products.Infrastructure.Repositories;

// Every method is one aggregated SQL query; only the small results come back.
public class MetricsRepository(AppDbContext db) : IMetricsRepository
{
    public async Task<ProductKpis> GetProductKpisAsync(DateTime from, CancellationToken cancellationToken)
    {
        var active = db.Products.AsNoTracking().OnlyVisible();
        var stockEvents = StockEvents(from);

        return new ProductKpis(
            await active.SumAsync(p => p.Price * p.Stock, cancellationToken),
            await active.CountAsync(p => p.Stock == 0, cancellationToken),
            await active.CountAsync(p => p.Stock > 0 && p.Stock <= ProductLimits.LowStockThreshold, cancellationToken),
            await stockEvents.Where(m => m.Action == MetricAction.AddStock).SumAsync(m => m.Quantity ?? 0, cancellationToken),
            await stockEvents.Where(m => m.Action == MetricAction.DecrementStock).SumAsync(m => m.Quantity ?? 0, cancellationToken));
    }

    public async Task<List<StockMovementPoint>> GetMovementsPerDayAsync(DateTime from, CancellationToken cancellationToken)
    {
        var days = await StockEvents(from)
            .GroupBy(m => m.CreatedAt.Date)
            .Select(g => new
            {
                Date = g.Key,
                Added = g.Sum(m => m.Action == MetricAction.AddStock ? m.Quantity ?? 0 : 0),
                Removed = g.Sum(m => m.Action == MetricAction.DecrementStock ? m.Quantity ?? 0 : 0),
            })
            .ToListAsync(cancellationToken);

        return days.Select(d => new StockMovementPoint(DateOnly.FromDateTime(d.Date), d.Added, d.Removed)).ToList();
    }

    public async Task<List<ProductUnits>> GetTopRemovedAsync(DateTime from, int top, CancellationToken cancellationToken)
    {
        var totals = await StockEvents(from)
            .Where(m => m.Action == MetricAction.DecrementStock && m.EntityId != null)
            .GroupBy(m => m.EntityId!.Value)
            .Select(g => new { ProductId = g.Key, Units = g.Sum(m => m.Quantity ?? 0) })
            .OrderByDescending(t => t.Units)
            .Take(top)
            .ToListAsync(cancellationToken);

        var ids = totals.Select(t => t.ProductId).ToList();
        var names = await db.Products.AsNoTracking()
            .Where(p => ids.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, p => p.Name, cancellationToken);

        // Metrics survive deletes (no foreign key), so a product may no longer exist.
        return totals
            .Select(t => new ProductUnits(t.ProductId, names.GetValueOrDefault(t.ProductId) ?? $"Deleted product {t.ProductId}", t.Units))
            .ToList();
    }

    public async Task<List<ProductStockHistoryResponse>> GetStockHistoryAsync(
        IReadOnlyCollection<int> productIds, DateTime from, DateTime to, CancellationToken cancellationToken)
    {
        var versions = await db.Products
            .TemporalAll()
            .AsNoTracking()
            .Where(p => productIds.Contains(p.Id) && EF.Property<DateTime>(p, AppDbContext.PeriodEnd) > from)
            .OrderBy(p => EF.Property<DateTime>(p, AppDbContext.PeriodStart))
            .Select(p => new { p.Id, p.Name, p.Stock, Start = EF.Property<DateTime>(p, AppDbContext.PeriodStart) })
            .ToListAsync(cancellationToken);

        return versions
            .GroupBy(v => v.Id)
            .Select(product =>
            {
                var points = new List<StockPoint>();
                foreach (var version in product)
                {
                    // Versions where only other fields changed don't add a step.
                    if (points.Count > 0 && points[^1].Stock == version.Stock)
                    {
                        continue;
                    }

                    var start = version.Start < from ? from : version.Start;
                    points.Add(new StockPoint(DateTime.SpecifyKind(start, DateTimeKind.Utc), version.Stock));
                }

                // Extend the last step to "now" so the line reaches the right edge of the chart.
                points.Add(new StockPoint(to, points[^1].Stock));
                return new ProductStockHistoryResponse(product.Key, product.Last().Name, points);
            })
            .ToList();
    }

    public async Task<UserKpis> GetUserKpisAsync(DateTime from, DateTime activeSince, CancellationToken cancellationToken)
    {
        var period = Activity(from);
        return new UserKpis(
            await db.UserMetrics.Where(m => m.CreatedAt >= activeSince).Select(m => m.UserId).Distinct().CountAsync(cancellationToken),
            await period.CountAsync(m => m.Action == MetricAction.Login, cancellationToken),
            await period.CountAsync(m => MetricActionGroups.Edits.Contains(m.Action), cancellationToken),
            await period.CountAsync(m => MetricActionGroups.StockChanges.Contains(m.Action), cancellationToken));
    }

    public async Task<List<ActivityPoint>> GetActivityPerDayAsync(DateTime from, CancellationToken cancellationToken)
    {
        var days = await Activity(from)
            .GroupBy(m => m.CreatedAt.Date)
            .Select(g => new
            {
                Date = g.Key,
                Logins = g.Count(m => m.Action == MetricAction.Login),
                Edits = g.Count(m => MetricActionGroups.Edits.Contains(m.Action)),
                StockChanges = g.Count(m => MetricActionGroups.StockChanges.Contains(m.Action)),
            })
            .ToListAsync(cancellationToken);

        return days.Select(d => new ActivityPoint(DateOnly.FromDateTime(d.Date), d.Logins, d.Edits, d.StockChanges)).ToList();
    }

    public async Task<List<UserActivity>> GetActivityPerUserAsync(DateTime from, CancellationToken cancellationToken)
    {
        var counts = await Activity(from)
            .GroupBy(m => m.UserId)
            .Select(g => new
            {
                UserId = g.Key,
                Logins = g.Count(m => m.Action == MetricAction.Login),
                Edits = g.Count(m => MetricActionGroups.Edits.Contains(m.Action)),
                StockChanges = g.Count(m => MetricActionGroups.StockChanges.Contains(m.Action)),
            })
            .ToDictionaryAsync(c => c.UserId, cancellationToken);

        // Every user is listed, also those without activity in the period.
        var users = await db.Users.AsNoTracking().OrderBy(u => u.Id).ToListAsync(cancellationToken);
        return users
            .Select(u => counts.TryGetValue(u.Id, out var c)
                ? new UserActivity(u.Id, u.Name, u.Role, c.Logins, c.Edits, c.StockChanges)
                : new UserActivity(u.Id, u.Name, u.Role, 0, 0, 0))
            .ToList();
    }

    public async Task<List<HourlyActivity>> GetActivityPerHourAsync(DateTime from, CancellationToken cancellationToken)
    {
        var hours = await Activity(from)
            .GroupBy(m => new { m.CreatedAt.Date, m.CreatedAt.Hour })
            .Select(g => new { g.Key.Date, g.Key.Hour, Count = g.Count() })
            .ToListAsync(cancellationToken);

        return hours
            .Select(h => new HourlyActivity(DateTime.SpecifyKind(h.Date.AddHours(h.Hour), DateTimeKind.Utc), h.Count))
            .OrderBy(h => h.HourUtc)
            .ToList();
    }

    private IQueryable<UserMetric> Activity(DateTime from) =>
        db.UserMetrics.AsNoTracking().Where(m => m.CreatedAt >= from);

    private IQueryable<UserMetric> StockEvents(DateTime from) =>
        Activity(from).Where(m => m.Quantity != null);
}
