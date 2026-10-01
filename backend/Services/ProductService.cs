using Backend.Auth;
using Backend.Data;
using Backend.Dtos;
using Backend.Errors;
using Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace Backend.Services;

// Product business logic. Controllers only translate HTTP to calls on this class.
public class ProductService(AppDbContext db, CurrentUser currentUser, MetricsService metrics, TimeProvider clock)
{
    public async Task<List<ProductResponse>> GetAllAsync(bool includeHidden, CancellationToken cancellationToken)
    {
        var query = await VisibleProductsAsync(includeHidden, cancellationToken);
        return await ToResponsesAsync(query, cancellationToken);
    }

    public async Task<List<ProductResponse>> SearchAsync(string name, bool includeHidden, CancellationToken cancellationToken)
    {
        var term = name.Trim();
        if (term.Length == 0)
        {
            throw new AppException(ErrorCode.ValidationFailed, "Search name is required.");
        }

        // Partial match; case-insensitive because of the SQL Server default collation.
        var query = await VisibleProductsAsync(includeHidden, cancellationToken);
        return await ToResponsesAsync(query.Where(p => p.Name.Contains(term)), cancellationToken);
    }

    public async Task<List<ProductResponse>> GetByStockLevelAsync(int? min, int? max, bool includeHidden, CancellationToken cancellationToken)
    {
        if (min < 0 || max < 0)
        {
            throw new AppException(ErrorCode.InvalidStockRange, "Stock values cannot be negative.");
        }

        if (min > max)
        {
            throw new AppException(ErrorCode.InvalidStockRange);
        }

        var query = await VisibleProductsAsync(includeHidden, cancellationToken);
        if (min is not null)
        {
            query = query.Where(p => p.Stock >= min);
        }

        if (max is not null)
        {
            query = query.Where(p => p.Stock <= max);
        }

        return await ToResponsesAsync(query, cancellationToken);
    }

    public async Task<ProductResponse> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        var query = await VisibleProductsAsync(includeHidden: true, cancellationToken);
        var product = await WithDetails(query).FirstOrDefaultAsync(p => p.Id == id, cancellationToken)
            ?? throw NotFound(id);

