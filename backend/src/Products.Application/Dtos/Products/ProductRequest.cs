using System.ComponentModel.DataAnnotations;
using Products.Application.Validation;
using Products.Domain.Entities;

namespace Products.Application.Dtos.Products;

// Body of POST /api/products.
public class ProductRequest
{
    [Required]
    [StringLength(ProductLimits.NameMaxLength, MinimumLength = ProductLimits.NameMinLength)]
    public string Name { get; init; } = string.Empty;

    [StringLength(ProductLimits.DescriptionMaxLength)]
    public string? Description { get; init; }

    // Invariant culture: the limits are written with a "." regardless of the server language.
    [Required]
    [Range(typeof(decimal), "0", ProductLimits.PriceMax, ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true)]
    [MaxDecimalPlaces(ProductLimits.PriceScale)]
    public decimal? Price { get; init; }

    [Required]
    [Range(0, ProductLimits.StockMax)]
    public int? Stock { get; init; }

    [Required]
    public int? CategoryId { get; init; }
}
