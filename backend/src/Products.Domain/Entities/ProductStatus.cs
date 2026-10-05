namespace Products.Domain.Entities;

// Why a product is (or isn't) visible to normal users. Exactly one applies, in this order of precedence.
public enum ProductStatus
{
    // Active, in an active, real category: visible to everyone.
    Active,
    // Switched off by an editor or admin.
    Disabled,
    // Its category was deleted (moved to Uncategorized).
    Uncategorized,
    // Active itself, but its category is switched off.
    CategoryDisabled,
}

public enum StockStatus
{
    InStock,
    // 1 to ProductLimits.LowStockThreshold units.
    LowStock,
    OutOfStock,
}
