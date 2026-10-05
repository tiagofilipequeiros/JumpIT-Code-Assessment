using System.Globalization;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Products.Domain.Entities;
using Products.Infrastructure.Repositories;

namespace Products.Infrastructure.Persistence;

// Generates ~90 days of realistic activity ending now (logins, stock movements, a few price changes) together with
// the matching product history, so the Metrics tab has data on a fresh install.
// - Turned on with the setting Seeding:DemoActivity (docker-compose and local runs; off in tests and production).
// - Runs once: skipped as soon as any activity exists. Deterministic (fixed random seed); dates are relative to now,
//   which is why this cannot be a migration (migrations only hold fixed dates).
// - Each product's simulated stock ends exactly at its current stock, so history and current data agree. The current row
//   is moved to start at the last simulated change (rows created before the table became temporal start in year 0001).
public class DemoActivitySeeder(AppDbContext db, TimeProvider clock)
{
    private const int Days = 90;
    private const int Admin = 1;
    private const int Editor = 2;
    private const int Sam = 3;
    private const int Taylor = 4;

    // Busier mid-morning and mid-afternoon, so the heatmap shows a real working pattern.
    private static readonly int[] WorkHours = [8, 9, 9, 10, 10, 10, 11, 11, 12, 13, 14, 14, 15, 15, 15, 16, 16, 17];

    // Products that get one price increase, and on which day of the period (spread out, so every range shows some).
    private static readonly Dictionary<int, int> PriceChangeDay = new()
    {
        [100000] = 80, [100001] = 65, [100003] = 50, [100006] = 35, [100009] = 86,
    };

    private readonly Random _random = new(42);
    private readonly List<UserMetric> _metrics = [];
    private readonly List<HistoryRow> _history = [];
    private readonly List<(int ProductId, Version Last)> _currentVersions = [];

    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        var strategy = db.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

            // Several API instances may start at the same time: only the first one seeds.
            await db.Database.ExecuteSqlRawAsync(
                "EXEC sp_getapplock @Resource = 'DemoActivitySeed', @LockMode = 'Exclusive', @LockOwner = 'Transaction'",
                cancellationToken);
            if (await db.UserMetrics.AnyAsync(cancellationToken))
            {
                return;
            }

            var now = clock.GetUtcNow().UtcDateTime;
            var start = now.Date.AddDays(-Days);
            var products = await db.Products
                .AsNoTracking()
                .OnlyVisible()
                .OrderBy(p => p.Id)
                .ToListAsync(cancellationToken);

            AddLogins(start, now);
            foreach (var product in products)
            {
                SimulateProduct(product, start, now);
            }

