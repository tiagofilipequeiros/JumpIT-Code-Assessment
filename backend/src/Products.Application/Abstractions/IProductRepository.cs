using Products.Domain.Entities;

namespace Products.Application.Abstractions;

public interface IProductRepository
{
    // Read-only queries (not tracked). IncludeHidden = false applies the visibility rule for normal users.
    Task<List<Product>> ListAsync(ProductFilter filter, CancellationToken cancellationToken);
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

public record ProductFilter(bool IncludeHidden, string? NameContains = null, int? MinStock = null, int? MaxStock = null);

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
