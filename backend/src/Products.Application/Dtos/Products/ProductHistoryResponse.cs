namespace Products.Application.Dtos.Products;

// One past (or the current) version of a product, from the temporal history table.
public record ProductHistoryResponse(
    string Name,
    decimal Price,
    int Stock,
    bool IsActive,
    int CategoryId,
    string? UpdatedByName,
    DateTime ValidFrom,
    DateTime? ValidTo);
