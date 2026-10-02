using Microsoft.EntityFrameworkCore;
using Products.Application.Abstractions;
using Products.Domain.Entities;
using Products.Infrastructure.Persistence;

namespace Products.Infrastructure.Repositories;

public class CategoryRepository(AppDbContext db) : ICategoryRepository
{
    public Task<List<CategorySummary>> ListAsync(bool includeHidden, CancellationToken cancellationToken) =>
        Summaries(Visible(includeHidden).OrderBy(c => c.Name)).ToListAsync(cancellationToken);

    public Task<CategorySummary?> GetAsync(int id, bool includeHidden, CancellationToken cancellationToken) =>
        Summaries(Visible(includeHidden).Where(c => c.Id == id)).FirstOrDefaultAsync(cancellationToken);

    public Task<bool> ExistsAsync(int id, CancellationToken cancellationToken) =>
        db.Categories.AnyAsync(c => c.Id == id, cancellationToken);

    // Case-insensitive because of the SQL Server default collation (same as the unique index).
    public Task<bool> NameExistsAsync(string name, int? exceptId, CancellationToken cancellationToken) =>
        db.Categories.AnyAsync(c => c.Name == name && c.Id != exceptId, cancellationToken);

    public Task<Category?> FindForUpdateAsync(int id, CancellationToken cancellationToken) =>
        db.Categories.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public void Add(Category category) => db.Categories.Add(category);

    public void Remove(Category category) => db.Categories.Remove(category);

    public void ExpectVersion(Category category, byte[] rowVersion) =>
        db.Entry(category).Property(c => c.RowVersion).OriginalValue = rowVersion;

    private IQueryable<Category> Visible(bool includeHidden) =>
        includeHidden ? db.Categories : db.Categories.Where(c => c.IsActive && c.Id != Category.UncategorizedId);

    private static IQueryable<CategorySummary> Summaries(IQueryable<Category> query) =>
        query.AsNoTracking().Select(c => new CategorySummary(c, c.Products.Count));
}