            db.UserMetrics.AddRange(_metrics);
            await db.SaveChangesAsync(cancellationToken);
            await WriteHistoryAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        });
    }

    private void AddLogins(DateTime start, DateTime end)
    {
        // How likely each user logs in on a working day.
        var habits = new Dictionary<int, double> { [Sam] = 0.9, [Taylor] = 0.85, [Editor] = 0.7, [Admin] = 0.35 };
        for (var day = start; day < end; day = day.AddDays(1))
        {
            foreach (var (userId, chance) in habits)
            {
                var time = day.AddHours(_random.Next(8, 10)).AddMinutes(_random.Next(60));
                if (_random.NextDouble() < (IsWeekend(day) ? chance / 10 : chance) && time < end)
                {
                    Record(userId, MetricEntity.User, MetricAction.Login, userId, time);
                }
            }
        }
    }

    // Walks backwards from today's real stock, so the simulated history ends exactly at the current values:
    // going back in time a removal means there was more stock before it, a restock means there was less.
    private void SimulateProduct(Product product, DateTime start, DateTime end)
    {
        var velocity = Math.Max(0.5, product.Stock / 15.0);             // Typical units removed per working day.
        var reorderPoint = (int)Math.Ceiling(velocity * 4);
        var restockBatch = Math.Max(10, (int)Math.Ceiling(velocity * 20 / 10) * 10);
        // Out of stock today: it ran out about 12 days ago and nothing moved since.
        var quietFrom = product.Stock == 0 ? end.AddDays(-12) : end;
        DateTime? priceChange = PriceChangeDay.TryGetValue(product.Id, out var changeDay) ? At(start.AddDays(changeDay), 7) : null;
        var oldPrice = Math.Round(product.Price / 1.08m, 2);
        decimal PriceAt(DateTime time) => priceChange is { } change && time < change ? oldPrice : product.Price;

        var stock = product.Stock;
        var changes = new List<Version>();                             // Newest first; each holds the stock after it.
        for (var day = end.Date; day >= start; day = day.AddDays(-1))
        {
            var weekend = IsWeekend(day);
            var events = new List<(DateTime Time, int Quantity, bool Restock, int UserId)>();
            if (!weekend && _random.NextDouble() < 0.6)
            {
                events.Add((At(day, _random.Next(9, 11)), restockBatch, true, _random.NextDouble() < 0.8 ? Editor : Admin));
            }

            var removals = weekend ? (_random.NextDouble() < 0.15 ? 1 : 0) : _random.Next(0, 3) + (velocity > 10 ? 2 : 0);
            for (var i = 0; i < removals; i++)
            {
                var quantity = _random.Next(1, Math.Max(2, (int)Math.Round(velocity)) + 1);
                var userId = _random.NextDouble() switch { < 0.5 => Sam, < 0.9 => Taylor, _ => Editor };
                events.Add((At(day, WorkHours[_random.Next(WorkHours.Length)]), quantity, false, userId));
            }

            foreach (var e in events.Where(e => e.Time < quietFrom).OrderByDescending(e => e.Time))
            {
                if (priceChange is { } change && e.Time < change && (changes.Count == 0 || changes[^1].Start > change))
                {
                    AddPriceChange(product, changes, oldPrice, change, stock);
                }

                // A restock only happens when the stock before it was low (forwards: "reorder when low").
                if (e.Restock && stock < restockBatch + reorderPoint)
                {
                    continue;
                }

                var before = e.Restock ? stock - e.Quantity : stock + e.Quantity;
                Record(
                    e.UserId,
                    MetricEntity.Product,
                    e.Restock ? MetricAction.AddStock : MetricAction.DecrementStock,
                    product.Id,
                    e.Time,
                    $"Stock {(e.Restock ? "+" : "-")}{e.Quantity} ({before} → {stock})",
                    e.Quantity);
                changes.Add(new Version(e.Time, stock, PriceAt(e.Time), e.UserId));
                stock = before;
            }
        }

        if (priceChange is { } lastChance && (changes.Count == 0 || changes[^1].Start > lastChance))
        {
            AddPriceChange(product, changes, oldPrice, lastChance, stock);
        }

        var versions = new List<Version> { new(start, stock, PriceAt(start), null) };
        versions.AddRange(Enumerable.Reverse(changes));
        AddHistory(product, versions);
    }

    private void AddPriceChange(Product product, List<Version> changes, decimal oldPrice, DateTime time, int stock)
    {
        Record(Editor, MetricEntity.Product, MetricAction.Update, product.Id, time,
            string.Create(CultureInfo.InvariantCulture, $"Price {oldPrice} → {product.Price}"));
        changes.Add(new Version(time, stock, product.Price, Editor));
    }

    // Every version but the last becomes a history row, valid until the next one. The last one is the current row.
    private void AddHistory(Product product, List<Version> versions)
    {
        for (var i = 0; i + 1 < versions.Count; i++)
        {
            if (versions[i + 1].Start > versions[i].Start)
            {
                _history.Add(new HistoryRow(product, versions[i], versions[i + 1].Start));
            }
        }

        _currentVersions.Add((product.Id, versions[^1]));
    }

    // Writing past versions requires switching system versioning off and removing the period for a moment
    // (inside this transaction). All values come from the database or from this class, never from user input.
    private async Task WriteHistoryAsync(CancellationToken cancellationToken)
    {
        await db.Database.ExecuteSqlRawAsync("ALTER TABLE [Products] SET (SYSTEM_VERSIONING = OFF)", cancellationToken);
        await db.Database.ExecuteSqlRawAsync("ALTER TABLE [Products] DROP PERIOD FOR SYSTEM_TIME", cancellationToken);

        foreach (var batch in _history.Chunk(500))
        {
            var sql = new StringBuilder(
                "INSERT INTO [ProductsHistory] ([Id], [Name], [Description], [Price], [Stock], [CategoryId], [IsActive], " +
                "[CreatedAt], [UpdatedAt], [UpdatedByUserId], [PeriodStart], [PeriodEnd]) VALUES ");
            sql.AppendJoin(", ", batch.Select(row => row.ToSqlValues()));
            await db.Database.ExecuteSqlRawAsync(sql.ToString(), cancellationToken);
        }

        foreach (var (productId, last) in _currentVersions)
        {
            await db.Database.ExecuteSqlRawAsync(string.Create(CultureInfo.InvariantCulture,
                $"UPDATE [Products] SET [PeriodStart] = {SqlDate(last.Start)}, [UpdatedAt] = {SqlDate(last.Start)}, " +
                $"[UpdatedByUserId] = {last.UpdatedByUserId?.ToString(CultureInfo.InvariantCulture) ?? "NULL"} WHERE [Id] = {productId}"),
                cancellationToken);
        }

        await db.Database.ExecuteSqlRawAsync(
            $"ALTER TABLE [Products] ADD PERIOD FOR SYSTEM_TIME ([{AppDbContext.PeriodStart}], [{AppDbContext.PeriodEnd}])", cancellationToken);
        await db.Database.ExecuteSqlRawAsync(
            "ALTER TABLE [Products] SET (SYSTEM_VERSIONING = ON (HISTORY_TABLE = [dbo].[ProductsHistory]))", cancellationToken);
    }

    // Invariant culture: ":" in a custom format is the culture's time separator otherwise.
    private static string SqlDate(DateTime value) =>
        "'" + value.ToString("yyyy-MM-ddTHH:mm:ss.fffffff", CultureInfo.InvariantCulture) + "'";

    private void Record(
        int userId, MetricEntity entity, MetricAction action, int entityId, DateTime time, string? details = null, int? quantity = null) =>
        _metrics.Add(new UserMetric
        {
            UserId = userId,
            Entity = entity,
            Action = action,
            EntityId = entityId,
            Details = details,
            Quantity = quantity,
            CreatedAt = time,
        });

    private DateTime At(DateTime day, int hour) => day.AddHours(hour).AddMinutes(_random.Next(60)).AddSeconds(_random.Next(60));

    private static bool IsWeekend(DateTime day) => day.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;

    private sealed record Version(DateTime Start, int Stock, decimal Price, int? UpdatedByUserId);

    private sealed record HistoryRow(Product Product, Version Version, DateTime End)
    {
        public string ToSqlValues() => string.Create(CultureInfo.InvariantCulture,
            $"({Product.Id}, {Text(Product.Name)}, {Text(Product.Description)}, {Version.Price}, {Version.Stock}, " +
            $"{Product.CategoryId}, {(Product.IsActive ? 1 : 0)}, {Date(Product.CreatedAt)}, {Date(Version.Start)}, " +
            $"{Version.UpdatedByUserId?.ToString(CultureInfo.InvariantCulture) ?? "NULL"}, {Date(Version.Start)}, {Date(End)})");

        private static string Text(string? value) => value is null ? "NULL" : $"N'{value.Replace("'", "''")}'";

        private static string Date(DateTime value) => SqlDate(value);
    }
}
