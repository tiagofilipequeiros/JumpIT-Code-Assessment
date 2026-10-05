using Microsoft.EntityFrameworkCore;
using Products.Application.Abstractions;
using Products.Domain.Entities;
using Products.Infrastructure.Persistence;

namespace Products.Infrastructure.Repositories;

public class ProductRepository(AppDbContext db) : IProductRepository
{
    public async Task<List<Product>> ListAsync(ProductFilter filter, CancellationToken cancellationToken)
    {
        var query = db.Products.AsQueryable()
            .WithStatus(filter.Statuses)
            .WithStockStatus(filter.StockStatuses);

        if (filter.NameContains is { } name)
        {
            // Translated to LIKE '%name%' (wildcards in the input are escaped).
            query = query.Where(p => p.Name.Contains(name));
        }

        if (filter.CategoryIds.Count > 0)
        {
            query = query.Where(p => filter.CategoryIds.Contains(p.CategoryId));
        }

        if (filter.MinStock is { } minStock)
        {
            query = query.Where(p => p.Stock >= minStock);
        }

        if (filter.MaxStock is { } maxStock)
        {
            query = query.Where(p => p.Stock <= maxStock);
        }

        if (filter.MinPrice is { } minPrice)
        {
            query = query.Where(p => p.Price >= minPrice);
        }

        if (filter.MaxPrice is { } maxPrice)
        {
            query = query.Where(p => p.Price <= maxPrice);
        }

        return await WithDetails(query).OrderBy(p => p.Id).ToListAsync(cancellationToken);
    }

    public Task<Product?> GetAsync(int id, bool includeHidden, CancellationToken cancellationToken) =>
        WithDetails(Visible(includeHidden)).FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public Task<bool> ExistsAsync(int id, bool includeHidden, CancellationToken cancellationToken) =>
        Visible(includeHidden).AnyAsync(p => p.Id == id, cancellationToken);

    public async Task<List<ProductVersion>> GetHistoryAsync(int id, CancellationToken cancellationToken)
    {
        var versions = await db.Products
            .TemporalAll()
            .AsNoTracking()
            .Where(p => p.Id == id)
            .OrderBy(p => EF.Property<DateTime>(p, AppDbContext.PeriodStart))
            .Select(p => new
            {
                p.Name,
                p.Price,
                p.Stock,
                p.IsActive,
                p.CategoryId,
                p.UpdatedByUserId,
                ValidFrom = EF.Property<DateTime>(p, AppDbContext.PeriodStart),
                ValidTo = EF.Property<DateTime>(p, AppDbContext.PeriodEnd),
            })
            .ToListAsync(cancellationToken);

        return versions
            .Select(v => new ProductVersion(
                v.Name,
                v.Price,
                v.Stock,
                v.IsActive,
                v.CategoryId,
                v.UpdatedByUserId,
                DateTime.SpecifyKind(v.ValidFrom, DateTimeKind.Utc),
                // The current version is "valid until" the end of time.
                v.ValidTo.Year == DateTime.MaxValue.Year ? null : DateTime.SpecifyKind(v.ValidTo, DateTimeKind.Utc)))
            .ToList();
    }

    public Task<Product?> FindForUpdateAsync(int id, CancellationToken cancellationToken) =>
        db.Products.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public void Add(Product product) => db.Products.Add(product);

    public void Remove(Product product) => db.Products.Remove(product);

    public void ExpectVersion(Product product, byte[] rowVersion) =>
        db.Entry(product).Property(p => p.RowVersion).OriginalValue = rowVersion;

    // One UPDATE with the limits in the WHERE clause: either the whole change happens or nothing does.
    public async Task<int?> TryChangeStockAsync(
        int id, int delta, bool includeHidden, int userId, DateTime now, CancellationToken cancellationToken)
    {
        var updated = await Visible(includeHidden)
            .Where(p => p.Id == id && p.Stock + delta >= 0 && p.Stock + delta <= ProductLimits.StockMax)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(p => p.Stock, p => p.Stock + delta)
                .SetProperty(p => p.UpdatedAt, now)
                .SetProperty(p => p.UpdatedByUserId, userId),
                cancellationToken);

        if (updated == 0)
        {
            return null;
        }

        return await db.Products.Where(p => p.Id == id).Select(p => p.Stock).FirstAsync(cancellationToken);
    }

    public Task<int> MoveToCategoryAsync(int fromCategoryId, int toCategoryId, int userId, DateTime now, CancellationToken cancellationToken) =>
        db.Products
            .Where(p => p.CategoryId == fromCategoryId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(p => p.CategoryId, toCategoryId)
                .SetProperty(p => p.UpdatedAt, now)
                .SetProperty(p => p.UpdatedByUserId, userId),
                cancellationToken);

    private IQueryable<Product> Visible(bool includeHidden) =>
        includeHidden ? db.Products : db.Products.OnlyVisible();

    private static IQueryable<Product> WithDetails(IQueryable<Product> query) =>
        query.AsNoTracking().Include(p => p.Category).Include(p => p.UpdatedByUser);
}
