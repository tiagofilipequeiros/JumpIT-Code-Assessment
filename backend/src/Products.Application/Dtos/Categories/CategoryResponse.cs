using Products.Application.Abstractions;
using Products.Domain.Entities;

namespace Products.Application.Dtos.Categories;

public record CategoryResponse(
    int Id,
    string Name,
    bool IsActive,
    bool IsProtected,
    int ProductCount,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    byte[] RowVersion)
{
    public static CategoryResponse From(CategorySummary summary) => new(
        summary.Category.Id,
        summary.Category.Name,
        summary.Category.IsActive,
        summary.Category.Id == Category.UncategorizedId,
        summary.ProductCount,
        summary.Category.CreatedAt,
        summary.Category.UpdatedAt,
        summary.Category.RowVersion);
}