        return ProductResponse.From(product);
    }

    public async Task<List<ProductHistoryResponse>> GetHistoryAsync(int id, CancellationToken cancellationToken)
    {
        // Same visibility rules as reading the product itself.
        await GetByIdAsync(id, cancellationToken);

        var versions = await db.Products
            .TemporalAll()
            .AsNoTracking()
            .Where(p => p.Id == id)
            .OrderBy(p => EF.Property<DateTime>(p, "PeriodStart"))
            .Select(p => new
            {
                p.Name,
                p.Price,
                p.Stock,
                p.IsActive,
                p.CategoryId,
                p.UpdatedByUserId,
                ValidFrom = EF.Property<DateTime>(p, "PeriodStart"),
                ValidTo = EF.Property<DateTime>(p, "PeriodEnd"),
            })
            .ToListAsync(cancellationToken);

        var userIds = versions.Where(v => v.UpdatedByUserId is not null).Select(v => v.UpdatedByUserId!.Value).Distinct();
        var userNames = await db.Users
            .Where(u => userIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.Name, cancellationToken);

        return versions
            .Select(v => new ProductHistoryResponse(
                v.Name,
                v.Price,
                v.Stock,
                v.IsActive,
                v.CategoryId,
                v.UpdatedByUserId is { } userId ? userNames.GetValueOrDefault(userId) : null,
                DateTime.SpecifyKind(v.ValidFrom, DateTimeKind.Utc),
                // The current version is "valid until" the end of time.
                v.ValidTo.Year == DateTime.MaxValue.Year ? null : DateTime.SpecifyKind(v.ValidTo, DateTimeKind.Utc)))
            .ToList();
    }

    public async Task<ProductResponse> CreateAsync(ProductRequest request, CancellationToken cancellationToken)
    {
        var user = await currentUser.RequireAsync(Permission.Edit, cancellationToken);
        await EnsureCategoryCanBeUsedAsync(request.CategoryId!.Value, currentCategoryId: null, cancellationToken);

        var now = Now();
        var product = new Product
        {
            Name = request.Name.Trim(),
            Description = Clean(request.Description),
            Price = request.Price!.Value,
            Stock = request.Stock!.Value,
            CategoryId = request.CategoryId.Value,
            CreatedAt = now,
            UpdatedAt = now,
            UpdatedByUserId = user.Id,
        };

        // The ID comes from the database, so save the product first, then record the metric with it.
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        db.Products.Add(product);
        await db.SaveChangesAsync(cancellationToken);

        metrics.Record(user.Id, MetricEntity.Product, MetricAction.Create, product.Id, product.Name);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return await GetByIdAsync(product.Id, cancellationToken);
    }

    public async Task<ProductResponse> UpdateAsync(int id, UpdateProductRequest request, CancellationToken cancellationToken)
    {
        var user = await currentUser.RequireAsync(Permission.Edit, cancellationToken);
        var product = await db.Products.FirstOrDefaultAsync(p => p.Id == id, cancellationToken)
            ?? throw NotFound(id);

        await EnsureCategoryCanBeUsedAsync(request.CategoryId!.Value, product.CategoryId, cancellationToken);

        // Each line reads the old value, then assigns the new one.
        var changes = new ChangeList();
        changes.Add("Name", product.Name, product.Name = request.Name.Trim());
        changes.Add("Description", product.Description, product.Description = Clean(request.Description));
        changes.Add("Price", product.Price, product.Price = request.Price!.Value);
        changes.Add("Stock", product.Stock, product.Stock = request.Stock!.Value);
        changes.Add("Category", product.CategoryId, product.CategoryId = request.CategoryId.Value);
        product.UpdatedAt = Now();
        product.UpdatedByUserId = user.Id;

        // Only save if nobody changed the product since the client loaded it.
        db.Entry(product).Property(p => p.RowVersion).OriginalValue = request.RowVersion;
        metrics.Record(user.Id, MetricEntity.Product, MetricAction.Update, product.Id, changes.ToString());
        await SaveAsync(cancellationToken);

        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken)
    {
        var user = await currentUser.RequireAsync(Permission.Delete, cancellationToken);
        var product = await db.Products.FirstOrDefaultAsync(p => p.Id == id, cancellationToken)
            ?? throw NotFound(id);

        db.Products.Remove(product);
        metrics.Record(user.Id, MetricEntity.Product, MetricAction.Delete, product.Id, product.Name);
        await SaveAsync(cancellationToken);
    }

    public async Task<ProductResponse> SetActiveAsync(int id, bool isActive, CancellationToken cancellationToken)
    {
        var user = await currentUser.RequireAsync(Permission.ToggleActive, cancellationToken);
        var product = await db.Products.FirstOrDefaultAsync(p => p.Id == id, cancellationToken)
            ?? throw NotFound(id);

        if (product.IsActive != isActive)
        {
            product.IsActive = isActive;
            product.UpdatedAt = Now();
            product.UpdatedByUserId = user.Id;
            metrics.Record(user.Id, MetricEntity.Product, isActive ? MetricAction.Enable : MetricAction.Disable, product.Id, product.Name);
            await SaveAsync(cancellationToken);
        }

        return await GetByIdAsync(id, cancellationToken);
    }

    public Task<ProductResponse> AddStockAsync(int id, int quantity, CancellationToken cancellationToken) =>
        ChangeStockAsync(id, quantity, cancellationToken);

    public Task<ProductResponse> DecrementStockAsync(int id, int quantity, CancellationToken cancellationToken) =>
        ChangeStockAsync(id, -quantity, cancellationToken);

    // One atomic UPDATE with the limit in the WHERE clause, so concurrent requests
    // (even from different API instances) can never lose a change or go below 0.
    private async Task<ProductResponse> ChangeStockAsync(int id, int delta, CancellationToken cancellationToken)
    {
        var user = await currentUser.RequireAsync(Permission.ChangeStock, cancellationToken);
        var quantity = Math.Abs(delta);
        if (quantity is < ProductLimits.QuantityMin or > ProductLimits.QuantityMax)
        {
            throw new AppException(
                ErrorCode.InvalidQuantity,
                $"Quantity must be between {ProductLimits.QuantityMin} and {ProductLimits.QuantityMax}.");
        }

        var visible = await VisibleProductsAsync(includeHidden: true, cancellationToken);
        var now = Now();

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var updated = await visible
            .Where(p => p.Id == id && p.Stock + delta >= 0 && p.Stock + delta <= ProductLimits.StockMax)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(p => p.Stock, p => p.Stock + delta)
                .SetProperty(p => p.UpdatedAt, now)
                .SetProperty(p => p.UpdatedByUserId, user.Id),
                cancellationToken);

        if (updated == 0)
        {
            if (!await visible.AnyAsync(p => p.Id == id, cancellationToken))
            {
                throw NotFound(id);
            }

            throw delta < 0
                ? new AppException(ErrorCode.InsufficientStock, $"Cannot remove {quantity} from product {id}: not enough stock.")
                : new AppException(ErrorCode.StockLimitExceeded, $"Stock cannot exceed {ProductLimits.StockMax}.");
        }

        var newStock = await db.Products.Where(p => p.Id == id).Select(p => p.Stock).FirstAsync(cancellationToken);
        metrics.Record(
            user.Id,
            MetricEntity.Product,
            delta > 0 ? MetricAction.AddStock : MetricAction.DecrementStock,
            id,
            $"Stock {(delta > 0 ? "+" : "-")}{quantity} ({newStock - delta} → {newStock})");
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return await GetByIdAsync(id, cancellationToken);
    }

    // The one visibility rule for every product query: normal users only see active products
    // in active, real categories. Editors and admins can ask for everything.
    private async Task<IQueryable<Product>> VisibleProductsAsync(bool includeHidden, CancellationToken cancellationToken)
    {
        if (includeHidden && await currentUser.HasAsync(Permission.ViewHidden, cancellationToken))
        {
            return db.Products;
        }

        return db.Products.Where(p => p.IsActive && p.Category.IsActive && p.CategoryId != Category.UncategorizedId);
    }

    private static IQueryable<Product> WithDetails(IQueryable<Product> query) =>
        query.AsNoTracking().Include(p => p.Category).Include(p => p.UpdatedByUser);

    private static async Task<List<ProductResponse>> ToResponsesAsync(IQueryable<Product> query, CancellationToken cancellationToken)
    {
        var products = await WithDetails(query).OrderBy(p => p.Id).ToListAsync(cancellationToken);
        return products.Select(ProductResponse.From).ToList();
    }

    // New products cannot go to Uncategorized; existing ones may stay there.
    private async Task EnsureCategoryCanBeUsedAsync(int categoryId, int? currentCategoryId, CancellationToken cancellationToken)
    {
        if (categoryId == currentCategoryId)
        {
            return;
        }

        if (categoryId == Category.UncategorizedId || !await db.Categories.AnyAsync(c => c.Id == categoryId, cancellationToken))
        {
            throw new AppException(ErrorCode.InvalidCategory, $"Category {categoryId} cannot be used.");
        }
    }

    private async Task SaveAsync(CancellationToken cancellationToken)
    {
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new AppException(ErrorCode.ConcurrencyConflict);
        }
    }

    private DateTime Now() => clock.GetUtcNow().UtcDateTime;

    private static string? Clean(string? text) => string.IsNullOrWhiteSpace(text) ? null : text.Trim();

    private static AppException NotFound(int id) => new(ErrorCode.ProductNotFound, $"Product {id} was not found.");
}
