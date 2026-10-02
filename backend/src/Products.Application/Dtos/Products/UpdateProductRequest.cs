using System.ComponentModel.DataAnnotations;

namespace Products.Application.Dtos.Products;

// Body of PUT /api/products/{id}: same fields plus the version the client last saw.
public class UpdateProductRequest : ProductRequest
{
    [Required]
    public byte[] RowVersion { get; init; } = [];
}
