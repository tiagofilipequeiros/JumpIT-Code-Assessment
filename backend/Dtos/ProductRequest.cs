using System.ComponentModel.DataAnnotations;
using Backend.Models;
using Backend.Validation;

namespace Backend.Dtos;

// Body of POST /api/products.
public class ProductRequest
{
    [Required]
    [StringLength(ProductLimits.NameMaxLength, MinimumLength = ProductLimits.NameMinLength)]
    public string Name { get; init; } = string.Empty;

    [StringLength(ProductLimits.DescriptionMaxLength)]
    public string? Description { get; init; }

    [Required]
    [Range(typeof(decimal), "0", ProductLimits.PriceMax)]
    [MaxDecimalPlaces(ProductLimits.PriceScale)]
    public decimal? Price { get; init; }

    [Required]
    [Range(0, ProductLimits.StockMax)]
    public int? Stock { get; init; }

    [Required]
    public int? CategoryId { get; init; }
}

// Body of PUT /api/products/{id}: same fields plus the version the client last saw.
public class UpdateProductRequest : ProductRequest
{
    [Required]
    public byte[] RowVersion { get; init; } = [];
}
