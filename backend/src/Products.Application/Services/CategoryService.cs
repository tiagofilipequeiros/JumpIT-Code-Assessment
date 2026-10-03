using Products.Application.Abstractions;
using Products.Application.Dtos.Categories;
using Products.Domain.Authorization;
using Products.Domain.Entities;
using Products.Domain.Errors;

namespace Products.Application.Services;

public class CategoryService(
    ICategoryRepository categories,
    IProductRepository products,
    IUnitOfWork unitOfWork,
    CurrentUser currentUser,
    UserMetricService metrics,
    TimeProvider clock)
{
    public async Task<List<CategoryResponse>> GetAllAsync(bool includeHidden, CancellationToken cancellationToken)
    {
        var canSeeHidden = includeHidden && await currentUser.HasAsync(Permission.ViewHidden, cancellationToken);
        return (await categories.ListAsync(canSeeHidden, cancellationToken)).Select(CategoryResponse.From).ToList();
    }

    public async Task<CategoryResponse> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        var canSeeHidden = await currentUser.HasAsync(Permission.ViewHidden, cancellationToken);
        var category = await categories.GetAsync(id, canSeeHidden, cancellationToken) ?? throw NotFound(id);
        return CategoryResponse.From(category);
    }

    public async Task<CategoryResponse> CreateAsync(CategoryRequest request, CancellationToken cancellationToken)
    {
        var user = await currentUser.RequireAsync(Permission.Edit, cancellationToken);
        var name = request.Name.Trim();
        await EnsureNameIsFreeAsync(name, exceptId: null, cancellationToken);

        var now = Now();
        var category = new Category { Name = name, CreatedAt = now, UpdatedAt = now };

        var id = await unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            categories.Add(category);
            await SaveAsync(name, cancellationToken);

            metrics.Record(user.Id, MetricEntity.Category, MetricAction.Create, category.Id, category.Name);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return category.Id;
        }, cancellationToken);

        return await GetByIdAsync(id, cancellationToken);
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

        categories.ExpectVersion(category, request.RowVersion);
        metrics.Record(user.Id, MetricEntity.Category, MetricAction.Update, id, changes.ToString());
        await SaveAsync(name, cancellationToken);

        return await GetByIdAsync(id, cancellationToken);
    }

    // Deleting a category keeps its products: they move to Uncategorized (visible to editors and admins only).
    public async Task DeleteAsync(int id, CancellationToken cancellationToken)
    {
        var user = await currentUser.RequireAsync(Permission.Delete, cancellationToken);
        var category = await FindEditableAsync(id, cancellationToken);

        await unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var moved = await products.MoveToCategoryAsync(id, Category.UncategorizedId, user.Id, Now(), cancellationToken);

            categories.Remove(category);
            metrics.Record(user.Id, MetricEntity.Category, MetricAction.Delete, id, $"{category.Name}; {moved} product(s) moved to Uncategorized");
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return moved;
        }, cancellationToken);
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
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return await GetByIdAsync(id, cancellationToken);
    }

    private async Task<Category> FindEditableAsync(int id, CancellationToken cancellationToken)
    {
        if (id == Category.UncategorizedId)
        {
            throw new AppException(ErrorCode.CategoryProtected);
        }

        return await categories.FindForUpdateAsync(id, cancellationToken) ?? throw NotFound(id);
    }

    // Checked first for a clear error. Two requests can still pass this check at the same moment;
    // the unique index then rejects the second one, which SaveAsync turns into the same error.
    private async Task EnsureNameIsFreeAsync(string name, int? exceptId, CancellationToken cancellationToken)
    {
        if (await categories.NameExistsAsync(name, exceptId, cancellationToken))
        {
            throw NameTaken(name);
        }
    }

    private async Task SaveAsync(string name, CancellationToken cancellationToken)
    {
        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (UniqueConstraintViolationException)
        {
            throw NameTaken(name);
        }
    }

    private DateTime Now() => clock.GetUtcNow().UtcDateTime;

    private static AppException NameTaken(string name) =>
        new(ErrorCode.CategoryNameTaken, $"A category named '{name}' already exists.");

    private static AppException NotFound(int id) => new(ErrorCode.CategoryNotFound, $"Category {id} was not found.");
}
