using System.ComponentModel.DataAnnotations;
using Products.Application.Validation;
using Products.Domain.Entities;

namespace Products.Application.Dtos.Products;

// Body of POST /api/products.
public class ProductRequest
{
    private readonly string _name = string.Empty;
    private readonly string? _description;

    [Required]
    [StringLength(ProductLimits.NameMaxLength, MinimumLength = ProductLimits.NameMinLength)]
    public string Name { get => _name; init => _name = TextInput.Trim(value); }

    // Blank becomes null.
    [StringLength(ProductLimits.DescriptionMaxLength)]
    public string? Description { get => _description; init => _description = TextInput.TrimToNull(value); }

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
