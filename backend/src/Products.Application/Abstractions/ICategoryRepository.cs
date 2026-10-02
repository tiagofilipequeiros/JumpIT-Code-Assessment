using Products.Domain.Entities;

namespace Products.Application.Abstractions;

public interface ICategoryRepository
{
    // Read-only queries (not tracked). IncludeHidden = false hides disabled categories and Uncategorized.
    Task<List<CategorySummary>> ListAsync(bool includeHidden, CancellationToken cancellationToken);
    Task<CategorySummary?> GetAsync(int id, bool includeHidden, CancellationToken cancellationToken);
    Task<bool> ExistsAsync(int id, CancellationToken cancellationToken);
    Task<bool> NameExistsAsync(string name, int? exceptId, CancellationToken cancellationToken);

    // Tracked: changes are saved by IUnitOfWork.
    Task<Category?> FindForUpdateAsync(int id, CancellationToken cancellationToken);
    void Add(Category category);
    void Remove(Category category);
    void ExpectVersion(Category category, byte[] rowVersion);
}

public record CategorySummary(Category Category, int ProductCount);
