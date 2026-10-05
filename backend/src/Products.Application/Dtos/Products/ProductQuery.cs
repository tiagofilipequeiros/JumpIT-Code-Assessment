using System.ComponentModel.DataAnnotations;
using Products.Domain.Entities;

namespace Products.Application.Dtos.Products;

// Query string of GET /api/products. Every filter is optional and they combine with AND,
// e.g. ?search=lens&categoryIds=2&categoryIds=3&stockStatuses=LowStock&maxPrice=300
public class ProductQuery
{
    // Part of the name, case-insensitive.
    [StringLength(ProductLimits.NameMaxLength)]
    public string? Search { get; init; }

    public int[] CategoryIds { get; init; } = [];

    // Hidden statuses (Disabled, Uncategorized, CategoryDisabled) only apply to editors and admins.
    // Empty = everything the caller may see.
    public ProductStatus[] Statuses { get; init; } = [];

    public StockStatus[] StockStatuses { get; init; } = [];

    [Range(0, ProductLimits.StockMax)]
    public int? MinStock { get; init; }

    [Range(0, ProductLimits.StockMax)]
    public int? MaxStock { get; init; }

    [Range(typeof(decimal), "0", ProductLimits.PriceMax, ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true)]
    public decimal? MinPrice { get; init; }

    [Range(typeof(decimal), "0", ProductLimits.PriceMax, ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true)]
    public decimal? MaxPrice { get; init; }
}
