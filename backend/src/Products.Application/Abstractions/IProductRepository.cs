using Products.Domain.Entities;

namespace Products.Application.Abstractions;

public interface IProductRepository
{
    // Read-only queries (not tracked). Every filter that is set must match (AND).
    Task<List<Product>> ListAsync(ProductFilter filter, CancellationToken cancellationToken);

    // includeHidden = false applies the visibility rule for normal users.
    Task<Product?> GetAsync(int id, bool includeHidden, CancellationToken cancellationToken);
    Task<bool> ExistsAsync(int id, bool includeHidden, CancellationToken cancellationToken);
    Task<List<ProductVersion>> GetHistoryAsync(int id, CancellationToken cancellationToken);

    // Tracked: changes are saved by IUnitOfWork.
    Task<Product?> FindForUpdateAsync(int id, CancellationToken cancellationToken);
    void Add(Product product);
    void Remove(Product product);
    void ExpectVersion(Product product, byte[] rowVersion);

    // Atomic single-statement updates, executed immediately.
    // Returns the new stock, or null if the product is not visible or the change would break the stock limits.
    Task<int?> TryChangeStockAsync(int id, int delta, bool includeHidden, int userId, DateTime now, CancellationToken cancellationToken);
    Task<int> MoveToCategoryAsync(int fromCategoryId, int toCategoryId, int userId, DateTime now, CancellationToken cancellationToken);
}

// Empty lists and nulls mean "don't filter on this". Within a list any value may match (OR); across filters all must (AND).
public record ProductFilter
{
    public string? NameContains { get; init; }
    public IReadOnlyCollection<int> CategoryIds { get; init; } = [];
    public IReadOnlyCollection<ProductStatus> Statuses { get; init; } = [];
    public IReadOnlyCollection<StockStatus> StockStatuses { get; init; } = [];
    public int? MinStock { get; init; }
    public int? MaxStock { get; init; }
    public decimal? MinPrice { get; init; }
    public decimal? MaxPrice { get; init; }
}

// One version of a product from the temporal history table.
public record ProductVersion(
    string Name,
    decimal Price,
    int Stock,
    bool IsActive,
    int CategoryId,
    int? UpdatedByUserId,
    DateTime ValidFrom,
    DateTime? ValidTo);
