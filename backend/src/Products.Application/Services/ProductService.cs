using Products.Application.Abstractions;
using Products.Application.Dtos.Products;
using Products.Domain.Authorization;
using Products.Domain.Entities;
using Products.Domain.Errors;

namespace Products.Application.Services;

// Product use cases. Controllers only translate HTTP to calls on this class.
public class ProductService(
    IProductRepository products,
    ICategoryRepository categories,
    IUserRepository users,
    IUnitOfWork unitOfWork,
    CurrentUser currentUser,
    UserMetricService metrics,
    TimeProvider clock)
{
    // GET /api/products: every filter optional, combined with AND.
    public async Task<List<ProductResponse>> GetAllAsync(ProductQuery query, CancellationToken cancellationToken)
    {
        EnsureValidRange(query.MinStock, query.MaxStock, ErrorCode.InvalidStockRange, "stock");
        EnsureValidRange(query.MinPrice, query.MaxPrice, ErrorCode.InvalidPriceRange, "price");

        var filter = new ProductFilter
        {
            NameContains = string.IsNullOrWhiteSpace(query.Search) ? null : query.Search.Trim(),
            CategoryIds = query.CategoryIds.Distinct().ToList(),
            Statuses = await VisibleStatusesAsync(query.Statuses, cancellationToken),
            StockStatuses = query.StockStatuses.Distinct().ToList(),
            MinStock = query.MinStock,
            MaxStock = query.MaxStock,
            MinPrice = query.MinPrice,
            MaxPrice = query.MaxPrice,
        };
        return await ListAsync(filter, cancellationToken);
    }

    // GET /api/products/search (from the assessment): the name filter on its own.
    public async Task<List<ProductResponse>> SearchAsync(string? name, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new AppException(ErrorCode.ValidationFailed, "Search name is required.");
        }

        return await GetAllAsync(new ProductQuery { Search = name }, cancellationToken);
    }

    // GET /api/products/stock-level (from the assessment): the stock range on its own.
    public async Task<List<ProductResponse>> GetByStockLevelAsync(int? min, int? max, CancellationToken cancellationToken)
    {
        if (min < 0 || max < 0)
        {
            throw new AppException(ErrorCode.InvalidStockRange, "Stock values cannot be negative.");
        }

        return await GetAllAsync(new ProductQuery { MinStock = min, MaxStock = max }, cancellationToken);
    }

    public async Task<ProductResponse> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        var product = await products.GetAsync(id, await CanSeeHiddenAsync(true, cancellationToken), cancellationToken)
            ?? throw NotFound(id);

        return ProductResponse.From(product);
    }

    public async Task<List<ProductHistoryResponse>> GetHistoryAsync(int id, CancellationToken cancellationToken)
    {
        // Same visibility rules as reading the product itself.
        if (!await products.ExistsAsync(id, await CanSeeHiddenAsync(true, cancellationToken), cancellationToken))
        {
            throw NotFound(id);
        }

        var versions = await products.GetHistoryAsync(id, cancellationToken);
        var userIds = versions.Where(v => v.UpdatedByUserId is not null).Select(v => v.UpdatedByUserId!.Value).Distinct();
        var userNames = await users.GetNamesAsync(userIds, cancellationToken);

        return versions
            .Select(v => new ProductHistoryResponse(
                v.Name,
                v.Price,
                v.Stock,
                v.IsActive,
                v.CategoryId,
                v.UpdatedByUserId is { } userId ? userNames.GetValueOrDefault(userId) : null,
                v.ValidFrom,
                v.ValidTo))
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
        var id = await unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            products.Add(product);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            metrics.Record(user.Id, MetricEntity.Product, MetricAction.Create, product.Id, product.Name);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return product.Id;
        }, cancellationToken);

        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task<ProductResponse> UpdateAsync(int id, UpdateProductRequest request, CancellationToken cancellationToken)
    {
        var user = await currentUser.RequireAsync(Permission.Edit, cancellationToken);
        var product = await products.FindForUpdateAsync(id, cancellationToken) ?? throw NotFound(id);

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

        // Only saved if nobody changed the product since the client loaded it.
        products.ExpectVersion(product, request.RowVersion);
        metrics.Record(user.Id, MetricEntity.Product, MetricAction.Update, product.Id, changes.ToString());
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken)
    {
        var user = await currentUser.RequireAsync(Permission.Delete, cancellationToken);
        var product = await products.FindForUpdateAsync(id, cancellationToken) ?? throw NotFound(id);

        products.Remove(product);
        metrics.Record(user.Id, MetricEntity.Product, MetricAction.Delete, product.Id, product.Name);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<ProductResponse> SetActiveAsync(int id, bool isActive, CancellationToken cancellationToken)
    {
        var user = await currentUser.RequireAsync(Permission.ToggleActive, cancellationToken);
        var product = await products.FindForUpdateAsync(id, cancellationToken) ?? throw NotFound(id);

        if (product.IsActive != isActive)
        {
            product.IsActive = isActive;
            product.UpdatedAt = Now();
            product.UpdatedByUserId = user.Id;
            metrics.Record(user.Id, MetricEntity.Product, isActive ? MetricAction.Enable : MetricAction.Disable, product.Id, product.Name);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return await GetByIdAsync(id, cancellationToken);
    }

    public Task<ProductResponse> AddStockAsync(int id, int quantity, CancellationToken cancellationToken) =>
        ChangeStockAsync(id, quantity, increase: true, cancellationToken);

    public Task<ProductResponse> DecrementStockAsync(int id, int quantity, CancellationToken cancellationToken) =>
        ChangeStockAsync(id, quantity, increase: false, cancellationToken);

    // The stock change is one atomic UPDATE with the limits in its WHERE clause (see ProductRepository),
    // so concurrent requests, even from different API instances, can never lose a change or go below 0.
    private async Task<ProductResponse> ChangeStockAsync(int id, int quantity, bool increase, CancellationToken cancellationToken)
    {
        var user = await currentUser.RequireAsync(Permission.ChangeStock, cancellationToken);

        // Validated before the direction is applied: a negative quantity must not turn "add" into "remove".
        if (quantity is < ProductLimits.QuantityMin or > ProductLimits.QuantityMax)
        {
            throw new AppException(
                ErrorCode.InvalidQuantity,
                $"Quantity must be between {ProductLimits.QuantityMin} and {ProductLimits.QuantityMax}.");
        }

        var delta = increase ? quantity : -quantity;

        var includeHidden = await CanSeeHiddenAsync(true, cancellationToken);
        await unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var newStock = await products.TryChangeStockAsync(id, delta, includeHidden, user.Id, Now(), cancellationToken);
            if (newStock is null)
            {
                if (!await products.ExistsAsync(id, includeHidden, cancellationToken))
                {
                    throw NotFound(id);
                }

                throw !increase
                    ? new AppException(ErrorCode.InsufficientStock, $"Cannot remove {quantity} from product {id}: not enough stock.")
                    : new AppException(ErrorCode.StockLimitExceeded, $"Stock cannot exceed {ProductLimits.StockMax}.");
            }

            metrics.Record(
                user.Id,
                MetricEntity.Product,
                increase ? MetricAction.AddStock : MetricAction.DecrementStock,
                id,
                $"Stock {(increase ? "+" : "-")}{quantity} ({newStock - delta} → {newStock})",
                quantity);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return newStock;
        }, cancellationToken);

        return await GetByIdAsync(id, cancellationToken);
    }

    // Only editors and admins may see hidden products; for anyone else the flag is ignored.
    private async Task<bool> CanSeeHiddenAsync(bool requested, CancellationToken cancellationToken) =>
        requested && await currentUser.HasAsync(Permission.ViewHidden, cancellationToken);

    // Normal users only ever get active products, whatever they ask for (silently, so one UI works for every role).
    private async Task<IReadOnlyCollection<ProductStatus>> VisibleStatusesAsync(
        IEnumerable<ProductStatus> requested, CancellationToken cancellationToken) =>
        await currentUser.HasAsync(Permission.ViewHidden, cancellationToken)
            ? requested.Distinct().ToList()
            : [ProductStatus.Active];

    private static void EnsureValidRange<T>(T? min, T? max, ErrorCode error, string what) where T : struct, IComparable<T>
    {
        if (min is { } low && max is { } high && low.CompareTo(high) > 0)
        {
            throw new AppException(error, $"Minimum {what} cannot be greater than maximum {what}.");
        }
    }

    private async Task<List<ProductResponse>> ListAsync(ProductFilter filter, CancellationToken cancellationToken) =>
        (await products.ListAsync(filter, cancellationToken)).Select(ProductResponse.From).ToList();

    // New products cannot go to Uncategorized; existing ones may stay there.
    private async Task EnsureCategoryCanBeUsedAsync(int categoryId, int? currentCategoryId, CancellationToken cancellationToken)
    {
        if (categoryId == currentCategoryId)
        {
            return;
        }

        if (categoryId == Category.UncategorizedId || !await categories.ExistsAsync(categoryId, cancellationToken))
        {
            throw new AppException(ErrorCode.InvalidCategory, $"Category {categoryId} cannot be used.");
        }
    }

    private DateTime Now() => clock.GetUtcNow().UtcDateTime;

    private static string? Clean(string? text) => string.IsNullOrWhiteSpace(text) ? null : text.Trim();

    private static AppException NotFound(int id) => new(ErrorCode.ProductNotFound, $"Product {id} was not found.");
}
