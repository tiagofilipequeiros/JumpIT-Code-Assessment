using Products.Domain.Entities;

namespace Products.Infrastructure.Repositories;

public static class ProductQueries
{
    // The one visibility rule: normal users only see active products in active, real categories.
    public static IQueryable<Product> OnlyVisible(this IQueryable<Product> products) =>
        products.Where(p => p.IsActive && p.Category.IsActive && p.CategoryId != Category.UncategorizedId);
}
