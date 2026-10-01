using Backend.Auth;
using Backend.Data;
using Backend.Dtos;
using Backend.Errors;
using Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace Backend.Services;

public class CategoryService(AppDbContext db, CurrentUser currentUser, MetricsService metrics, TimeProvider clock)
{
    public async Task<List<CategoryResponse>> GetAllAsync(bool includeHidden, CancellationToken cancellationToken)
    {
        var query = db.Categories.AsQueryable();
        if (!includeHidden || !await currentUser.HasAsync(Permission.ViewHidden, cancellationToken))
        {
            query = query.Where(c => c.IsActive && c.Id != Category.UncategorizedId);
        }

        return await ToResponses(query.OrderBy(c => c.Name)).ToListAsync(cancellationToken);
    }

    public async Task<CategoryResponse> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        var query = db.Categories.Where(c => c.Id == id);
        if (!await currentUser.HasAsync(Permission.ViewHidden, cancellationToken))
        {
            query = query.Where(c => c.IsActive && c.Id != Category.UncategorizedId);
        }

        return await ToResponses(query).FirstOrDefaultAsync(cancellationToken) ?? throw NotFound(id);
    }

    public async Task<CategoryResponse> CreateAsync(CategoryRequest request, CancellationToken cancellationToken)
    {
        var user = await currentUser.RequireAsync(Permission.Edit, cancellationToken);
        var name = request.Name.Trim();
        await EnsureNameIsFreeAsync(name, exceptId: null, cancellationToken);

        var now = Now();
        var category = new Category { Name = name, CreatedAt = now, UpdatedAt = now };

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        db.Categories.Add(category);
        await db.SaveChangesAsync(cancellationToken);

        metrics.Record(user.Id, MetricEntity.Category, MetricAction.Create, category.Id, category.Name);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return await GetByIdAsync(category.Id, cancellationToken);
    }

    public async Task<CategoryResponse> UpdateAsync(int id, UpdateCategoryRequest request, CancellationToken cancellationToken)
    {
        var user = await currentUser.RequireAsync(Permission.Edit, cancellationToken);
        var category = await FindEditableAsync(id, cancellationToken);
        var name = request.Name.Trim();
        await EnsureNameIsFreeAsync(name, exceptId: id, cancellationToken);

        var changes = new ChangeList();
        changes.Add("Name", category.Name, name);
        category.Name = name;
        category.UpdatedAt = Now();

        db.Entry(category).Property(c => c.RowVersion).OriginalValue = request.RowVersion;
        metrics.Record(user.Id, MetricEntity.Category, MetricAction.Update, id, changes.ToString());
        await SaveAsync(cancellationToken);

        return await GetByIdAsync(id, cancellationToken);
    }

    // Deleting a category keeps its products: they move to Uncategorized (visible to editors and admins only).
    public async Task DeleteAsync(int id, CancellationToken cancellationToken)
    {
        var user = await currentUser.RequireAsync(Permission.Delete, cancellationToken);
        var category = await FindEditableAsync(id, cancellationToken);

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var now = Now();
        var moved = await db.Products
            .Where(p => p.CategoryId == id)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(p => p.CategoryId, Category.UncategorizedId)
                .SetProperty(p => p.UpdatedAt, now)
                .SetProperty(p => p.UpdatedByUserId, user.Id),
                cancellationToken);

        db.Categories.Remove(category);
        metrics.Record(user.Id, MetricEntity.Category, MetricAction.Delete, id, $"{category.Name}; {moved} product(s) moved to Uncategorized");
        await SaveAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task<CategoryResponse> SetActiveAsync(int id, bool isActive, CancellationToken cancellationToken)
    {
        var user = await currentUser.RequireAsync(Permission.ToggleActive, cancellationToken);
        var category = await FindEditableAsync(id, cancellationToken);

        if (category.IsActive != isActive)
        {
            category.IsActive = isActive;
            category.UpdatedAt = Now();
            metrics.Record(user.Id, MetricEntity.Category, isActive ? MetricAction.Enable : MetricAction.Disable, id, category.Name);
            await SaveAsync(cancellationToken);
        }

        return await GetByIdAsync(id, cancellationToken);
    }

    private async Task<Category> FindEditableAsync(int id, CancellationToken cancellationToken)
    {
        if (id == Category.UncategorizedId)
        {
            throw new AppException(ErrorCode.CategoryProtected);
        }

        return await db.Categories.FirstOrDefaultAsync(c => c.Id == id, cancellationToken) ?? throw NotFound(id);
    }

    // Checked here for a clear error; the unique index is the final guarantee.
    private async Task EnsureNameIsFreeAsync(string name, int? exceptId, CancellationToken cancellationToken)
    {
        if (await db.Categories.AnyAsync(c => c.Name == name && c.Id != exceptId, cancellationToken))
        {
            throw new AppException(ErrorCode.CategoryNameTaken, $"A category named '{name}' already exists.");
        }
    }

    private static IQueryable<CategoryResponse> ToResponses(IQueryable<Category> query) =>
        query.AsNoTracking().Select(c => new CategoryResponse(
            c.Id,
            c.Name,
            c.IsActive,
            c.Id == Category.UncategorizedId,
            c.Products.Count,
            c.CreatedAt,
            c.UpdatedAt,
            c.RowVersion));

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

    private static AppException NotFound(int id) => new(ErrorCode.CategoryNotFound, $"Category {id} was not found.");
}
