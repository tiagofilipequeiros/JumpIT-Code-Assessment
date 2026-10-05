using Products.Domain.Entities;

namespace Products.Infrastructure.Repositories;

// The status rules as SQL-translatable filters. They mirror Product.Status / Product.StockStatus.
public static class ProductQueries
{
    // The one visibility rule: normal users only see active products in active, real categories.
    public static IQueryable<Product> OnlyVisible(this IQueryable<Product> products) =>
        products.Where(p => p.IsActive && p.Category.IsActive && p.CategoryId != Category.UncategorizedId);

    // Any of the given statuses (empty = no filter). Each flag becomes a SQL parameter.
    public static IQueryable<Product> WithStatus(this IQueryable<Product> products, IReadOnlyCollection<ProductStatus> statuses)
    {
        if (statuses.Count == 0)
        {
            return products;
        }

        var active = statuses.Contains(ProductStatus.Active);
        var disabled = statuses.Contains(ProductStatus.Disabled);
        var uncategorized = statuses.Contains(ProductStatus.Uncategorized);
        var categoryDisabled = statuses.Contains(ProductStatus.CategoryDisabled);
        const int uncategorizedId = Category.UncategorizedId;

        return products.Where(p =>
            (active && p.IsActive && p.CategoryId != uncategorizedId && p.Category.IsActive) ||
            (disabled && !p.IsActive) ||
            (uncategorized && p.IsActive && p.CategoryId == uncategorizedId) ||
            (categoryDisabled && p.IsActive && p.CategoryId != uncategorizedId && !p.Category.IsActive));
    }

    public static IQueryable<Product> WithStockStatus(this IQueryable<Product> products, IReadOnlyCollection<StockStatus> statuses)
    {
        if (statuses.Count == 0)
        {
            return products;
        }

        var inStock = statuses.Contains(StockStatus.InStock);
        var low = statuses.Contains(StockStatus.LowStock);
        var outOfStock = statuses.Contains(StockStatus.OutOfStock);
        const int threshold = ProductLimits.LowStockThreshold;

        return products.Where(p =>
            (inStock && p.Stock > threshold) ||
            (low && p.Stock > 0 && p.Stock <= threshold) ||
            (outOfStock && p.Stock == 0));
    }
}
