using Products.Domain.Entities;

namespace Products.Application.Dtos.Products;

// What the API returns for a product. Keeps the database entity out of the API contract.
public record ProductResponse(
    int Id,
    string Name,
    string? Description,
    decimal Price,
    int Stock,
    int CategoryId,
    string CategoryName,
    bool IsActive,
    ProductStatus Status,
    StockStatus StockStatus,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    string? UpdatedByName,
    byte[] RowVersion)
{
    // Requires Category and UpdatedByUser to be loaded.
    public static ProductResponse From(Product product) => new(
        product.Id,
        product.Name,
        product.Description,
        product.Price,
        product.Stock,
        product.CategoryId,
        product.Category.Name,
        product.IsActive,
        product.Status,
        product.StockStatus,
        product.CreatedAt,
        product.UpdatedAt,
        product.UpdatedByUser?.Name,
        product.RowVersion);
}
