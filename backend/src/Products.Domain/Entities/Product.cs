namespace Products.Domain.Entities;

public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public int Stock { get; set; }
    public int CategoryId { get; set; }
    public Category Category { get; set; } = null!;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public int? UpdatedByUserId { get; set; }
    public User? UpdatedByUser { get; set; }
    public byte[] RowVersion { get; set; } = [];

    // Requires Category to be loaded.
    public ProductStatus Status =>
        !IsActive ? ProductStatus.Disabled
        : CategoryId == Category.UncategorizedId ? ProductStatus.Uncategorized
        : !Category.IsActive ? ProductStatus.CategoryDisabled
        : ProductStatus.Active;

    public StockStatus StockStatus =>
        Stock == 0 ? StockStatus.OutOfStock
        : Stock <= ProductLimits.LowStockThreshold ? StockStatus.LowStock
        : StockStatus.InStock;
}
